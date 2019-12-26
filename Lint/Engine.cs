//#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Lint.Attributes;
using Lint.Exceptions;
using Lint.Native;
using Lint.Native.LuaHooks;
using Lint.ObjectTranslation;

namespace Lint
{
    /// <summary>
    ///     Represents a Lua engine, which is a wrapper around Lua's C API.
    /// </summary>
    public sealed class Engine : IDisposable
    {
        // See https://github.com/lua/lua/blob/c6f7181e910b6b2ff1346b5486a31be87b1da5af/lua.h for a list of Lua constants.

        /// <summary>
        ///     Gets a flag which indicates whether the main state was already disposed.
        /// </summary>
        /// <remarks>This allows successive calls to the <see cref="Dispose" /> method.</remarks>
        private bool _disposed;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Engine" /> class. This will create an entirely new Lua state
        ///     for an application to run on.
        /// </summary>
        public Engine()
        {
            StatePointer = LuaLibrary.LuaLNewState();
            LuaLibrary.LuaLOpenLibs(StatePointer);
            this["import_namespace"] = CreateFunction(new Action<string>(ImportNamespace));
            this["import_type"] = CreateFunction(new Action<string, string>((typeName, nameOverride) =>
            {
                var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
                var exportedTypes = loadedAssemblies.SelectMany(a => a.GetExportedTypes());
                foreach (var type in exportedTypes)
                {
                    if (type.Name == typeName || type.FullName == typeName)
                    {
                        ImportType(type, nameOverride);
                    }
                }
            }));
            this["load_assembly"] = CreateFunction(new Action<string>(LoadAssembly));

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var type in assemblies.SelectMany(a => a.GetExportedTypes()))
            {
            	LuaGlobalAttribute luaGlobalAttribute = (LuaGlobalAttribute)Attribute.GetCustomAttribute(type, typeof(LuaGlobalAttribute));
                if (luaGlobalAttribute != null)
                {
                    ImportType(type, luaGlobalAttribute.NameOverride);
                    continue;
                }

                foreach (var method in type.GetMethods(
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance))
                {
                	luaGlobalAttribute = (LuaGlobalAttribute)Attribute.GetCustomAttribute(method, typeof(LuaGlobalAttribute));
                    if (luaGlobalAttribute == null)
                    {
                        continue;
                    }

                    var name = luaGlobalAttribute.NameOverride ?? method.Name;
                    this[name] = CreateFunction(method);
                }
            }
        }

        /// <summary>
        ///     Gets the indexer for this Lua state. The indexer exchanges information between Lua's global environment and C#,
        ///     allowing .NET applications to manipulate the contents of the global environment.
        /// </summary>
        /// <param name="global">The name of the global variable to fetch or store, which must not be <see langword="null" />.</param>
        /// <returns>The .NET representation of a Lua value stored under the specified name.</returns>
        public object this[string global]
        {
            get
            {
                ThrowIfDisposed();
                if (String.IsNullOrEmpty(global) || global.Trim().Length == 0)
                {
                    throw new ArgumentException("Global name must not be null or empty.", "global");
                }

                LuaLibrary.LuaGetGlobal(StatePointer, global);
                var @object = ObjectTranslator.GetObject(StatePointer, -1);
                LuaLibrary.LuaPop(StatePointer, 1);
                return @object;
            }
            set
            {
                ThrowIfDisposed();
                if (String.IsNullOrEmpty(global) || global.Trim().Length == 0)
                {
                    throw new ArgumentException("Global name must not be null or empty.", "global");
                }

                ObjectTranslator.PushToStack(StatePointer, value);
                LuaLibrary.LuaSetGlobal(StatePointer, global);
            }
        }

        /// <summary>
        ///     Gets the pointer to this Lua state.
        /// </summary>
        public IntPtr StatePointer { get;private set; }

        /// <summary>
        ///     Disposes the interpreter. This will close the current Lua state, which in turn calls GC metamethods and cleans up
        ///     used objects.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
            _disposed = true;
        }

        /// <summary>
        ///     The finalizer.
        /// </summary>
        ~Engine()
        {
            ReleaseUnmanagedResources();
        }

        /// <summary>
        ///     Registers a new Lua hook with a specified mask.
        /// </summary>
        /// <param name="hook">The hook, which must not be <see langword="null" />.</param>
        /// <param name="mask">The hook mask.</param>
        /// <param name="numberOfInstructions">
        ///     A value that specifies when the hook should execute if the
        ///     <see cref="EventMask.LUA_MASKCOUNT" /> mask is specified.
        /// </param>
        public void AddHook(LuaHook hook, EventMask mask, int numberOfInstructions = 0)
        {
            if (hook == null)
            {
                throw new ArgumentNullException("hook");
            }

            LuaLibrary.LuaSetHook(StatePointer, hook, (int) mask, numberOfInstructions);
        }

        /// <summary>
        ///     Creates and returns a new coroutine with the specified Lua function to execute.
        /// </summary>
        /// <param name="luaFunction">The Lua function which the coroutine will execute, which must not be <c>null</c>.</param>
        /// <returns>The coroutine.</returns>
        /// <exception cref="ObjectDisposedException">The interpreter was disposed.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="luaFunction" /> is <c>null</c>.</exception>
        public LuaCoroutine CreateCoroutine(LuaFunction luaFunction)
        {
            ThrowIfDisposed();
            var statePointer = LuaLibrary.LuaNewThread(StatePointer);
            luaFunction.PushToStack(StatePointer);
            LuaLibrary.LuaXMove(StatePointer, statePointer, 1);
            var coroutine = (LuaCoroutine) ObjectTranslator.GetObject(StatePointer, -1);
            LuaLibrary.LuaPop(StatePointer, 1);
            return coroutine;
        }

        /// <summary>
        ///     Creates and returns a new function with the specified body.
        /// </summary>
        /// <param name="luaChunk">
        ///     The chunk which the function will execute, i.e the function's body, which must not be
        ///     <c>null</c>.
        /// </param>
        /// <returns>The function.</returns>
        /// <exception cref="ObjectDisposedException">The interpreter was disposed.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="luaChunk" /> is <c>null</c>.</exception>
        /// <exception cref="LuaScriptException"><paramref name="luaChunk" /> is invalid.</exception>
        public LuaFunction CreateFunction(string luaChunk)
        {
            ThrowIfDisposed();
            if (LuaLibrary.LuaLLoadString(StatePointer, Encoding.UTF8.GetBytes(luaChunk)) !=
                (int) LuaThreadStatus.LUA_OK)
            {
                var errorMessage = (string) ObjectTranslator.GetObject(StatePointer, -1);
                LuaLibrary.LuaPop(StatePointer, 1);
                throw new LuaScriptException("An exception has occured while creating a function: " + errorMessage + "");
            }

            var function = (LuaFunction) ObjectTranslator.GetObject(StatePointer, -1);
            LuaLibrary.LuaPop(StatePointer, 1);
            return function;
        }

        /// <summary>
        ///     Creates and returns a function which will run the specified delegate.
        /// </summary>
        /// <param name="delegate">The delegate which the function will execute, which must not be <c>null</c>.</param>
        /// <returns>The function.</returns>
        /// <exception cref="ObjectDisposedException">The interpreter was disposed.</exception>
        /// <exception cref="ArgumentNullException">The <paramref name="delegate" /> is <c>null</c>.</exception>
        public LuaFunction CreateFunction(Delegate @delegate)
        {
            ThrowIfDisposed();
            var function = DoString(@"
                local delegate = delegate --[[ A common Lua idiom which allows caching functions and as a result improves access speed --]]
                return function(...)
                    return delegate(...)
                end")[0] as LuaFunction;

            this["delegate"] = null;
            return function;
        }

        /// <summary>
        ///     Creates and returns a function which will run the specified method.
        /// </summary>
        /// <param name="methodInfo">The method, which must not be <c>null</c>.</param>
        /// <param name="target">
        ///     The object on which to invoke the method. This argument is ignored if the method in question is
        ///     not an instance method.
        /// </param>
        /// <returns>The function.</returns>
        /// <exception cref="ObjectDisposedException">The interpreter was disposed.</exception>
        /// <exception cref="ArgumentNullException">The <paramref name="methodInfo" /> is <c>null</c>.</exception>
        /// <exception cref="NotSupportedException">The <paramref name="methodInfo" /> is a generic method.</exception>
        public LuaFunction CreateFunction(MethodInfo methodInfo, object target = null)
        {
            ThrowIfDisposed();
            if (methodInfo == null)
            {
                throw new ArgumentNullException("methodInfo");
            }

            if (methodInfo.ContainsGenericParameters)
            {
                throw new NotSupportedException();
            }

            // DeclaringType can only be null if the method is a global method defined on module level
            // C# cannot declare such methods natively and it seems unlikely that the caller
            // is referring to a VB.NET assembly
            if (target == null)
            {
	            if (methodInfo.DeclaringType == null)
	            {
	            	throw new InvalidOperationException();
	            }
	            else
	            {
	            	 target = Activator.CreateInstance(methodInfo.DeclaringType);
	            }
            }
            var typeArguments = methodInfo.GetParameters().Select(p => p.ParameterType)
                .Concat(new[] {methodInfo.ReturnType}).ToArray();
            var delegateType = Expression.GetFuncType(typeArguments);
            var @delegate = methodInfo.IsStatic
                ? Delegate.CreateDelegate(delegateType, methodInfo)
                : Delegate.CreateDelegate(delegateType, target, methodInfo.Name);

            return CreateFunction(@delegate);
        }

        /// <summary>
        ///     Creates and returns a new table with the specified number of elements.
        /// </summary>
        /// <param name="numberOfSequentialElements">The number of sequential elements.</param>
        /// <param name="numberOfOtherElements">The number of other elements.</param>
        /// <returns>The table.</returns>
        /// <exception cref="ObjectDisposedException">The interpreter was disposed.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="numberOfSequentialElements" /> is negative.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="numberOfOtherElements" /> is negative.</exception>
        /// <remarks>This method acts as the <c>lua_newtable</c> function by default.</remarks>
        public LuaTable CreateTable(int numberOfSequentialElements = 0, int numberOfOtherElements = 0)
        {
            ThrowIfDisposed();
            if (numberOfSequentialElements < 0)
            {
                throw new ArgumentOutOfRangeException("numberOfSequentialElements",
                    "Expected a non negative value.");
            }

            if (numberOfOtherElements < 0)
            {
                throw new ArgumentOutOfRangeException("numberOfOtherElements", "Expected a non negative value.");
            }

            // Create the table and push it to the stack
            LuaLibrary.LuaCreateTable(StatePointer, numberOfSequentialElements, numberOfOtherElements);
            var table = (LuaTable) ObjectTranslator.GetObject(StatePointer, -1);
            LuaLibrary.LuaPop(StatePointer, 1);
            return table;
        }

        /// <summary>
        ///     Executes the specified Lua file and returns the results.
        /// </summary>
        /// <param name="filePath">The file path, which must not be <c>null</c>.</param>
        /// <param name="numberOfResults">The number of results to return.</param>
        /// <returns>The results.</returns>
        /// <exception cref="ObjectDisposedException">The interpreter was disposed.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="filePath" /> is <c>null</c>.</exception>
        /// <exception cref="FileNotFoundException"><paramref name="filePath" /> is invalid.</exception>
        /// <exception cref="LuaException">The specified file is not a .lua file.</exception>
        /// <exception cref="LuaScriptException">Something went wrong while executing the file.</exception>
        public object[] DoFile(string filePath, int numberOfResults = LuaLibrary.LUA_MULTRET) { return
        		LuaLibrary.DoFile(StatePointer, filePath, numberOfResults); }

        /// <summary>
        ///     Executes the specified Lua chunk and returns the results.
        /// </summary>
        /// <param name="luaChunk">The Lua chunk to execute, which must not be <c>null</c>.</param>
        /// <param name="numberOfResults">The number of results to return.</param>
        /// <returns>The chunk's results.</returns>
        /// <exception cref="ObjectDisposedException">The interpreter was disposed.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="luaChunk" /> is <c>null</c>.</exception>
        /// <exception cref="LuaScriptException"><paramref name="luaChunk" /> is invalid.</exception>
        public object[] DoString(string luaChunk, int numberOfResults = LuaLibrary.LUA_MULTRET) { return
        		LuaLibrary.DoString(StatePointer, luaChunk, numberOfResults); }

        /// <summary>
        ///     Imports all types contained within the specified namespace.
        /// </summary>
        /// <param name="namespace">The namespace.</param>
        public void ImportNamespace(string @namespace)
        {
            ThrowIfDisposed();
            var exportedTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetExportedTypes());
            foreach (var type in exportedTypes)
            {
            	if (type.Namespace == @namespace && Attribute.GetCustomAttribute(type, typeof(LuaHideAttribute)) == null)
                {
                    ImportType(type);
                }
            }
        }

        /// <summary>
        ///     Stores the specified .NET type as a global variable.
        /// </summary>
        /// <param name="type">The type, which must not be <c>null</c>.</param>
        /// <param name="nameOverride">The name override which represents the name under which the type will be stored.</param>
        public void ImportType(Type type, string nameOverride = null)
        {
            ThrowIfDisposed();
            if (nameOverride == null)
            {
            	nameOverride = Regex.Replace(type.Name, "`.$", string.Empty);
            }
            ObjectTranslator.PushToStack(StatePointer, type);
            LuaLibrary.LuaSetGlobal(StatePointer, nameOverride);
        }

        /// <summary>
        ///     Instructs the engine to load the assembly from the specified assembly file.
        /// </summary>
        /// <param name="assemblyFile">The assembly file.</param>
        public void LoadAssembly(string assemblyFile)
        {
            ThrowIfDisposed();
            Assembly assembly = null;

            try
            {
                assembly = Assembly.LoadFrom(assemblyFile);
            }
            catch (FileNotFoundException)
            {
                // Swallow the exception and attempt to resolve the assembly using the AssemblyName
            }

            if (assembly == null)
            {
                Assembly.Load(AssemblyName.GetAssemblyName(assemblyFile));
            }
        }

        /// <summary>
        ///     Releases unmanaged resources.
        /// </summary>
        /// <remarks>
        ///     As we are dealing with unmanaged code we need to make sure that all handles are disposed of properly. Thus,
        ///     both the <see cref="Dispose" /> method and the finalizer run this method. This is somewhat similar to the popular
        ///     dispose pattern where the finalizer and Dispose() calls a virtual dispose method with an extra 'disposing' flag
        ///     which executes the actual disposal logic. The flag is used as an indication that the disposal is done properly
        ///     (from the Dispose method rather than the finalizer) and it is safe to reference other objects with finalizers.
        ///     However, since we're not working with such objects there is no need for the exact pattern.
        /// </remarks>
        private void ReleaseUnmanagedResources()
        {
            LuaLibrary.LuaClose(StatePointer);
        }

        /// <summary>
        ///     Throws an <see cref="ObjectDisposedException" /> if operating on a disposed instance of this class.
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (!_disposed)
            {
                return;
            }

            throw new ObjectDisposedException(GetType().Name);
        }
    }
}