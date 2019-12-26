using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security;
using Lint.Native.LuaHooks;
using lua_Integer = System.Int64;

namespace Lint.Native
{
    /// <summary>
    ///     Holds Lua's function signatures. See https://www.lua.org/manual/5.3/contents.html#contentsup
    /// </summary>
    [SuppressMessage("ReSharper", "BuiltInTypeReferenceStyle")]
    public static class LuaFunctionDelegates
    {
        /// <summary>
        ///     The delegate signature for C functions.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <returns>The number of return values pushed onto the stack.</returns>
        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int LuaCFunction(IntPtr luaState);

        /// <summary>
        ///     Ensures that the specified number of values can be safely pushed onto the stack.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <param name="n">The number of values to push.</param>
        /// <returns>
        ///     <c>false</c> if the stack would overflow or if it cannot allocate memory for the extra space, otherwise;
        ///     <c>true</c>.
        /// </returns>
        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate bool LuaCheckStack(IntPtr luaState, int n);

        /// <summary>
        ///     Destroys all objects created by the specified Lua state and closes the state.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LuaClose(IntPtr luaState);

        /// <summary>
        ///     Creates a new table and pushes it onto the stack.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <param name="numberOfSequentialElements">The number of elements as a sequence.</param>
        /// <param name="numberOfOtherElements">The number of other elements</param>
        /// <remarks>Lua uses the provided numbers as a preallocation hint which may result in a performance improvement.</remarks>
        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LuaCreateTable(IntPtr luaState, int numberOfSequentialElements,
            int numberOfOtherElements);

        /// <summary>
        ///     Pushes the value t[key] to the top of the stack, where t is the table at the specified index.
        ///     This function may invoke the __index metamethod.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <param name="tableIndex">The table index.</param>
        /// <param name="key">The key.</param>
        /// <returns>The type of pushed value.</returns>
        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int LuaGetField(IntPtr luaState, int tableIndex, string key);

        /// <summary>
        ///     Pushes the value of the specified global to the top of the stack.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <param name="globalName">The name of the global.</param>
        /// <returns>The type of value the global holds.</returns>
        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int LuaGetGlobal(IntPtr luaState, string globalName);

        /// <summary>
        ///     Pushes the metatable for the table at the specified index in the stack (if it exists) to the top of the stack.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <param name="tableIndex">The table's stack index.</param>
        /// <returns><c>true</c> if the specified table has a metatable; otherwise <c>false</c>.</returns>
        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate bool LuaGetMetatable(IntPtr luaState, int tableIndex);

        /// <summary>
        ///     Gets information about the interpreter runtime stack.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <param name="level">The function level, 0 for the currently running function.</param>
        /// <param name="ar">
        ///     The Lua debug structure which is filled with the identification of the activation record of the
        ///     function running at the specified level.
        /// </param>
        /// <returns>1 if there are no errors, 0 if the level is greater than the stack depth.</returns>
        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int LuaGetStack(IntPtr luaState, int level, out LuaDebug ar);

        /// <summary>
        ///     Pushes the value of t[key] to the top of the stack where t is the table at the specified index and k is the key at
        ///     the top of the stack.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <param name="tableIndex">The table's stack index.</param>
        /// <returns>The type of pushed value.</returns>
        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int LuaGetTable(IntPtr luaState, int tableIndex);

        /// <summary>
        ///     Gets the stack index of the element at the top of the stack.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <returns>The stack index of the top element, where 0 indicates an empty stack.</returns>
        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int LuaGetTop(IntPtr luaState);

        /// <summary>
        ///     Gets the Lua type of the element at the specified index in the stack.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <param name="stackIndex">The stack index of the element.</param>
        /// <returns>The Lua type.</returns>
        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int LuaGetType(IntPtr luaState, int stackIndex);

        /// <summary>
        ///     Checks whether the value at the specified index in the stack is a number that can be represented by an integer.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <param name="stackIndex">The stack index of the element.</param>
        /// <returns><c>true</c> if the value is an integer; otherwise, <c>false</c>.</returns>
        /// <remarks>
        ///     Note that Lua does implicit conversions for floating point numbers. This means that a value of x.0 will be
        ///     rounded to x, thus making it an integer.
        /// </remarks>
        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate bool LuaIsInteger(IntPtr luaState, int stackIndex);

        /// <summary>
        ///     Checks whether the specified coroutine is yieldable.
        /// </summary>
        /// <param name="luaState">The coroutine state pointer.</param>
        /// <returns><c>true</c> if the coroutine is yieldable; otherwise, <c>false</c>.</returns>
        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate bool LuaIsYieldable(IntPtr luaState);

        /// <summary>
        ///     Loads the specified null terminated string as a Lua chunk. In case of errors and error message is pushed to the
        ///     stack, otherwise the chunk is pushed as a function to the top of the stack.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <param name="stringBytes">The encoded string.</param>
        /// <returns>An error code denoting the whether the load was successful.</returns>
        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int LuaLLoadString(IntPtr luaState, [In] byte[] stringBytes);

        /// <summary>
        ///     Creates a new metatable with the specified name in the registry.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <param name="name">The metatable's name.</param>
        /// <returns><c>false</c> if the registry contains a value with the key 'name'; otherwise,<c>true</c>.</returns>
        /// <remarks>
        ///     The final value associated with 'name' is pushed to the top of the stack regardless of the method's return
        ///     value.
        /// </remarks>
        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate bool LuaLNewMetatable(IntPtr luaState, string name);

        /// <summary>
        ///     Creates a new Lua state, pushes it onto the stack and returns a pointer to the state.
        /// </summary>
        /// <returns>The state, or <c>NULL</c> if memory allocation fails.</returns>
        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr LuaLNewState();

        /// <summary>
        ///     Loads the standard set of Lua libraries into the specified Lua state.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LuaLOpenLibs(IntPtr luaState);

        /// <summary>
        ///     Creates and returns a unique reference in the table at the specified index for the object at the top of the stack.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <param name="tableIndex">The table index.</param>
        /// <returns>An integer representing the reference key.</returns>
        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int LuaLRef(IntPtr luaState, int tableIndex);

        /// <summary>
        ///     Releases a reference from the table at the specified index.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <param name="tableIndex">The table index.</param>
        /// <param name="reference">The reference.</param>
        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LuaLUnref(IntPtr luaState, int tableIndex, int reference);

        /// <summary>
        ///     Creates a new thread (coroutine) and returns its handle.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <returns>The pointer to the newly created thread.</returns>
        /// <remarks>
        ///     Threads have an independent execution stack but share the global environment with the main state. Furthermore,
        ///     as any Lua object, threads are subject to GC. To prevent the GC from collecting new threads they have to be
        ///     anchored by creating a reference in the registry or any other table.
        /// </remarks>
        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr LuaNewThread(IntPtr luaState);

        /// <summary>
        ///     Allocates the specified amount of memory for full userdata and pushes it to the top of the stack. The function
        ///     returns the block address of the created userdata.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <param name="size">The required memory size to be allocated for userdata.</param>
        /// <returns>The userdata address.</returns>
        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr LuaNewUserdata(IntPtr luaState, UIntPtr size);

        /// <summary>
        ///     Pops a key from the stack and pushes a key-value pair (the next pair after the key) to the top of the stack for the
        ///     table at the specified index in the stack.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <param name="tableIndex">The table's stack index.</param>
        /// <returns>Zero if the table has no further key-value pairs.</returns>
        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int LuaNext(IntPtr luaState, int tableIndex);

        /// <summary>
        ///     Calls a function in protected mode, allowing the function to yield.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <param name="numberOfArguments">The number of arguments passed to the function.</param>
        /// <param name="numberOfResults">The number of results the function returns.</param>
        /// <param name="messageHandler">
        ///     If 0 in case of an error then the error object pushed to the stack is the original error object. Otherwise this
        ///     represents the stack index of a message handler which will be called with the error object and and its return value
        ///     will be the object returned on the stack by lua_pcall.
        /// </param>
        /// <param name="context">The context.</param>
        /// <param name="continuationFunction">The continuation function.</param>
        /// <returns>One of <see cref="LuaThreadStatus" /> codes.</returns>
        /// <remarks>
        ///     The function gets pushed to the stack first, followed by arguments being pushed in direct order. These objects
        ///     are popped from the stack when the function is called. After the function is called the results are pushed in
        ///     direct order and are adjusted to the specified number of results. In case of an error the function will push the
        ///     error object to the top of the stack and return the error code.
        /// </remarks>
        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int LuaPCallK(IntPtr luaState, int numberOfArguments,
                                      int numberOfResults = LuaLibrary.LUA_MULTRET, int messageHandler = 0, IntPtr context = new IntPtr(),
                                      IntPtr continuationFunction = new IntPtr());

        /// <summary>
        ///     Pushes the specified boolean value onto the stack.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <param name="boolValue">The value.</param>
        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LuaPushBoolean(IntPtr luaState, bool boolValue);

        /// <summary>
        ///     Pushes a C closure onto the stack.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <param name="luaCFunction">The C function.</param>
        /// <param name="n">The number of arguments associated with the function.</param>
        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LuaPushCClosure(IntPtr luaState, LuaCFunction luaCFunction, int n);

        /// <summary>
        ///     Pushes the specified 64-bit integer onto the stack.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <param name="number">The number.</param>
        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LuaPushInteger(IntPtr luaState, lua_Integer number);

        /// <summary>
        ///     Pushes the string represented by the encoded bytes and with the specified length.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <param name="stringBytes">The encoded bytes.</param>
        /// <param name="length">The length of the string.</param>
        /// <returns>A pointer to a public copy of the string.</returns>
        /// <remarks>
        ///     Lua creates a public copy of the string which allows continuous reuse while allowing memory to be freed as soon
        ///     as the function that invoked the delegate returns. This function also allows strings to contain any binary data.
        /// </remarks>
        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr LuaPushLString(IntPtr luaState, [In] byte[] stringBytes, UIntPtr length);

        /// <summary>
        ///     Pushes a nil value onto the stack.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LuaPushNil(IntPtr luaState);

        /// <summary>
        ///     Pushes the specified floating point number onto the stack. Lua uses doubles by default.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <param name="number">The number.</param>
        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LuaPushNumber(IntPtr luaState, double number);

        /// <summary>
        ///     Pushes a copy of the value at the specified index to the top of the stack.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <param name="stackIndex">The stack index of the element.</param>
        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LuaPushValue(IntPtr luaState, int stackIndex);

        /// <summary>
        ///     Pushes onto the stack the value t[elementIndex], where t is the table at the given index.
        ///     The access is raw, that is, it does not invoke the __index metamethod.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <param name="tableIndex">The table index.</param>
        /// <param name="elementIndex">The element index.</param>
        /// <returns>The type of the pushed value.</returns>
        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int LuaRawGetI(IntPtr luaState, int tableIndex, lua_Integer elementIndex);

        /// <summary>
        ///     Resumes the specified Lua coroutine.
        /// </summary>
        /// <param name="coroutineState">The coroutine's state pointer.</param>
        /// <param name="fromCoroutineState">A pointer to the coroutine that is resuming the specified coroutine. None by default.</param>
        /// <param name="nargs">The number of arguments to be pushed to the resume call.</param>
        /// <param name="nresults">The number of results the coroutine returns.</param>
        /// <returns>
        ///     <see cref="LuaThreadStatus.LUA_OK" /> if the coroutine executes without errors,
        ///     <see cref="LuaThreadStatus.LUA_YIELD" /> if the coroutine yields or an error code in case of errors.
        /// </returns>
        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int LuaResume(IntPtr coroutineState, IntPtr fromCoroutineState, int nargs, out int nresults);

        /// <summary>
        ///     Pops a value from the stack and pushes it as a global with the specified name.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <param name="globalName">The name of the global to push.</param>
        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LuaSetGlobal(IntPtr luaState, string globalName);

        /// <summary>
        ///     Sets the debugging hook function.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <param name="hookFunction">The callback function.</param>
        /// <param name="hookMask">
        ///     The hook mask. The mask can be formed by a bitwise OR performed on the <see cref="EventMask" />
        ///     enum.
        /// </param>
        /// <param name="count">
        ///     The number of instructions required for Lua to execute the hook. This parameter has no effect when
        ///     the <see cref="EventMask.LUA_MASKCOUNT" /> mask is not specified.
        /// </param>
        /// <remarks>In order to detach a hook the hook mask must be set to zero.</remarks>
        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LuaSetHook(IntPtr luaState, LuaHook hookFunction, int hookMask, int count = 0);

        /// <summary>
        ///     Pops a table from the stack and sets it as the metatable for the table at the specified index in the stack.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <param name="tableIndex">The table's stack index.</param>
        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LuaSetMetatable(IntPtr luaState, int tableIndex);

        /// <summary>
        ///     Sets the value of the element associated to the specified table and key.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <param name="tableIndex">The table index.</param>
        /// <remarks>The value is located at the top of the stack, and the key is at the penultimate index in the stack.</remarks>
        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LuaSetTable(IntPtr luaState, int tableIndex);

        /// <summary>
        ///     Accepts any index, or 0, and sets the stack top to this index.
        ///     If the new top is larger than the old one, then the new elements are filled with nil.
        ///     If the index is 0, then all stack elements are removed.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <param name="index">The index.</param>
        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LuaSetTop(IntPtr luaState, int index);

        /// <summary>
        ///     Gets the status of the specified Lua thread.
        /// </summary>
        /// <param name="threadState">The thread state pointer.</param>
        /// <returns>
        ///     <see cref="LuaThreadStatus.LUA_OK" /> for a normal thread, <see cref="LuaThreadStatus.LUA_YIELD" /> if the thread
        ///     is
        ///     suspended or an error code if the thread finished its execution with an error.
        /// </returns>
        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int LuaStatus(IntPtr threadState);

        /// <summary>
        ///     Converts the value at the specified index in the stack to its boolean equivalent.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <param name="stackIndex">The stack index of the element.</param>
        /// <returns>A C-like boolean value (0 or 1).</returns>
        /// <remarks>
        ///     This function uses the standard Lua convention for booleans, i.e anything that is not <c>nil</c> or
        ///     <c>false</c> is treated as <c>true</c>.
        /// </remarks>
        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate bool LuaToBoolean(IntPtr luaState, int stackIndex);

        /// <summary>
        ///     Converts the value at the specified index in the stack to a signed integer. The value must be either an integer
        ///     itself or a string that can be converted to an integer.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <param name="stackIndex">The stack index of the element.</param>
        /// <param name="isNum">
        ///     When not <c>null</c>, the referent holds a C boolean value indicating whether the conversion
        ///     succeeded.
        /// </param>
        /// <returns>The integer.</returns>
        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate lua_Integer LuaToIntegerX(IntPtr luaState, int stackIndex, out IntPtr isNum);

        /// <summary>
        ///     Converts the value at the specified index in the stack to a string. The value must be either a string itself or an
        ///     integer.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <param name="stackIndex">The stack index of the element.</param>
        /// <param name="length">The length of the string.</param>
        /// <returns>A pointer to the string.</returns>
        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr LuaToLString(IntPtr luaState, int stackIndex, out UIntPtr length);

        /// <summary>
        ///     Converts the value at the specified index in the stack to a number. The value must be either a number itself or a
        ///     string that can be converted to a number.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <param name="stackIndex">The stack index of the element.</param>
        /// <param name="isNum">
        ///     When not <c>null</c>, the referent holds a C boolean value indicating whether the conversion
        ///     succeeded.
        /// </param>
        /// <returns>The number.</returns>
        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double LuaToNumberX(IntPtr luaState, int stackIndex, out IntPtr isNum);

        /// <summary>
        ///     Converts the value at the specified index in the stack to a generic C pointer.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <param name="stackIndex">The stack index of the element.</param>
        /// <returns>A generic C pointer for the value.</returns>
        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr LuaToPointer(IntPtr luaState, int stackIndex);

        /// <summary>
        ///     Converts the element at the specified index in the stack to a Lua thread and returns its handle.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <param name="stackIndex">The thread's stack index.</param>
        /// <returns>The thread's handle, or <c>NULL</c> if the element at the specified index is not a Lua thread.</returns>
        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr LuaToThread(IntPtr luaState, int stackIndex);

        /// <summary>
        ///     Converts the value at the specified index in the stack to userdata. If it is full userdata the method returns its
        ///     block address, otherwise returns the userdata's pointer.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <param name="stackIndex">The stack index of the element.</param>
        /// <returns>The block address for full userdata, a pointer for light userdata.</returns>
        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr LuaToUserdata(IntPtr luaState, int stackIndex);

        /// <summary>
        ///     Pops the specified number of arguments from the stack of <c>fromThreadState</c> and pushes them onto the stack of
        ///     <c>toThreadState</c>.
        /// </summary>
        /// <param name="fromThreadState">The Lua state pointer of the state holding the arguments.</param>
        /// <param name="toThreadState">The Lua state pointer of the state that's requesting the arguments.</param>
        /// <param name="nargs">The number of arguments to transfer between the states.</param>
        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LuaXMove(IntPtr fromThreadState, IntPtr toThreadState, int nargs);
    }
}