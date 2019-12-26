using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Lint.Extensions
{
    /// <summary>
    ///     Provides extension methods for the <see cref="Type" /> type.
    /// </summary>
    public static class TypeExtensions
    {
        /// <summary>
        ///     Gets the metadata cache.
        /// </summary>
        private static readonly Dictionary<Type, TypeMetadata> MetadataCache =
            new Dictionary<Type, TypeMetadata>();

        /// <summary>
        ///     Gets the extension methods for the specified type.
        /// </summary>
        /// <param name="type">The type, which must not be <c>null</c>.</param>
        /// <returns>An enumerable collection of extension methods.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="type" /> is <c>null</c>.</exception>
        public static IEnumerable<MethodInfo> GetExtensionMethods(this Type type)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var extensionMethods = from a in assemblies
                let types = a.GetTypes()
                from t in types
                where Attribute.IsDefined(t, typeof(ExtensionAttribute))
                from m in t.GetMethods(BindingFlags.Public | BindingFlags.Static)
                where m.IsExtensionMethod() && m.GetParameters().ElementAt(0) != null && m.GetParameters().ElementAt(0).ParameterType == type
                select m;

          
            return extensionMethods;
        }

        /// <summary>
        ///     Gets or creates type metadata for the specified type.
        /// </summary>
        /// <param name="type">The type, which must not be <c>null</c>.</param>
        /// <returns>The metadata.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="type" /> is <c>null</c>.</exception>
        public static TypeMetadata GetOrCreateMetadata(this Type type)
        {
        	TypeMetadata metadata;
            if (!MetadataCache.TryGetValue(type, out metadata))
            {
                metadata = TypeMetadata.Create(type);
                MetadataCache.Add(type, metadata);
            }

            return metadata;
        }

        /// <summary>
        ///     Checks whether the type object is a numeric type.
        /// </summary>
        /// <param name="type">The type, which must not be <c>null</c>.</param>
        /// <returns><c>true</c> if the type is numeric; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="type" /> is <c>null</c>.</exception>
        public static bool IsNumeric(this Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.SByte:
                case TypeCode.Single:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;
                default:
                    return false;
            }
        }
    }
}