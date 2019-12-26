using System.Diagnostics.CodeAnalysis;

namespace Lint.Native.LuaHooks
{
    /// <summary>
    ///     Specifies the event code of a Lua event. These values are constants pulled from the lua.h file.
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification =
        "Error message names are pulled directly from Lua's reference manual and are kept in tact.")]
    public enum EventCode
    {
        /// <summary>
        ///     The call event code. Used when Lua calls a function.
        /// </summary>
        LUA_HOOKCALL = 0,

        /// <summary>
        ///     The ret event code. Used when Lua returns from a function.
        /// </summary>
        LUA_HOOKRET = 1,

        /// <summary>
        ///     The line event code. Used when Lua executes a line of code.
        /// </summary>
        LUA_HOOKLINE = 2,

        /// <summary>
        ///     The count event code. Used when Lua executes a specified number of instructions.
        /// </summary>
        LUA_HOOKCOUNT = 3,

        /// <summary>
        ///     The tail call event code.
        /// </summary>
        LUA_HOOKTAILCALL = 4
    }
}