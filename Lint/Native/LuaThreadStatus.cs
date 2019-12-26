using System;
using System.Diagnostics.CodeAnalysis;

namespace Lint.Native
{
    /// <summary>
    ///     Specifies the status of a Lua thread. These values are constants pulled from the lua.h file.
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification =
        "Error message names are pulled directly from Lua's reference manual and are kept in tact.")]
    public enum LuaThreadStatus
    {
        /// <summary>
        ///     Represents the normal status for a thread. When resuming threads with <see cref="LUA_OK" /> a new coroutine is
        ///     started, whereas <see cref="LUA_YIELD" /> resumes a couroutine.
        /// </summary>
        LUA_OK = 0,

        /// <summary>
        ///     Represents a suspended thread.
        /// </summary>
        LUA_YIELD = 1,

        /// <summary>
        ///     Represents a runtime error.
        /// </summary>
        LUA_ERRRUN = 2,

        /// <summary>
        ///     Represents a syntax error.
        /// </summary>
        LUA_ERRSYNTAX = 3,

        /// <summary>
        ///     Represents a memory allocation error.
        /// </summary>
        LUA_ERRMEM = 4,

        /// <summary>
        ///     Represents an error thrown while running the message handler.
        /// </summary>
        LUA_ERRERR = 5,

        /// <summary>
        ///     Represents an error thrown during the execution of a __gc metamethod.
        /// </summary>
        [Obsolete] LUA_ERRGCMM = 6
    }
}