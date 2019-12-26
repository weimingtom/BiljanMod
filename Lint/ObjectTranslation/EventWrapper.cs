using System;
using System.Collections.Generic;
using System.Reflection;
using Lint.Exceptions;
using Lint.Native;

namespace Lint.ObjectTranslation
{
    /// <summary>
    ///     Represents a .NET event wrapper.
    /// </summary>
    internal sealed class EventWrapper
    {
        /// <summary>
        ///     Gets a mapping of Lua functions to their corresponding event handler delegates.
        /// </summary>
        /// <remarks>
        ///     This field is static due to the fact that the .NET translator pushes a new <see cref="EventWrapper" /> to
        ///     the top of the stack each time an event is indexed. As non-static fields are instance specific, the event handler
        ///     list would always be empty, which would compromise deregistration.
        /// </remarks>
        private static readonly Dictionary<LuaFunction, Delegate> EventHandlers =
            new Dictionary<LuaFunction, Delegate>();

        /// <summary>
        ///     Gets the underlying event information.
        /// </summary>
        private readonly EventInfo _event;

        /// <summary>
        ///     Gets the object on which the underlying event is invoked.
        /// </summary>
        private readonly object _objectInstance;

        /// <summary>
        ///     Initializes a new instance of the <see cref="EventWrapper" /> class with the specified class instance and event
        ///     information.
        /// </summary>
        /// <param name="instance">The object instance on which the event is invoked, or <c>null</c> for static events.</param>
        /// <param name="eventInfo">The event information, which must not be <c>null</c>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="eventInfo" /> is <c>null</c>.</exception>
        public EventWrapper(object instance, EventInfo eventInfo)
        {
            _objectInstance = instance;
            _event = eventInfo;
        }

        /// <summary>
        ///     Removes the provided Lua function from the underlying event's event handler list.
        /// </summary>
        /// <param name="luaFunction">The Lua function, which must not be <c>null</c>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="luaFunction" /> is <c>null</c>.</exception>
        public void Deregister(LuaFunction luaFunction)
        {
            if (luaFunction == null)
            {
                throw new ArgumentNullException("luaFunction");
            }

            Delegate handler;
            if (!EventHandlers.TryGetValue(luaFunction, out handler))
            {
                return;
            }

            try
            {
                _event.RemoveEventHandler(_objectInstance, handler);
            }
            catch (TargetInvocationException ex)
            {
                throw new LuaException("An exception has occured while removing an event handler: " + ex + "");
            }

            EventHandlers.Remove(luaFunction);
        }

        /// <summary>
        ///     Registers the provided Lua function as a .NET event handler.
        /// </summary>
        /// <param name="luaFunction">The Lua function, which must not be <c>null</c>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="luaFunction" /> is <c>null</c>.</exception>
        public void Register(LuaFunction luaFunction)
        {
            if (luaFunction == null)
            {
                throw new ArgumentNullException("luaFunction");
            }

            var eventHandlerType = _event.EventHandlerType;
            if (eventHandlerType != typeof(EventHandler) && eventHandlerType != typeof(EventHandler<>))
            {
                throw new LuaException("Cannot hook event with a non 'void(object, TEventArgs)' signature.");
            }

            // Type arguments have to be interpretable at compile time, so we have to resort to reflection
            var eventArgsType = (eventHandlerType.GetMethod("Invoke") != null ? eventHandlerType.GetMethod("Invoke").GetParameters()[1].ParameterType : null);
            var constructedWrapperType = typeof(LuaFunctionWrapper<>).MakeGenericType(eventArgsType);
            var luaFunctionWrapper = Activator.CreateInstance(constructedWrapperType, luaFunction);
            var @delegate = Delegate.CreateDelegate(_event.EventHandlerType, luaFunctionWrapper, "Call");
            try
            {
                _event.AddEventHandler(_objectInstance, @delegate);
                EventHandlers[luaFunction] = @delegate;
            }
            catch (TargetInvocationException ex)
            {
                throw new LuaException("An exception has occured while adding an event handler: " + ex + "");
            }
        }
    }
}