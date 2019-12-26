using System;
using Lint.Native;

namespace Lint.ObjectTranslation
{
    /// <summary>
    ///     Represents a <see cref="LuaFunction" /> wrapper.
    /// </summary>
    /// <typeparam name="TEventArgs">The type of event arguments the wrapped function encapsulates.</typeparam>
    /// <remarks>
    ///     The wrapper acts as a proxy event handler which is invoked with the appropriate arguments (of
    ///     <typeparamref name="TEventArgs" /> type) once the underlying event fires.
    ///     Thought: ideally, <typeparamref name="TEventArgs" /> should have an <see cref="EventArgs" /> constraint. However,
    ///     this could break third party libraries that do not follow MS's event conventions.
    /// </remarks>
    internal sealed class LuaFunctionWrapper<TEventArgs> where TEventArgs : class
    {
        /// <summary>
        ///     Gets the Lua function.
        /// </summary>
        private readonly LuaFunction _luaFunction;

        /// <summary>
        ///     Initializes a new instance of the <see cref="LuaFunctionWrapper{TEventArgs}" /> class with the specified
        ///     <see cref="LuaFunction" /> instance.
        /// </summary>
        /// <param name="luaFunction">The Lua function, which must not be <c>null</c>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="luaFunction" /> is <c>null</c>.</exception>
        public LuaFunctionWrapper(LuaFunction luaFunction)
        {
            _luaFunction = luaFunction;
        }

        /// <summary>
        ///     Calls the wrapped function using the specified event handler arguments.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The arguments.</param>
        public void Call(object sender, TEventArgs args)
        {
            _luaFunction.Call(new[] {sender, args});
        }
    }
}