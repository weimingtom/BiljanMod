using System;
using System.Diagnostics.CodeAnalysis;

namespace Lint.Native.LuaHooks
{
    /// <summary>
    ///     Specifies the hook mask of a Lua event. These values are constants pulled from the lua.h file.
    /// </summary>
    [Flags]
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification =
        "Error message names are pulled directly from Lua's reference manual and are kept in tact.")]
    public enum EventMask
    {
        /// <summary>
        ///     The hook is called when Lua calls a function, right before the function gets its arguments.
        /// </summary>
        LUA_MASKCALL = 1 << EventCode.LUA_HOOKCALL,

        /// <summary>
        ///     The hook is called when Lua is about to exit a function.
        /// </summary>
        LUA_MASKRET = 1 << EventCode.LUA_HOOKRET,

        /// <summary>
        ///     The hook is called when Lua is about to execute a new line of code, or when it jumps back in code.
        /// </summary>
        LUA_MASKLINE = 1 << EventCode.LUA_HOOKLINE,

        /// <summary>
        ///     The hook is called when Lua executes a certain number of instructions.
        /// </summary>
        LUA_MASKCOUNT = 1 << EventCode.LUA_HOOKCOUNT
    }
}