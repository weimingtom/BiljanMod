using System;
using System.IO;
using System.Reflection;
using System.Text;
using Lint.Exceptions;
using Lint.Native.OS;
using Lint.ObjectTranslation;
using System.Runtime.InteropServices;

namespace Lint.Native
{
    /// <summary>
    ///     Represents the Lua library. This class holds delegate instances of all known Lua function signatures, i.e it is in
    ///     charge of communicating with lua53.dll.
    /// </summary>
    internal static class LuaLibrary
    {
        /// <summary>
        ///     Gets the minimum required number of free stack slots. This value is used with lua_checkstack.
        /// </summary>
        public const int LUA_MINSTACK = 20;

        /// <summary>
        ///     Gets the MULTRET value. MULTRET values are used with lua_call and lua_pcall to indicate that all
        ///     function results should be pushed to the stack.
        /// </summary>
        public const int LUA_MULTRET = -1;

        /// <summary>
        ///     Gets the lua_checkstack delegate.
        /// </summary>
        public static readonly LuaFunctionDelegates.LuaCheckStack LuaCheckStack;

        /// <summary>
        ///     Gets the lua_close delegate.
        /// </summary>
        public static readonly LuaFunctionDelegates.LuaClose LuaClose;

        /// <summary>
        ///     Gets the lua_createtable delegate.
        /// </summary>
        public static readonly LuaFunctionDelegates.LuaCreateTable LuaCreateTable;

        /// <summary>
        ///     Gets the lua_getfield delegate.
        /// </summary>
        public static readonly LuaFunctionDelegates.LuaGetField LuaGetField;

        /// <summary>
        ///     Gets the lua_getglobal delegate.
        /// </summary>
        public static readonly LuaFunctionDelegates.LuaGetGlobal LuaGetGlobal;

        /// <summary>
        ///     Gets the lua_getmetatable delegate.
        /// </summary>
        public static readonly LuaFunctionDelegates.LuaGetMetatable LuaGetMetatable;

        /// <summary>
        ///     Gets the lua_getstack delegate.
        /// </summary>
        public static readonly LuaFunctionDelegates.LuaGetStack LuaGetStack;

        /// <summary>
        ///     Gets the lua_gettable delegate.
        /// </summary>
        public static readonly LuaFunctionDelegates.LuaGetTable LuaGetTable;

        /// <summary>
        ///     Gets the lua_gettop delegate.
        /// </summary>
        public static readonly LuaFunctionDelegates.LuaGetTop LuaGetTop;

        /// <summary>
        ///     Gets the lua_isinteger delegate.
        /// </summary>
        public static readonly LuaFunctionDelegates.LuaIsInteger LuaIsInteger;

        /// <summary>
        ///     Gets the luaL_loadstring delegate.
        /// </summary>
        public static readonly LuaFunctionDelegates.LuaLLoadString LuaLLoadString;

        /// <summary>
        ///     Gets the luaL_newmetatable delegate.
        /// </summary>
        public static readonly LuaFunctionDelegates.LuaLNewMetatable LuaLNewMetatable;

        /// <summary>
        ///     Gets the luaL_newstate delegate.
        /// </summary>
        public static readonly LuaFunctionDelegates.LuaLNewState LuaLNewState;

        /// <summary>
        ///     Gets the luaL_openlibs delegate.
        /// </summary>
        public static readonly LuaFunctionDelegates.LuaLOpenLibs LuaLOpenLibs;

        /// <summary>
        ///     Gets the luaL_ref delegate.
        /// </summary>
        public static readonly LuaFunctionDelegates.LuaLRef LuaLRef;

        /// <summary>
        ///     Gets the lua_newthread delegate.
        /// </summary>
        public static readonly LuaFunctionDelegates.LuaNewThread LuaNewThread;

        /// <summary>
        ///     Gets the lua_newuserdata delegate.
        /// </summary>
        public static readonly LuaFunctionDelegates.LuaNewUserdata LuaNewUserdata;

        /// <summary>
        ///     Gets the lua_next delegate.
        /// </summary>
        public static readonly LuaFunctionDelegates.LuaNext LuaNext;

        /// <summary>
        ///     Gets the lua_pcall delegate.
        /// </summary>
        public static readonly LuaFunctionDelegates.LuaPCallK LuaPCallK;

        /// <summary>
        ///     Gets the lua_pushboolean delegate.
        /// </summary>
        public static readonly LuaFunctionDelegates.LuaPushBoolean LuaPushBoolean;

        /// <summary>
        ///     Gets the lua_pushcclosure delegate.
        /// </summary>
        public static readonly LuaFunctionDelegates.LuaPushCClosure LuaPushCClosure;

        /// <summary>
        ///     Gets the lua_pushinteger delegate.
        /// </summary>
        public static readonly LuaFunctionDelegates.LuaPushInteger LuaPushInteger;

        /// <summary>
        ///     Gets the lua_pushlstring delegate.
        /// </summary>
        public static readonly LuaFunctionDelegates.LuaPushLString LuaPushLString;

        /// <summary>
        ///     Gets the lua_pushnil delegate.
        /// </summary>
        public static readonly LuaFunctionDelegates.LuaPushNil LuaPushNil;

        /// <summary>
        ///     Gets the lua_pushnumber delegate.
        /// </summary>
        public static readonly LuaFunctionDelegates.LuaPushNumber LuaPushNumber;

        /// <summary>
        ///     Gets the lua_pushvalue delegate.
        /// </summary>
        public static readonly LuaFunctionDelegates.LuaPushValue LuaPushValue;

        /// <summary>
        ///     Gets the lua_rawgeti delegate.
        /// </summary>
        public static readonly LuaFunctionDelegates.LuaRawGetI LuaRawGetI;

        /// <summary>
        ///     Gets the lua_resume delegate.
        /// </summary>
        public static readonly LuaFunctionDelegates.LuaResume LuaResume;

        /// <summary>
        ///     Gets the lua_setglobal delegate.
        /// </summary>
        public static readonly LuaFunctionDelegates.LuaSetGlobal LuaSetGlobal;

        /// <summary>
        ///     Gets the lua_sethook delegate.
        /// </summary>
        public static readonly LuaFunctionDelegates.LuaSetHook LuaSetHook;

        /// <summary>
        ///     Gets the lua_setmetatable delegate.
        /// </summary>
        public static readonly LuaFunctionDelegates.LuaSetMetatable LuaSetMetatable;

        /// <summary>
        ///     Gets the lua_settable delegate.
        /// </summary>
        public static readonly LuaFunctionDelegates.LuaSetTable LuaSetTable;

        /// <summary>
        ///     Gets the lua_settop delegate.
        /// </summary>
        public static readonly LuaFunctionDelegates.LuaSetTop LuaSetTop;

        /// <summary>
        ///     Gets the lua_status delegate.
        /// </summary>
        public static readonly LuaFunctionDelegates.LuaStatus LuaStatus;

        /// <summary>
        ///     Gets the lua_toboolean delegate.
        /// </summary>
        public static readonly LuaFunctionDelegates.LuaToBoolean LuaToBoolean;

        /// <summary>
        ///     Gets the lua_tointegerx delegate.
        /// </summary>
        public static readonly LuaFunctionDelegates.LuaToIntegerX LuaToIntegerX;

        /// <summary>
        ///     Gets the lua_tolstring delegate.
        /// </summary>
        public static readonly LuaFunctionDelegates.LuaToLString LuaToLString;

        /// <summary>
        ///     Gets the lua_tonumberx delegate.
        /// </summary>
        public static readonly LuaFunctionDelegates.LuaToNumberX LuaToNumberX;

        /// <summary>
        ///     Gets the lua_topointer delegate.
        /// </summary>
        public static readonly LuaFunctionDelegates.LuaToPointer LuaToPointer;

        /// <summary>
        ///     Gets the lua_tothread delegate.
        /// </summary>
        public static readonly LuaFunctionDelegates.LuaToThread LuaToThread;

        /// <summary>
        ///     Gets the lua_touserdata delegate.
        /// </summary>
        public static readonly LuaFunctionDelegates.LuaToUserdata LuaToUserdata;

        /// <summary>
        ///     Gets the lua_type delegate.
        /// </summary>
        private static readonly LuaFunctionDelegates.LuaGetType LuaTypeDelegate;

        /// <summary>
        ///     Gets the lua_xmove delegate.
        /// </summary>
        public static readonly LuaFunctionDelegates.LuaXMove LuaXMove;

        /// <summary>
        ///     Gets the static constructor. This constructor is used to assign static Lua function delegates.
        /// </summary>
        /// <remarks>
        ///     Static constructors are only invoked once per class, which is right before the first instance is created or
        ///     before accessing static fields.
        /// </remarks>
        static LuaLibrary()
        {
            // The former does not account for 32-bit apps and thus attempts to load a 64 bit library
            var architecture = IntPtr.Size == 8 ? "64bit" : "32bit";
            var fileName = "lua53.dll"; //RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "lua53.so" : "lua53.dll";
            
            FunctionLoader functionLoader;
            string name = Path.GetDirectoryName(new Uri(Assembly.GetAssembly(typeof(Engine)).CodeBase).LocalPath);
            if (name == null)
            {
            	throw new InvalidOperationException();
            }
            else
            {
            	functionLoader = new FunctionLoader(Path.Combine(Path.Combine(Path.Combine(name, "libs"), architecture), fileName));
            }

            // TODO: One of these functions results in a nullptr and keeps throwing exceptions [X]
            LuaCheckStack = (LuaFunctionDelegates.LuaCheckStack)functionLoader.LoadFunction("lua_checkstack", typeof(LuaFunctionDelegates.LuaCheckStack));
            LuaClose = (LuaFunctionDelegates.LuaClose)functionLoader.LoadFunction("lua_close", typeof(LuaFunctionDelegates.LuaClose));
            LuaCreateTable = (LuaFunctionDelegates.LuaCreateTable)functionLoader.LoadFunction("lua_createtable", typeof(LuaFunctionDelegates.LuaCreateTable));
            LuaGetField = (LuaFunctionDelegates.LuaGetField)functionLoader.LoadFunction("lua_getfield", typeof(LuaFunctionDelegates.LuaGetField));
            LuaGetGlobal = (LuaFunctionDelegates.LuaGetGlobal)functionLoader.LoadFunction("lua_getglobal", typeof(LuaFunctionDelegates.LuaGetGlobal));
            LuaGetMetatable = (LuaFunctionDelegates.LuaGetMetatable)functionLoader.LoadFunction("lua_getmetatable", typeof(LuaFunctionDelegates.LuaGetMetatable));
            LuaGetStack = (LuaFunctionDelegates.LuaGetStack)functionLoader.LoadFunction("lua_getstack", typeof(LuaFunctionDelegates.LuaGetStack));
            LuaGetTable = (LuaFunctionDelegates.LuaGetTable)functionLoader.LoadFunction("lua_gettable", typeof(LuaFunctionDelegates.LuaGetTable));
            LuaGetTop = (LuaFunctionDelegates.LuaGetTop)functionLoader.LoadFunction("lua_gettop", typeof(LuaFunctionDelegates.LuaGetTop));
            LuaIsInteger = (LuaFunctionDelegates.LuaIsInteger)functionLoader.LoadFunction("lua_isinteger", typeof(LuaFunctionDelegates.LuaIsInteger));
            LuaLLoadString = (LuaFunctionDelegates.LuaLLoadString)functionLoader.LoadFunction("luaL_loadstring", typeof(LuaFunctionDelegates.LuaLLoadString));
            LuaLNewMetatable = (LuaFunctionDelegates.LuaLNewMetatable)functionLoader.LoadFunction("luaL_newmetatable", typeof(LuaFunctionDelegates.LuaLNewMetatable));
            LuaLNewState = (LuaFunctionDelegates.LuaLNewState)functionLoader.LoadFunction("luaL_newstate", typeof(LuaFunctionDelegates.LuaLNewState));
            LuaLOpenLibs = (LuaFunctionDelegates.LuaLOpenLibs)functionLoader.LoadFunction("luaL_openlibs", typeof(LuaFunctionDelegates.LuaLOpenLibs));
            LuaLRef = (LuaFunctionDelegates.LuaLRef)functionLoader.LoadFunction("luaL_ref", typeof(LuaFunctionDelegates.LuaLRef));
            LuaNewThread = (LuaFunctionDelegates.LuaNewThread)functionLoader.LoadFunction("lua_newthread", typeof(LuaFunctionDelegates.LuaNewThread));
            LuaNewUserdata = (LuaFunctionDelegates.LuaNewUserdata)functionLoader.LoadFunction("lua_newuserdata", typeof(LuaFunctionDelegates.LuaNewUserdata));
            LuaNext = (LuaFunctionDelegates.LuaNext)functionLoader.LoadFunction("lua_next", typeof(LuaFunctionDelegates.LuaNext));
            LuaPCallK = (LuaFunctionDelegates.LuaPCallK)functionLoader.LoadFunction("lua_pcallk", typeof(LuaFunctionDelegates.LuaPCallK));
            //LuaPCall = functionLoader.LoadFunction<LuaPCall>("lua_pcall"); TODO: Replace with lua_pcallk which is also safer [X]
            //LuaPop = functionLoader.LoadFunction<LuaPop>("lua_pop"); 
            /*
             * For some reason the native file handler cannot access the pop function (10.2.2019)
             *
             * Elaboration: lua_pcall and lua_pop are not actual C functions but macros.
             * lua_pcall invokes the lua_pcallk function with default parameters while
             * lua_pop invokes the lua_settop function and executes specific logic. Since both of these Lua functions are C macros
             * they cannot be marshaled as pointers to the interpreter, which results in an exception being raised when attempting
             * to marshal them. (11.2.2019)
             */
            LuaPushBoolean = (LuaFunctionDelegates.LuaPushBoolean)functionLoader.LoadFunction("lua_pushboolean", typeof(LuaFunctionDelegates.LuaPushBoolean));
            LuaPushCClosure = (LuaFunctionDelegates.LuaPushCClosure)functionLoader.LoadFunction("lua_pushcclosure", typeof(LuaFunctionDelegates.LuaPushCClosure));
            LuaPushInteger = (LuaFunctionDelegates.LuaPushInteger)functionLoader.LoadFunction("lua_pushinteger", typeof(LuaFunctionDelegates.LuaPushInteger));
            LuaPushLString = (LuaFunctionDelegates.LuaPushLString)functionLoader.LoadFunction("lua_pushlstring", typeof(LuaFunctionDelegates.LuaPushLString));
            LuaPushNil = (LuaFunctionDelegates.LuaPushNil)functionLoader.LoadFunction("lua_pushnil", typeof(LuaFunctionDelegates.LuaPushNil));
            LuaPushNumber = (LuaFunctionDelegates.LuaPushNumber)functionLoader.LoadFunction("lua_pushnumber", typeof(LuaFunctionDelegates.LuaPushNumber));
            LuaPushValue = (LuaFunctionDelegates.LuaPushValue)functionLoader.LoadFunction("lua_pushvalue", typeof(LuaFunctionDelegates.LuaPushValue));
            LuaRawGetI = (LuaFunctionDelegates.LuaRawGetI)functionLoader.LoadFunction("lua_rawgeti", typeof(LuaFunctionDelegates.LuaRawGetI));
            //LuaReplace = functionLoader.LoadFunction<LuaFunctionDelegates.LuaReplace>("lua_replace");
            LuaResume = (LuaFunctionDelegates.LuaResume)functionLoader.LoadFunction("lua_resume", typeof(LuaFunctionDelegates.LuaResume));
            LuaSetGlobal = (LuaFunctionDelegates.LuaSetGlobal)functionLoader.LoadFunction("lua_setglobal", typeof(LuaFunctionDelegates.LuaSetGlobal));
            LuaSetHook = (LuaFunctionDelegates.LuaSetHook)functionLoader.LoadFunction("lua_sethook", typeof(LuaFunctionDelegates.LuaSetHook));
            LuaSetMetatable = (LuaFunctionDelegates.LuaSetMetatable)functionLoader.LoadFunction("lua_setmetatable", typeof(LuaFunctionDelegates.LuaSetMetatable));
            LuaSetTable = (LuaFunctionDelegates.LuaSetTable)functionLoader.LoadFunction("lua_settable", typeof(LuaFunctionDelegates.LuaSetTable));
            LuaSetTop = (LuaFunctionDelegates.LuaSetTop)functionLoader.LoadFunction("lua_settop", typeof(LuaFunctionDelegates.LuaSetTop));
            LuaStatus = (LuaFunctionDelegates.LuaStatus)functionLoader.LoadFunction("lua_status", typeof(LuaFunctionDelegates.LuaStatus));
            LuaToBoolean = (LuaFunctionDelegates.LuaToBoolean)functionLoader.LoadFunction("lua_toboolean", typeof(LuaFunctionDelegates.LuaToBoolean));
            LuaToIntegerX = (LuaFunctionDelegates.LuaToIntegerX)functionLoader.LoadFunction("lua_tointegerx", typeof(LuaFunctionDelegates.LuaToIntegerX));
            LuaToLString = (LuaFunctionDelegates.LuaToLString)functionLoader.LoadFunction("lua_tolstring", typeof(LuaFunctionDelegates.LuaToLString));
            LuaToNumberX = (LuaFunctionDelegates.LuaToNumberX)functionLoader.LoadFunction("lua_tonumberx", typeof(LuaFunctionDelegates.LuaToNumberX));
            LuaToPointer = (LuaFunctionDelegates.LuaToPointer)functionLoader.LoadFunction("lua_topointer", typeof(LuaFunctionDelegates.LuaToPointer));
            LuaToThread = (LuaFunctionDelegates.LuaToThread)functionLoader.LoadFunction("lua_tothread", typeof(LuaFunctionDelegates.LuaToThread));
            LuaToUserdata = (LuaFunctionDelegates.LuaToUserdata)functionLoader.LoadFunction("lua_touserdata", typeof(LuaFunctionDelegates.LuaToUserdata));
            LuaTypeDelegate = (LuaFunctionDelegates.LuaGetType)functionLoader.LoadFunction("lua_type", typeof(LuaFunctionDelegates.LuaGetType));
            LuaXMove = (LuaFunctionDelegates.LuaXMove)functionLoader.LoadFunction("lua_xmove", typeof(LuaFunctionDelegates.LuaXMove));
        }

        /// <summary>
        ///     Executes the specified Lua file and returns the results.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <param name="filePath">The file path, which must not be <c>null</c>.</param>
        /// <param name="numberOfResults">The number of results to return.</param>
        /// <returns>The results.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="filePath" /> is <c>null</c>.</exception>
        /// <exception cref="FileNotFoundException"><paramref name="filePath" /> is invalid.</exception>
        /// <exception cref="LuaException">The specified file is not a .lua file.</exception>
        /// <exception cref="LuaScriptException">Something went wrong while executing the file.</exception>
        public static object[] DoFile(IntPtr luaState, string filePath, int numberOfResults = LUA_MULTRET)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException();
            }

            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
	            if (Path.GetExtension(fileStream.Name) != ".lua")
	            {
	                throw new LuaException("Cannot execute a non Lua file.");
	            }
	
	            using (var binaryReader = new BinaryReader(fileStream))
	            {
		            var buffer = binaryReader.ReadBytes((int) fileStream.Length);
		            LuaThreadStatus errorCode;
		            if ((errorCode = (LuaThreadStatus) LuaLLoadString(luaState, buffer)) != LuaThreadStatus.LUA_OK)
		            {
		                // Lua pushes an error message in case of exceptions
		                var errorMessage = (string) ObjectTranslator.GetObject(luaState, -1);
		                LuaPop(luaState, 1); // Pop the error message and throw an exception
		                throw new LuaScriptException("[" + errorCode + "]: " + errorMessage + "");
		            }
	            }
            }

            return CallWithArguments(luaState, numberOfResults: numberOfResults);
        }

        /// <summary>
        ///     Executes the specified Lua chunk and returns the results.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <param name="luaChunk">The Lua chunk to execute, which must not be <c>null</c>.</param>
        /// <param name="numberOfResults">The number of results to return.</param>
        /// <returns>The chunk's results.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="luaChunk" /> is <c>null</c>.</exception>
        /// <exception cref="LuaScriptException"><paramref name="luaChunk" /> is invalid.</exception>
        public static object[] DoString(IntPtr luaState, string luaChunk, int numberOfResults = LUA_MULTRET)
        {
            LuaThreadStatus errorCode;
            if ((errorCode = (LuaThreadStatus) LuaLLoadString(luaState, System.Text.Encoding.Default.GetBytes(luaChunk))) !=
                LuaThreadStatus.LUA_OK)
            {
                // Lua pushes an error message in case of errors
                var errorMessage = (string) ObjectTranslator.GetObject(luaState, -1);
                LuaPop(luaState, 1); // Pop the error message and throw an exception
                throw new LuaScriptException("[" + errorCode + "]: " + errorMessage + "");
            }

            return CallWithArguments(luaState, numberOfResults: numberOfResults);
        }

        /// <summary>
        ///     Gets the Lua type of the element at the specified index in the stack of the specified Lua state.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <param name="stackIndex">The stack index of the element.</param>
        /// <returns>The corresponding <see cref="LuaType" />.</returns>
        public static LuaType GetLuaType(IntPtr luaState, int stackIndex) { return
        		(LuaType) LuaTypeDelegate(luaState, stackIndex); }

        /// <summary>
        ///     Pops the specified number of elements from the stack of the specified Lua state.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <param name="numberOfElementsToPop">The number of elements to pop.</param>
        public static void LuaPop(IntPtr luaState, int numberOfElementsToPop)
        {
            LuaSetTop(luaState, -numberOfElementsToPop - 1);
        }

        /// <summary>
        ///     Pushes a C function to the top of the stack of the specified Lua state.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <param name="luaCFunction">The C function.</param>
        /// <remarks>
        ///     All C functions must follow the required syntax; the function takes a Lua state as the only parameter and it
        ///     must return the number of results pushed to the stack.
        /// </remarks>
        public static void LuaPushCFunction(IntPtr luaState, LuaFunctionDelegates.LuaCFunction luaCFunction)
        {
            LuaPushCClosure(luaState, luaCFunction, 0);
        }

        /// <summary>
        ///     Pushes the specified string to the top of the stack of the specified Lua state.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <param name="str">The string to push.</param>
        public static void LuaPushString(IntPtr luaState, string str)
        {
            var bytes = System.Text.Encoding.Default.GetBytes(str); // Lua uses UTF-8 encoding for strings
            LuaPushLString(luaState, bytes, new UIntPtr((uint) bytes.Length));
        }

        /// <summary>
        ///     Gets the string representation of the element at the specified index in the stack of the specified Lua state.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <param name="index">The stack index of the element.</param>
        /// <returns>The string representation of the element at the specified index.</returns>
        public static string LuaToString(IntPtr luaState, int index)
        {
        	UIntPtr length;
            var stringPointer = LuaToLString(luaState, index, out length);
            var buffer = new byte[(byte) length];
            Marshal.Copy(stringPointer, buffer, 0, buffer.Length);
            return Encoding.UTF8.GetString(buffer);
        }

        /// <summary>
        ///     Returns the pseudo-index that represents the i-th upvalue of the function that is currently executing.
        /// </summary>
        /// <param name="upvalue">The upvalue.</param>
        /// <returns>The pseudo-index representing the i-th upvalue.</returns>
        public static int LuaUpvalueIndex(int upvalue) { return (int) LuaRegistry.RegistryIndex - upvalue; }

        /// <summary>
        ///     Calls the function at the top of the stack with the specified arguments.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <param name="arguments">The function's arguments.</param>
        /// <param name="numberOfResults">The number of results to be pushed to the stack. Defaults to all results.</param>
        /// <returns>The function's results.</returns>
        /// <exception cref="LuaException">Something went wrong while executing the function.</exception>
        public static object[] CallWithArguments(IntPtr luaState, object[] arguments = null,
            int numberOfResults = LUA_MULTRET)
        {
            // The function (which is currently at the top of the stack) gets popped along with the arguments when it's called
            var stackTop = LuaGetTop(luaState) - 1;

            // The function is already on the stack so the only thing left to do is push the arguments in direct order
            if (arguments != null)
            {
                if (!LuaCheckStack(luaState, arguments.Length))
                {
                    throw new LuaException("The stack does not have enough space to allocate that many arguments.");
                }

                foreach (var argument in arguments)
                {
                    ObjectTranslator.PushToStack(luaState, argument);
                }
            }

            // Adjust the number of results to avoid errors
            numberOfResults = numberOfResults < -1 ? -1 : numberOfResults;
            LuaThreadStatus errorCode;
            if ((errorCode =
                 (LuaThreadStatus) LuaPCallK(luaState, (arguments != null ? arguments.Length : 0), numberOfResults)) !=
                LuaThreadStatus.LUA_OK)
            {
                // Lua pushes an error message in case of errors
                var errorMessage = (string) ObjectTranslator.GetObject(luaState, -1);
                LuaPop(luaState, 1); // Pop the error message and throw an exception
                throw new LuaException(
                    "An exception has occured while calling a function: [" + errorCode + "]: " + errorMessage + "");
            }

            var newStackTop = LuaGetTop(luaState);
            var results = new object[newStackTop - stackTop];
            for (var i = newStackTop; i > stackTop; --i) // Results are also pushed in direct order
            {
                results[i - stackTop - 1] = ObjectTranslator.GetObject(luaState, i);
            }

            // As per Lua's reference manual, resetting the stack to it's original form is considered good programming practice
            LuaSetTop(luaState, stackTop);
            return results;
        }
    }
}