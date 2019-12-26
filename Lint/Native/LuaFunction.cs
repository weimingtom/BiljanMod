//#nullable enable
using System;

namespace Lint.Native
{
    /// <summary>
    ///     Represents a Lua function.
    /// </summary>
    public sealed class LuaFunction : LuaObject
    {
        /// <inheritdoc />
        internal LuaFunction(IntPtr state, int reference) : base(state, reference)
        {
        }

        /// <summary>
        ///     Calls the function using the provided arguments and returns the results.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <param name="numberOfResults">The number of results to return.</param>
        /// <returns>The results.</returns>
        public object[] Call(object[] args = null, int numberOfResults = LuaLibrary.LUA_MULTRET)
        {
            PushToStack(); // ProtectedCallK assumes that there is a function at the top of the stack
            return LuaLibrary.CallWithArguments(State, args, numberOfResults);
        }
    }
}