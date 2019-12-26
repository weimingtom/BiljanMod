using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Lint.Debugging;
using Lint.Exceptions;
using Lint.Extensions;
using Lint.Native;

namespace Lint.ObjectTranslation
{
    /*
     * TODO LIST:
     *      [X] Delegate invocations should handle type conversions
     *      [X] When doing arithmetic operations both sides should have their methods evaluated
     *      [X] Implement an event handling mechanism
     *      [X] Work out a better overload resolution approach
     *      [X] Make the code DRY
     *      [X] Allow specifying constructor arguments when constructing generic types
     *      [X] Ensure that extension methods are properly invoked
     */

    /// <summary>
    ///     Represents a .NET object translator.
    /// </summary>
    internal sealed class ObjectTranslator
    {
        /// <summary>
        ///     Gets Lua's __index metamethod implemented in Lua itself. This metamethod is used to cache .NET objects and their
        ///     members. The function was pulled from NLua.
        /// </summary>
        private const string LuaBaseIndexMetamethod = @"
            local objectCache = setmetatable({}, { __mode = 'k' })
            return function(indexingFunction)
                return function(object, memberIndex)
                    local memberCache = objectCache[object]
                    if memberCache ~= nil then
                        if memberCache[memberIndex] ~= nil then
                            return memberCache[memberIndex]
                        end
                    else
                        memberCache = {}
                        objectCache[object] = memberCache
                    end

                    res, flag = indexingFunction(object, memberIndex)
                    if flag then
                        memberCache[memberIndex] = res
                    end
                    return res
                end
            end";

        private struct BoolObjectTuple
        {
        	public bool bValue; 
        	public object oValue;
        	
        	public BoolObjectTuple(bool bValue, object oValue)
        	{
        		this.bValue = bValue;
        		this.oValue = oValue;
        	}
        }
        
        /// <summary>
        ///     Gets the map which holds type coercion logic.
        /// </summary>
        private static readonly Dictionary<Type, Func<string, BoolObjectTuple>> CoercionMap;
        private static BoolObjectTuple byte_func(string s) 
        {
        	byte result;
        	bool s1 = byte.TryParse(s, out result); 
        	return new BoolObjectTuple(s1, result);
        }
        private static BoolObjectTuple decimal_func(string s) 
        {
        	decimal result;
        	bool s1 = decimal.TryParse(s, out result); 
        	return new BoolObjectTuple(s1, result);
        }
        private static BoolObjectTuple double_func(string s) 
        {
        	double result;
        	bool s1 = double.TryParse(s, out result); 
        	return new BoolObjectTuple(s1, result);
        }
        private static BoolObjectTuple float_func(string s) 
        {
        	float result;
        	bool s1 = float.TryParse(s, out result); 
        	return new BoolObjectTuple(s1, result);
        }
        private static BoolObjectTuple int_func(string s) 
        {
        	int result;
        	bool s1 = int.TryParse(s, out result); 
        	return new BoolObjectTuple(s1, result);
        }
        private static BoolObjectTuple long_func(string s) 
        {
        	long result;
        	bool s1 = long.TryParse(s, out result); 
        	return new BoolObjectTuple(s1, result);
        }
        private static BoolObjectTuple sbyte_func(string s) 
        {
        	sbyte result;
        	bool s1 = sbyte.TryParse(s, out result); 
        	return new BoolObjectTuple(s1, result);
        }
        private static BoolObjectTuple short_func(string s) 
        {
        	short result;
        	bool s1 = short.TryParse(s, out result); 
        	return new BoolObjectTuple(s1, result);
        }
        private static BoolObjectTuple uint_func(string s) 
        {
        	uint result;
        	bool s1 = uint.TryParse(s, out result); 
        	return new BoolObjectTuple(s1, result);
        }
        private static BoolObjectTuple ulong_func(string s) 
        {
        	ulong result;
        	bool s1 = ulong.TryParse(s, out result); 
        	return new BoolObjectTuple(s1, result);
        }
        private static BoolObjectTuple ushort_func(string s) 
        {
        	ushort result;
        	bool s1 = ushort.TryParse(s, out result); 
        	return new BoolObjectTuple(s1, result);
        }
        static ObjectTranslator() {
        	CoercionMap = new Dictionary<Type, Func<string, BoolObjectTuple>>();
        	CoercionMap.Add(typeof(byte), byte_func);
            CoercionMap.Add(typeof(decimal), decimal_func);
            CoercionMap.Add(typeof(double), double_func);
            CoercionMap.Add(typeof(float), float_func);
            CoercionMap.Add(typeof(int), int_func);
            CoercionMap.Add(typeof(long), long_func);
            CoercionMap.Add(typeof(sbyte), sbyte_func);
            CoercionMap.Add(typeof(short), short_func);
            CoercionMap.Add(typeof(uint), uint_func);
            CoercionMap.Add(typeof(ulong), ulong_func);
            CoercionMap.Add(typeof(ushort), ushort_func);
        }

        /// <summary>
        ///     Gets the object lookup table.
        /// </summary>
        private static readonly Dictionary<GCHandle, object> ObjectLookup = new Dictionary<GCHandle, object>();

        /// <summary>
        ///     Gets the base index metamethod.
        /// </summary>
        /// <remarks>This field acts as a strong root for the metamethod. See also: <see cref="_classMetamethods" />.</remarks>
        private readonly LuaFunction _baseIndexMetamethod;

        /// <summary>
        /// Gets the Lua engine.
        /// </summary>
        private readonly Engine _engine;

        /// <summary>
        ///     Holds the list of metamethods defined for the 'luaNet_class' metatable.
        /// </summary>
        /// <remarks>
        ///     This list acts as a strong root for metamethods. When delegates are passed to unmanaged code they must be kept
        ///     alive by the host environment until it is guaranteed that they will never be called. Failing to do so results in
        ///     callback exceptions since the delegates get GC'd eventually.
        /// </remarks>
        private readonly Dictionary<string, LuaFunctionDelegates.LuaCFunction> _classMetamethods;

        /// <summary>
        ///     Gets the method invocation function.
        /// </summary>
        /// <remarks>This field acts as a strong root.</remarks>
        private readonly LuaFunctionDelegates.LuaCFunction _methodInvocationCallback;

        /// <summary>
        ///     Holds the list of metamethods defined for the 'luaNet_object' metatable.
        /// </summary>
        /// <remarks>
        ///     This list acts as a strong root for metamethods. When delegates are passed to unmanaged code they must be kept
        ///     alive by the host environment until it is guaranteed that they will never be called. Failing to do so results in
        ///     callback exceptions since the delegates get GC'd eventually.
        /// </remarks>
        private readonly Dictionary<string, LuaFunctionDelegates.LuaCFunction> _objectMetamethods;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ObjectTranslator" /> class.
        /// </summary>
        public ObjectTranslator(Engine engine)
        {
            _engine = engine;
            _baseIndexMetamethod = (LuaFunction) _engine.DoString(LuaBaseIndexMetamethod)[0];
            _classMetamethods = new Dictionary<string, LuaFunctionDelegates.LuaCFunction>();
            _classMetamethods.Add("__gc", GcMetamethod);
            _classMetamethods.Add("__tostring", ToStringMetamethod);
            _classMetamethods.Add("__index", IndexMetamethod);
            _classMetamethods.Add("__newindex", NewIndexMetamethod);
            _classMetamethods.Add("__call", ClassCallMetamethod);

            _methodInvocationCallback = CallMethodCallback;

            _objectMetamethods = new Dictionary<string, LuaFunctionDelegates.LuaCFunction>();
            _objectMetamethods.Add("__gc", GcMetamethod);
            _objectMetamethods.Add("__tostring", ToStringMetamethod);
            _objectMetamethods.Add("__index", IndexMetamethod);
            _objectMetamethods.Add("__newindex", NewIndexMetamethod);
            _objectMetamethods.Add("__call", ObjectsCallMetamethod);
            _objectMetamethods.Add("__add", ObjectsAdditionMetamethod);
            _objectMetamethods.Add("__sub", ObjectsSubtractionMetamethod);
            _objectMetamethods.Add("__mul", ObjectsMultiplicationMetamethod);
            _objectMetamethods.Add("__div", ObjectsDivisionMetamethod);
            _objectMetamethods.Add("__unm", ObjectsNegationMetamethod);


            SetupMetatables();
        }

        /// <summary>
        ///     Returns the object at the specified index in the stack of the specified Lua state.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <param name="stackIndex">The stack index.</param>
        /// <returns>The object at the specified index.</returns>
        public static object GetObject(IntPtr luaState, int stackIndex)
        {
            var type = LuaLibrary.GetLuaType(luaState, stackIndex);
            switch (type)
            {
                case LuaType.None:
                    throw new LuaException("Invalid type.");
                case LuaType.Nil:
                    return null;
                case LuaType.Boolean:
                    return LuaLibrary.LuaToBoolean(luaState, stackIndex);
                case LuaType.Number:
                    IntPtr _temp;
                    if (LuaLibrary.LuaIsInteger(luaState, stackIndex))
                    {
                        return LuaLibrary.LuaToIntegerX(luaState, stackIndex, out _temp);
                    }

                    return LuaLibrary.LuaToNumberX(luaState, stackIndex, out _temp);
                case LuaType.String:
                    return LuaLibrary.LuaToString(luaState, stackIndex);
                case LuaType.Table:
                    return new LuaTable(luaState, GetReference(luaState, stackIndex));
                case LuaType.Function:
                    return new LuaFunction(luaState, GetReference(luaState, stackIndex));
                case LuaType.Thread:
                    return new LuaCoroutine(luaState, GetReference(luaState, stackIndex));
                case LuaType.Userdata:
                    var gcHandle =
                        GCHandle.FromIntPtr(Marshal.ReadIntPtr(LuaLibrary.LuaToUserdata(luaState, stackIndex)));
                    return ObjectLookup.GetValueOrDefault(gcHandle, null);
                default:
                    throw new ArgumentOutOfRangeException("type");
            }
        }

        private static int GetReference(IntPtr luaState, int stackIndex)
        {
            LuaLibrary.LuaPushValue(luaState, stackIndex);
            return LuaLibrary.LuaLRef(luaState, (int) LuaRegistry.RegistryIndex);
        }

        /// <summary>
        ///     Pushes the specified object to the top of the stack of the specified Lua state.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <param name="obj">The object.</param>
        public static void PushToStack(IntPtr luaState, object obj)
        {
            if (obj == null)
            {
            	LuaLibrary.LuaPushNil(luaState);
                return;
            } 
            else if (obj is LuaObject)
            {
                LuaObject luaObject = obj as LuaObject;
                luaObject.PushToStack(luaState);
                return;
            } 
            else if (obj is Type)
            {
                Type type = obj as Type;
                PushUserdata(luaState, type, "luaNet_class");
                return;
            }
            else
            {
                switch (Type.GetTypeCode(obj.GetType()))
                {
                    // TypeCode.Empty is handled by the null check above and TypeCode.Decimal is not supported
                    case TypeCode.Object:
                    case TypeCode.DBNull:
                    case TypeCode.DateTime:
                        PushObject(luaState, obj);
                        break;
                    case TypeCode.Boolean:
                        IConvertible convertible = (obj is IConvertible) ? (IConvertible)obj : null;
                        if (convertible != null)
                        {
                        	LuaLibrary.LuaPushBoolean(luaState, Convert.ToBoolean(convertible));
                        }
                        else
                        {
                        	LuaLibrary.LuaPushBoolean(luaState, false);
                        }
                        break;
                    case TypeCode.Char:
                    case TypeCode.String:
                        LuaLibrary.LuaPushString(luaState, obj.ToString());
                        break;
                    case TypeCode.SByte:
                    case TypeCode.Byte:
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                    case TypeCode.Int64:
                        LuaLibrary.LuaPushInteger(luaState, Convert.ToInt64(obj));
                        break;
                    case TypeCode.Single:
                    case TypeCode.Double:
                        LuaLibrary.LuaPushNumber(luaState, (double) obj);
                        break;
                    default:
                        throw new NotSupportedException("Decimals are not supported in Lua.");
                }
            }
        }

        /// <summary>
        ///     Scans the specified list of methods and returns the overload that best fits the given arguments.
        /// </summary>
        /// <param name="methods">The list of methods.</param>
        /// <param name="arguments">The arguments.</param>
        /// <returns>The method.</returns>
        private static MethodBase GetBestMatchingOverload(IEnumerable<MethodBase> methods, params object[] arguments)
        {
            var maxScore = 0;
            MethodBase bestOverload = null;

            foreach (var method in methods)
            {
                if (method == null)
                {
                    continue;
                }

                var parameters = method.IsExtensionMethod()
                    ? method.GetParameters().Skip(1).ToArray()
                    : method.GetParameters();
                if (parameters.Length == 0 && arguments.Length == 0) // Short-circuit methods that take no arguments
                {
                    return method;
                }

                var score = 0;
                for (var i = 0; i < parameters.Length; ++i)
                {
                    var parameter = parameters[i];
                    var argument = arguments.ElementAtOrDefault(i);
                    if (parameter.IsOut)
                    {
                        continue;
                    }

                    if (argument == null)
                    {
                        if (!parameter.IsOptional)
                        {
                            score = -1;
                            break;
                        }

                        arguments[i] = parameter.DefaultValue;
                        ++score;
                    }
                    else
                    {
                    	object result;
                        if (!TryImplicitConversion(parameter.ParameterType, argument, out result))
                        {
                            score = -1;
                            break; // Incompatible types, no further logic required
                        }

                        arguments[i] = result;
                        score += 2;
                    }
                }

                if (score > maxScore)
                {
                    maxScore = score;
                    bestOverload = method;
                }
            }

            return bestOverload;
        }

        /// <summary>
        ///     Pulls all objects starting at <paramref name="startIndex" /> to <paramref name="endIndex" /> from the stack of the
        ///     specified Lua state.
        /// </summary>
        /// <param name="luaState">The state from which to pull the objects from.</param>
        /// <param name="startIndex">The starting index.</param>
        /// <param name="endIndex">The ending index, inclusive.</param>
        /// <returns>The objects.</returns>
        private static object[] GetObjects(IntPtr luaState, int startIndex, int endIndex)
        {
            var objects = new object[endIndex - startIndex + 1];
            for (var i = startIndex; i <= endIndex; ++i)
            {
                objects[i - startIndex] = GetObject(luaState, i);
            }

            return objects;
        }

        /// <summary>
        ///     Pushes the specified object to the top of the stack of the specified Lua state.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <param name="obj">The object.</param>
        private static void PushObject(IntPtr luaState, object obj)
        {
            PushUserdata(luaState, obj, "luaNet_object");
        }

        /// <summary>
        ///     Pushes the specified object as userdata to the top of the stack of the specified Lua state.
        /// </summary>
        /// <param name="luaState">The Lua state.</param>
        /// <param name="obj">The object.</param>
        /// <param name="metatable">The object's metatable.</param>
        private static void PushUserdata(IntPtr luaState, object obj, string metatable = null)
        {
            var gcHandle = GCHandle.Alloc(obj);
            var pointer = LuaLibrary.LuaNewUserdata(luaState, new UIntPtr((uint) IntPtr.Size));
            Marshal.WriteIntPtr(pointer, GCHandle.ToIntPtr(gcHandle));
            if (!(String.IsNullOrEmpty(metatable) || metatable.Trim().Length == 0))
            {
                LuaLibrary.LuaGetField(luaState, (int) LuaRegistry.RegistryIndex, metatable);
                LuaLibrary.LuaSetMetatable(luaState, -2);
            }

            ObjectLookup[gcHandle] = obj;
        }

        /// <summary>
        ///     Gets the __tostring metamethod handler. This method pushes the string representation of the object to the top of
        ///     the stack.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <returns>The number of results pushed to the stack.</returns>
        private static int ToStringMetamethod(IntPtr luaState)
        {
            var obj = GCHandle.FromIntPtr(Marshal.ReadIntPtr(LuaLibrary.LuaToUserdata(luaState, 1))).Target;
            if (obj == null)
            {
                LuaLibrary.LuaPushNil(luaState);
            }
            else
            {
                LuaLibrary.LuaPushString(luaState, obj.ToString());
            }

            return 1;
        }

        /// <summary>
        ///     Attempts to implicitly convert the given object into the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="obj">The object.</param>
        /// <param name="result">The result.</param>
        /// <returns><c>true</c> if the conversion is successful; otherwise, <c>false</c>.</returns>
        private static bool TryImplicitConversion(Type type, object obj, out object result)
        {
            result = obj;
            if (obj is double)
            {
            	double d = (double)obj;
                if (type == typeof(float))
                {
                    result = (float) d;
                    return true;
                }

                result = (decimal) d;
                return true;
            }

            if (obj is long)
            {
                if (type == typeof(sbyte) || type == typeof(byte) || type == typeof(ushort) || type == typeof(short) ||
                    type == typeof(int) || type == typeof(ulong) || type == typeof(decimal) || type == typeof(double))
                {
                    result = Convert.ChangeType(obj, type);
                    return true;
                }
            }

            // Convert.ChangeType does not suffice as it is prone to format errors
            if (obj is string && type.IsNumeric())
            {
            	string s = obj as string;
				//(success, parsedObject)            	
                BoolObjectTuple _temp  = CoercionMap[type].Invoke(s);
                result = _temp.oValue;
                return _temp.bValue;
            }

            // Tables are utilized as array placeholders as they technically are arrays
            if (obj is LuaTable && type.IsArray)
            {
            	LuaTable luaTable = obj as LuaTable;
                var arrayType = type.GetElementType();
                var array = Array.CreateInstance(arrayType, luaTable.Values.Count);
                for (var i = 0; i < array.Length; ++i)
                {
                	Object temp;
                    if (!TryImplicitConversion(arrayType, luaTable.Values.ElementAt(i), out temp))
                    {
                        return false;
                    }

                    array.SetValue(temp, i);
                }

                result = array;
                return true;
            }

            return type.IsInstanceOfType(obj);
        }

        /// <summary>
        ///     The method that handles generic method resolution.
        /// </summary>
        /// <param name="methods">The list of methods.</param>
        /// <param name="obj">The instance on which the resolved method gets invoked on.</param>
        /// <param name="arguments">The method's arguments.</param>
        /// <returns></returns>
        private int CallGenericMethodCallback(IEnumerable<MethodInfo> methods, object obj, params object[] arguments)
        {
            var typeArguments = arguments.OfType<Type>().ToArray();
            if (typeArguments.Length == 0)
            {
                throw new LuaException("Attempt to call a method with invalid arguments.");
            }

            var genericMethods = new List<MethodInfo>();
            foreach (var method in methods)
            {
                if (!method.ContainsGenericParameters ||
                    method.GetGenericArguments().Length != typeArguments.Length)
                {
                    continue;
                }

                try
                {
                    genericMethods.Add(method.MakeGenericMethod(typeArguments));
                }
                catch (ArgumentException)
                {
                    throw new LuaException("Generic method arguments do not satisfy type constraints.");
                }
            }

            arguments = arguments.Skip(typeArguments.Length).ToArray();
            var methodInfo = GetBestMatchingOverload(genericMethods.ToArray(), arguments) as MethodInfo;
            if (methodInfo == null)
            {
                throw new LuaException("Attempt to call a generic method with invalid arguments.");
            }

            object result;
            try
            {
                result = methodInfo.Invoke(obj, arguments);
            }
            catch (TargetInvocationException ex)
            {
                throw new LuaException("An exception has occured while executing a generic .NET method: " + ex + "");
            }

            var numberOfResults = 0;
            if (methodInfo.ReturnType != typeof(void))
            {
                ++numberOfResults;
                PushToStack(_engine.StatePointer, result);
            }

            foreach (var parameter in methodInfo.GetParameters().Where(p => p.IsOut))
            {
                ++numberOfResults;
                PushToStack(_engine.StatePointer, arguments[parameter.Position]);
            }

            return numberOfResults;
        }

        /// <summary>
        ///     The method that is invoked when the caller calls an indexed method.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <returns>The number of results pushed to the stack.</returns>
        private int CallMethodCallback(IntPtr luaState)
        {
            DebugHelper.DumpStack(luaState);
            var @object = GetObject(luaState, LuaLibrary.LuaUpvalueIndex(1));
            var methodName = LuaLibrary.LuaToString(luaState, LuaLibrary.LuaUpvalueIndex(2));
            var isStatic = @object is Type;
            var typeMetadata =
                isStatic ? ((Type) @object).GetOrCreateMetadata() : @object.GetType().GetOrCreateMetadata();
            var methods = typeMetadata.GetMethods(methodName, !isStatic).ToArray();
            if (methods.Length == 0)
            {
                throw new LuaException("Attempt to call invalid method '" + methodName + "'.");
            }

            var arguments = GetObjects(luaState, isStatic ? 1 : 2, LuaLibrary.LuaGetTop(luaState));
            var method = (MethodInfo) GetBestMatchingOverload(methods, arguments);
            if (method == null)
            {
                return CallGenericMethodCallback(methods, isStatic ? null : @object, arguments);
            }

            object result;
            try
            {
                result = method.Invoke(isStatic || method.IsExtensionMethod() ? null : @object,
                    method.IsExtensionMethod() ? new[] {@object}.Concat(arguments).ToArray() : arguments);
            }
            catch (TargetInvocationException ex)
            {
                throw new LuaException("An exception has occured while executing a .NET method: " + ex + "");
            }

            var results = 0;
            if (method.ReturnType != typeof(void))
            {
                ++results;
                PushToStack(luaState, result);
            }

            foreach (var parameter in method.GetParameters().Where(p => p.IsOut || p.ParameterType.IsByRef))
            {
                ++results;
                PushToStack(luaState, arguments[parameter.Position]);
            }

            return results;
        }

        /// <summary>
        ///     Gets the __call metamethod. This method is invoked when Lua calls a type.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <returns>The number of results pushed to the stack.</returns>
        private int ClassCallMetamethod(IntPtr luaState)
        {
            // Possible TODO: try to infer type parameters from the arguments themselves like the compiler does
            DebugHelper.DumpStack(luaState);
            var type = (Type) GetObject(luaState, 1);
            if (type.IsAbstract || type.IsInterface)
            {
                throw new LuaException("Attempt to create an instance of an abstract class or an interface.");
            }

            var typeMetadata = type.GetOrCreateMetadata();
            var arguments = GetObjects(luaState, 2, LuaLibrary.LuaGetTop(luaState));
            if (type.ContainsGenericParameters)
            {
                var typeDefinition = type.GetGenericTypeDefinition();
                var numberOfTypeArguments = typeDefinition.GetGenericArguments().Length;
                var typeArguments = new Type[numberOfTypeArguments];
                for (var i = 0; i < numberOfTypeArguments; ++i)
                {
                	Type typeArg;
                    if (!(arguments[i] is Type))
                    {
                        throw new LuaException("Attempt to construct a generic type with a non-type argument.");
                    }
                    typeArg = arguments[i] as Type;

                    if (typeArg.IsPointer || type.IsByRef || typeArg == typeof(void) || typeArg.IsGenericType)
                    {
                        throw new LuaException("Attempt to construct a generic type with an invalid type argument.");
                    }

                    typeArguments[i] = typeArg;
                }

                try
                {
                    // Some constructors failed when arguments were supplied
                    var constructedType = typeDefinition.MakeGenericType(typeArguments);
                    //PushObject(Activator.CreateInstance(constructedType));
                    PushToStack(luaState, constructedType);
                }
                catch (ArgumentException)
                {
                    throw new LuaException(
                        "An error has occured while constructing a generic type: generic type arguments do not satisfy the type constraints.");
                }
            }
            else
            {
                object result;
                var constructors = typeMetadata.Constructors;
                var constructor = (ConstructorInfo) GetBestMatchingOverload(constructors.ToArray(), arguments);
                if (constructor == null)
                {
                    throw new LuaException(
                        "Type " + type.Name + " does not contain a constructor that contains the provided arguments.");
                }

                try
                {
                    result = constructor.Invoke(arguments);
                }
                catch (TargetInvocationException ex)
                {
                    throw new LuaException("An exception has occured while constructing a type: " + ex + "");
                }

                PushToStack(luaState, result);
            }

            return 1;
        }

        /// <summary>
        ///     Gets the __gc metamethod handler. This method will free the GCHandle bound to the userdata when the __gc metamethod
        ///     is invoked.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <returns>The number of results pushed to the stack.</returns>
        private int GcMetamethod(IntPtr luaState)
        {
            var gcHandle = GCHandle.FromIntPtr(Marshal.ReadIntPtr(LuaLibrary.LuaToUserdata(luaState, 1)));
            ObjectLookup.Remove(gcHandle);
            gcHandle.Free();
            return 0;
        }

        /// <summary>
        ///     Handles arithmetic method resolution.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <param name="opMethodName">The IL generated method name of an arithmetic operator.</param>
        /// <returns>The number of results pushed to the stack.</returns>
        private int HandleArithmeticMetamethod(IntPtr luaState, string opMethodName)
        {
            var firstOperand = GetObject(luaState, 1);
            var secondOperand = GetObject(luaState, 2);
            if (firstOperand == null && secondOperand == null)
            {
                throw new LuaException("Cannot perform arithmetic operations on nil objects.");
            }

            var arguments = new[] {firstOperand, secondOperand};
            var method = GetBestMatchingOverload(new MethodBase[]
            {
                firstOperand != null ? firstOperand.GetType().GetOrCreateMetadata().GetMethods(opMethodName).ElementAtOrDefault(0) : null,
                secondOperand != null ? secondOperand.GetType().GetOrCreateMetadata().GetMethods(opMethodName).ElementAtOrDefault(0) : null
            }, arguments);

            if (method == null)
            {
                throw new LuaException(
                    "Attempt to perform an arithmetic operation on operands that do not overload the '" + opMethodName + "' operator.");
            }

            object result;
            try
            {
                result = method.Invoke(null, arguments);
            }
            catch (TargetInvocationException ex)
            {
                throw new LuaException("An exception has occured while executing an operator method: " + ex + "");
            }

            PushToStack(luaState, result);
            return 1;
        }

        /// <summary>
        ///     Gets the __index metamethod. This method is invoked when Lua indexes an object or a type.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <returns>The number of results pushed to the stack.</returns>
        private int IndexMetamethod(IntPtr luaState)
        {
            DebugHelper.DumpStack(luaState);
            var @object = GetObject(luaState, 1);
            if (@object == null)
            {
                throw new LuaException("Attempt to index a null object.");
            }

            var typeReference = @object as Type;
            if (typeReference != null && (typeReference.IsAbstract || typeReference.IsInterface))
            {
                throw new LuaException("Attempt to index an abstract class or interface.");
            }

            var isStatic = typeReference != null;
            var member = LuaLibrary.LuaToString(luaState, 2);
            var typeMetadata = isStatic ? typeReference.GetOrCreateMetadata() : @object.GetType().GetOrCreateMetadata();
            var memberInfo = typeMetadata.GetMembers(member, !isStatic).ElementAtOrDefault(0);
            if (memberInfo == null)
            {
                throw new LuaException("Attempt to index invalid member '" + member + "'.");
            }

            @object = isStatic ? null : @object;
            switch (memberInfo.MemberType)
            {
                case MemberTypes.Event:
                    PushObject(luaState, new EventWrapper(@object, (EventInfo) memberInfo));
                    LuaLibrary.LuaPushBoolean(luaState, false);
                    break;
                case MemberTypes.Field:
                {
                    var fieldInfo = (FieldInfo) memberInfo;
                    PushToStack(luaState, fieldInfo.GetValue(@object));
                    LuaLibrary.LuaPushBoolean(luaState, fieldInfo.IsLiteral);
                }
                    break;
                case MemberTypes.Method:
                {
                    LuaLibrary.LuaPushValue(luaState, 1); // Push a copy of the type / object
                    LuaLibrary.LuaPushValue(luaState, 2); // Push a copy of the member's name (the method)
                    LuaLibrary.LuaPushCClosure(luaState, _methodInvocationCallback, 2);
                    LuaLibrary.LuaPushBoolean(luaState, true);
                }
                    break;
                case MemberTypes.Property:
                {
                    var propertyInfo = (PropertyInfo) memberInfo;
                    if (propertyInfo.GetGetMethod() == null)
                    {
                        throw new LuaException("Attempt to access a property without a valid getter.");
                    }

                    if (propertyInfo.GetIndexParameters().Length > 0)
                    {
                        PushObject(luaState, new IndexerWrapper(@object, propertyInfo));
                        LuaLibrary.LuaPushBoolean(luaState, false);
                        break;
                    }

                    PushToStack(luaState, propertyInfo.GetValue(@object, null));
                    LuaLibrary.LuaPushBoolean(luaState, !propertyInfo.CanWrite);
                }
                    break;
                default:
                {
                    PushObject(luaState, @object);
                    LuaLibrary.LuaPushBoolean(luaState, true);
                }
                    break;
            }

            return 2;
        }

        /// <summary>
        ///     Gets the __newindex metamethod. This method is invoked when Lua modifies a member of an object or a type.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <returns>The number of results pushed to the stack.</returns>
        private int NewIndexMetamethod(IntPtr luaState)
        {
            DebugHelper.DumpStack(luaState);
            var @object = GetObject(luaState, 1);
            if (@object == null)
            {
                throw new LuaException("Attempt to index a null object.");
            }

            var typeReference = @object as Type;
            if (typeReference != null && (typeReference.IsAbstract || typeReference.IsInterface))
            {
                throw new LuaException("Attempt to index an abstract class or interface.");
            }

            var isStatic = typeReference != null;
            var member = LuaLibrary.LuaToString(luaState, 2);
            var value = GetObject(luaState, 3);
            var typeMetadata =
            	isStatic ? typeReference.GetOrCreateMetadata() : @object.GetType().GetOrCreateMetadata();
            var memberInfo = typeMetadata.GetMembers(member, !isStatic).ElementAtOrDefault(0);
            if (memberInfo == null)
            {
                throw new LuaException("Attempt to index invalid member '" + member + "'.");
            }

            @object = isStatic ? null : @object;
            switch (memberInfo.MemberType)
            {
                case MemberTypes.Event:
                    throw new LuaException("Attempt to set an event.");
                case MemberTypes.Field:
                {
                    var field = (FieldInfo) memberInfo;
                    if (field.IsLiteral)
                    {
                        throw new LuaException("Attempt to set a constant.");
                    }

                    if (value == null && field.FieldType.IsValueType || (value != null ? value.GetType() : null) != field.FieldType)
                    {
                        throw new LuaException("Attempt to set field to invalid value.");
                    }

                    try
                    {
                        field.SetValue(@object, value);
                    }
                    catch (TargetInvocationException ex)
                    {
                        throw new LuaException("An exception has occured while setting a field: \n" + ex + "");
                    }
                }
                    break;
                case MemberTypes.Method:
                    throw new LuaException("Attempt to set a method.");
                case MemberTypes.Property:
                {
                    var property = (PropertyInfo) memberInfo;
                    if (property.GetSetMethod() == null || property.GetSetMethod().IsPrivate)
                    {
                        throw new LuaException("Attempt to set a read-only property.");
                    }

                    if (property.GetIndexParameters().Length > 0)
                    {
                        throw new LuaException("Attempt to set an indexed property. Use a :Set(obj) call instead.");
                    }

                    try
                    {
                        property.SetValue(@object, value, null);
                    }
                    catch (TargetInvocationException ex)
                    {
                        throw new LuaException("An exception has occured while setting a property: \n" + ex + "");
                    }
                }
                    break;
            }

            return 0;
        }

        /// <summary>
        ///     Gets the __add metamethod handler. This method is invoked when Lua performs addition on two objects.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <returns>The number of results pushed to the stack.</returns>
        private int ObjectsAdditionMetamethod(IntPtr luaState) { return HandleArithmeticMetamethod(luaState, "op_Addition");}

        /// <summary>
        ///     Gets the __call metamethod handler. This method is invoked when Lua calls an object.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <returns>The number of results pushed to the stack.</returns>
        private int ObjectsCallMetamethod(IntPtr luaState)
        {
            DebugHelper.DumpStack(luaState);
            var @object = GetObject(luaState, 1);
            if (@object == null)
            {
                throw new LuaException("Attempt to call a nil object.");
            }

            if (!(@object is Delegate))
            {
                throw new LuaException("Attempt to call a non-delegate.");
            }
            Delegate @delegate = @object as Delegate;

            // Arguments are usually coerced to the right type when resolving overloads
            // Since there is no overload resolution here we have to coerce the arguments manually
            var arguments = GetObjects(luaState, 2, LuaLibrary.LuaGetTop(luaState));
            for (var i = 0; i < arguments.Length; ++i)
            {
                var parameter = @delegate.Method.GetParameters().ElementAtOrDefault(i);
                if (parameter == null)
                {
                    throw new LuaException("Attempt to call a delegate with too many arguments.");
                }

                if (!TryImplicitConversion(parameter.ParameterType, arguments[i], out arguments[i]))
                {
                    throw new LuaException("Attempt to call a delegate with invalid arguments.");
                }
            }

            object result;
            try
            {
                result = @delegate.Method.Invoke(@delegate.Target, arguments);
            }
            catch (TargetInvocationException ex)
            {
                throw new LuaException("An exception has occured while executing a .NET delegate: " + ex + "");
            }

            var results = 0;
            if (@delegate.Method.ReturnType != typeof(void))
            {
                ++results;
                PushToStack(luaState, result);
            }

            foreach (var parameter in @delegate.Method.GetParameters().Where(p => p.IsOut || p.ParameterType.IsByRef))
            {
                ++results;
                PushToStack(luaState, arguments[parameter.Position]);
            }

            return results;
        }

        /// <summary>
        ///     Gets the __div metamethod handler. This method is invoked when Lua performs division on two objects.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <returns>The number of results pushed to the stack.</returns>
        private int ObjectsDivisionMetamethod(IntPtr luaState) { return HandleArithmeticMetamethod(luaState, "op_Division");}

        /// <summary>
        ///     Gets the __mul metamethod handler. This method is invoked when Lua performs multiplication on two objects.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <returns>The number of results pushed to the stack.</returns>
        private int ObjectsMultiplicationMetamethod(IntPtr luaState) { return
        		HandleArithmeticMetamethod(luaState, "op_Multiply"); }

        /// <summary>
        ///     Gets the __unm metamethod handler. This method is invoked when Lua performs negation on an object.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <returns>The number of results pushed to the stack.</returns>
        private int ObjectsNegationMetamethod(IntPtr luaState)
        {
            var @object = GetObject(luaState, 1);
            if (@object == null)
            {
                throw new LuaException("Cannot perform negation on a nil object.");
            }

            var typeMetadata = @object.GetType().GetOrCreateMetadata();
            var negationOperator = typeMetadata.GetMethods("op_UnaryNegation").ElementAtOrDefault(0);
            if (negationOperator == null)
            {
                throw new LuaException(
                    "Attempt to perform negation on an object that does not overload the negation operator.");
            }

            var result = negationOperator.Invoke(@object, new[] {@object});
            PushToStack(luaState, result);
            return 1;
        }

        /// <summary>
        ///     Gets the __sub metamethod handler. This method is invoked when Lua subtracts two objects.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <returns>The number of results pushed to the stack.</returns>
        private int ObjectsSubtractionMetamethod(IntPtr luaState) { return
        		HandleArithmeticMetamethod(luaState, "op_Subtraction"); }

        /// <summary>
        ///     Creates the metatables for classes and objects.
        /// </summary>
        private void SetupMetatables()
        {
            LuaLibrary.LuaLNewMetatable(_engine.StatePointer, "luaNet_class");
            foreach (var key in _classMetamethods.Keys)
            {
            	var value = _classMetamethods[key];
                PushMetamethod(key, value);
            }

            LuaLibrary.LuaPop(_engine.StatePointer, 1);

            LuaLibrary.LuaLNewMetatable(_engine.StatePointer, "luaNet_object");
            foreach (var key in _objectMetamethods.Keys)
            {
            	var value = _objectMetamethods[key];
                PushMetamethod(key, value);
            }

            LuaLibrary.LuaPop(_engine.StatePointer, 1);
        }
        
        void PushMetamethod(string key, LuaFunctionDelegates.LuaCFunction function)
        {
            LuaLibrary.LuaPushString(_engine.StatePointer, key);
            if (key == "__index") // The __index metamethod incorporates a special caching mechanism
            {
                _baseIndexMetamethod.PushToStack(_engine.StatePointer);
                LuaLibrary.LuaPushCFunction(_engine.StatePointer, function);
                LuaLibrary.LuaPCallK(_engine.StatePointer, 1, 1);
            }
            else
            {
                LuaLibrary.LuaPushCFunction(_engine.StatePointer, function);
            }

            LuaLibrary.LuaSetTable(_engine.StatePointer, -3);
        }        
    }
}