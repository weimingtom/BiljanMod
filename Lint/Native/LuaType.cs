namespace Lint.Native
{
    /// <summary>
    ///     Specifies a Lua type. These values are constants pulled from the lua.h file.
    /// </summary>
    public enum LuaType
    {
        /// <summary>
        ///     Represents no type.
        /// </summary>
        None = -1,

        /// <summary>
        ///     Represents absence of useful value.
        /// </summary>
        Nil = 0,

        /// <summary>
        ///     Represents a boolean value (true or false). Lua considers <c>nil</c> and <c>false</c> as false, everything else is
        ///     <c>true</c>.
        /// </summary>
        Boolean = 1,

        /// <summary>
        ///     Represents light userdata.
        /// </summary>
        LightUserdata = 2,

        /// <summary>
        ///     Represents a number. Lua 5.3 uses two representations for numbers: 64-bit integers and double-precision
        ///     floating-point numbers. Lua also provides 32-bit integer support when compiled as 'Small Lua'.
        /// </summary>
        Number = 3,

        /// <summary>
        ///     Represents a series of characters.
        /// </summary>
        String = 4,

        /// <summary>
        ///     Represents a Lua table.
        /// </summary>
        Table = 5,

        /// <summary>
        ///     Represents a Lua function.
        /// </summary>
        Function = 6,

        /// <summary>
        ///     Represents a type used to store arbitrary C data. This is used to store types provided by a C application.
        /// </summary>
        Userdata = 7,

        /// <summary>
        ///     Represents a Lua thread.
        /// </summary>
        Thread = 8
    }
}