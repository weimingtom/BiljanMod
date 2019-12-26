using System;

namespace Lint.Exceptions
{
    /// <summary>
    ///     Represents a Lua script exception.
    /// </summary>
    [Serializable]
    public sealed class LuaScriptException : LuaException
    {
        /// <inheritdoc />
        public LuaScriptException(string message) : base(message)
        {
        }

        /// <inheritdoc />
        public LuaScriptException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}