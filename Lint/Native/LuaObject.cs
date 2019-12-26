using System;

namespace Lint.Native
{
    /// <summary>
    ///     Represents the base class for Lua objects (tables, functions, and threads).
    /// </summary>
    public abstract class LuaObject : MarshalByRefObject
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="LuaObject" /> class with the specified state and reference key.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="reference">The reference key.</param>
        protected LuaObject(IntPtr state, int reference)
        {
            State = state;
            Reference = reference;
        }

        /// <summary>
        ///     Gets the reference to this object. References are used to identify various objects in the registry.
        /// </summary>
        protected int Reference { get; private set; }

        /// <summary>
        ///     Gets the Lua state for this Lua object.
        /// </summary>
        protected IntPtr State { get; private set; }

        /// <summary>
        ///     Pushes the current object to the top of the stack of the specified Lua state.
        /// </summary>
        /// <param name="luaState">The Lua state pointer, which defaults to the parent state.</param>
        public virtual void PushToStack(IntPtr? luaState = null)
        {
            LuaLibrary.LuaRawGetI(luaState ?? State, (int) LuaRegistry.RegistryIndex, Reference);
        }
    }
}