using System;
using System.Text;

namespace Lint.Extensions
{
    /// <summary>
    ///     Provides extension methods for the <see cref="string" /> type.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        ///     Encodes the string using UTF-8 encoding and returns the encoded byte array.
        /// </summary>
        /// <param name="source">The source string, which must not be <c>null</c>.</param>
        /// <returns>The encoded byte array.</returns>
        public static byte[] GetEncodedBytes(this string source)
        {
            var buffer = new byte[Encoding.UTF8.GetByteCount(source)];
            Encoding.UTF8.GetBytes(source, 0, source.Length, buffer, 0);
            return buffer;
        }
    }
}