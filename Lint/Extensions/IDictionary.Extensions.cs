using System;
using System.Collections.Generic;

namespace Lint.Extensions
{
    /// <summary>
    ///     Provides extension methods for the <see cref="IDictionary{TKey,TValue}" /> type.
    /// </summary>
    public static class IDictionaryExtensions
    {
        /// <summary>
        ///     Deconstructs the specified <see cref="KeyValuePair{TKey, TValue}" />.
        /// </summary>
        /// <typeparam name="TKey">The type of key.</typeparam>
        /// <typeparam name="TValue">The type of value.</typeparam>
        /// <param name="kvp">The <see cref="KeyValuePair{TKey, TValue}" /> to deconstruct.</param>
        /// <param name="key">The deconstructed key</param>
        /// <param name="value">The deconstructed value.</param>
        public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> kvp, out TKey key,
            out TValue value)
        {
            key = kvp.Key;
            value = kvp.Value;
        }

        /// <summary>
        ///     Gets a value mapped to the specified key in the dictionary. If the key does not exist the specified default value
        ///     is returned.
        /// </summary>
        /// <typeparam name="TKey">The type of key.</typeparam>
        /// <typeparam name="TValue">The type of value.</typeparam>
        /// <param name="dictionary">The dictionary, which must not be <c>null</c>.</param>
        /// <param name="key">The key, which must not be <c>null</c>.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>
        ///     The value associated with the specified key if the key exists, otherwise returns the
        ///     <paramref name="defaultValue" />.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary" /> is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="key" /> is <c>null</c>.</exception>
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue /*= defult*/)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException("dictionary");
            }

            if (key == null)
            {
                throw new ArgumentNullException("key");
            }
			
            TValue value;
            return dictionary.TryGetValue(key, out value) ? value : defaultValue;
        }
    }
}