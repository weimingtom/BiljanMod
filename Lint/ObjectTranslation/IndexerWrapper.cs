using System;
using System.Reflection;
using Lint.Exceptions;

namespace Lint.ObjectTranslation
{
    /// <summary>
    ///     Represents an indexer wrapper.
    /// </summary>
    internal sealed class IndexerWrapper
    {
        /// <summary>
        ///     Gets the object instance on which the underlying property gets indexed.
        /// </summary>
        private readonly object _objectInstance;

        /// <summary>
        ///     Gets the underlying property information.
        /// </summary>
        private readonly PropertyInfo _property;

        /// <summary>
        ///     Initializes a new instance of the <see cref="IndexerWrapper" /> class with the specified class instance and
        ///     property information.
        /// </summary>
        /// <param name="instance">The object instance on which the indexer is invoked.</param>
        /// <param name="propertyInfo">The property information.</param>
        /// <exception cref="ArgumentNullException"><paramref name="propertyInfo" /> is <c>null</c>.</exception>
        public IndexerWrapper(object instance, PropertyInfo propertyInfo)
        {
            _objectInstance = instance;
            _property = propertyInfo;
        }

        /// <summary>
        ///     Gets the value of the underlying property using the specified indices.
        /// </summary>
        /// <param name="indices">The indices, which must not be <c>null</c>.</param>
        /// <returns>The indexed object.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="indices" /> is <c>null</c>.</exception>
        public object Get(params object[] indices)
        {
            if (indices == null)
            {
                throw new ArgumentNullException("indices");
            }

            try
            {
                return _property.GetValue(_objectInstance, indices);
            }
            catch (TargetInvocationException ex)
            {
                throw new LuaException("An exception has occured while reading an indexer: " + ex + "");
            }
        }

        /// <summary>
        ///     Sets the property value stored under the specified index.
        /// </summary>
        /// <param name="value">The new property value.</param>
        /// <param name="indices">The indices.</param>
        /// <exception cref="ArgumentNullException"><paramref name="indices" /> is <c>null</c>.</exception>
        public void Set(object value, params object[] indices)
        {
            if (indices == null)
            {
                throw new LuaException("indices");
            }

            try
            {
                _property.SetValue(_objectInstance, value, indices);
            }
            catch (TargetInvocationException ex)
            {
                throw new LuaException("An exception has occured while setting an indexer value: " + ex + "");
            }
        }
    }
}