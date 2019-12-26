//#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lint.ObjectTranslation;

namespace Lint.Native
{
    // TODO: Handle metamethods (?)

    /// <summary>
    ///     Represents a Lua table.
    /// </summary>
    /// <remarks>
    ///     Tables are the closest Lua gets to objects. They are a fundamental part of most Lua applications. They are
    ///     used as both arrays (which use list-style initialization) and dictionaries (which use record-style initialization).
    ///     Table constructors allow a mixture of both initialization styles (such as the following:
    ///     <c>local table = {x=10, y=45; "one", "two", "three"}</c>). In this example, the first two values are represented by
    ///     keys "x" and "y" (respectively), the remaining values are automatically assigned a one-based index (Lua's indices
    ///     always start at 1). This means that all tables are essentially dictionaries. Due to this we implement the
    ///     <see cref="IDictionary{TKey,TValue}" /> interface to provide a proper mechanism for dealing with table pairs
    ///     (there has to be a way to push values to the stack which cannot be achieved without overriding the dictionary's
    ///     methods).
    /// </remarks>
    public sealed class LuaTable : LuaObject, IDictionary<string, object>
    {
        private readonly KeyCollection _keyCollection;
        private readonly ValueCollection _valueCollection;

        /// <summary>
        ///     Initializes a new instance of the <see cref="LuaTable" /> class with the specified Engine instance and
        ///     reference.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="reference">The reference.</param>
        internal LuaTable(IntPtr state, int reference) : base(state, reference)
        {
            _keyCollection = new KeyCollection(this);
            _valueCollection = new ValueCollection(this);
        }

        /// <summary>
        ///     Gets or sets the current table's metatable.
        /// </summary>
        public LuaTable Metatable
        {
            get
            {
                PushToStack(); // Push the table to the top of the stack
                var @object = LuaLibrary.LuaGetMetatable(State, -1) // Get the object
                    ? (LuaTable) ObjectTranslator.GetObject(State, -1)
                    : null;
                LuaLibrary.LuaPop(State, 1); // Pop the object
                return @object; // Return the object
            }
            set
            {
                if (value == null)
                {
                    ObjectTranslator.PushToStack(State, null);
                }
                else
                {
                    value.PushToStack(State); // Push the metatable to the top of the stack
                }

                PushToStack(State); // Push the table to the top of the stack
                LuaLibrary.LuaSetMetatable(State, -2); // Set the metatable
            }
        }

        /// <inheritdoc />
        public ICollection<string> Keys {get{return _keyCollection;}}

        /// <inheritdoc />
        public ICollection<object> Values {get{return _valueCollection;}}

        /// <inheritdoc />
        public bool IsReadOnly {get{return false;}}

        /// <inheritdoc />
        public object this[string key]
        {
            get
            {
                PushToStack(); // Push the current table to the top of the stack
                ObjectTranslator.PushToStack(State, key); // Push the key to the top of the stack
                LuaLibrary.LuaGetTable(State, -2); // The table is now at the penultimate index
                var obj = ObjectTranslator.GetObject(State, -1); // Get the object
                LuaLibrary.LuaPop(State, 1); // Finally, remove the pushed value
                return obj;
            }
            set
            {
                PushToStack(); // Push the current table to the top of the stack
                ObjectTranslator.PushToStack(State, key); // Push the key to the top of the stack
                ObjectTranslator.PushToStack(State, value); // Push the value to the top of the stack
                LuaLibrary.LuaSetTable(State, -3); // Store the value under the specified key
            }
        }

        /// <inheritdoc />
        public bool ContainsKey(string key) { return this[key] != null; }

        /// <inheritdoc />
        public void Add(string key, object value)
        {
            this[key] = value;
        }

        /// <inheritdoc />
        public bool Remove(string key)
        {
            this[key] = null;
            return true;
        }

        /// <inheritdoc />
        public bool TryGetValue(string key, out object value) { return (value = this[key]) != null; }

        /// <inheritdoc />
        public void Add(KeyValuePair<string, object> item)
        {
            Add(item.Key, item.Value);
        }

        /// <inheritdoc />
        public void Clear()
        {
            foreach (var key in Keys)
            {
                this[key] = null;
            }
        }

        /// <inheritdoc />
        public bool Contains(KeyValuePair<string, object> item) {return ContainsKey(item.Key);}

        /// <inheritdoc />
        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            //if (array == null)
            //{
            //    throw new ArgumentNullException(nameof(array));
            //}

            //if (arrayIndex < 0 || arrayIndex > array.Length)
            //{
            //    throw new ArgumentOutOfRangeException(nameof(arrayIndex),
            //        "The starting index may not be negative or greater than the size of the array.");
            //}

            //if (Keys.Count > array.Length - arrayIndex)
            //{
            //    throw new ArgumentException(nameof(arrayIndex),
            //        "The starting index is too large and the contents of the dictionary cannot be copied to the designated array.");
            //}

            //((ICollection) _dictionary).CopyTo(array, arrayIndex);
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public bool Remove(KeyValuePair<string, object> item) {return Remove(item.Key);}

        /// <inheritdoc />
        public int Count
        {
            get
            {
                PushToStack();

                var count = 0;
                ObjectTranslator.PushToStack(State, null);
                while (LuaLibrary.LuaNext(State, -2) != 0)
                {
                    ++count;
                    LuaLibrary.LuaPop(State, 1);
                }

                LuaLibrary.LuaPop(State, 1);
                return count;
            }
        }

        // TODO: Table tree traversal []

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            PushToStack(); // Push the current table to the top of the stack
            ObjectTranslator.PushToStack(State, null); // Push the "first key"
            while (LuaLibrary.LuaNext(State, -2) != 0) // The table is at the penultimate index now
            {
                LuaLibrary.LuaPushValue(State, -2); // Push a copy of the key
                var value = ObjectTranslator.GetObject(State, -2); // Get the value (keys go first)
                var key = LuaLibrary.LuaToString(State, -1);

                if (value is LuaTable) // Table tree traversal is done with recursion
                {
                	LuaTable table = value as LuaTable;
                    foreach (var kvp in table)
                    {
                        yield return kvp;
                    }
                }

                // Pop the key/value pair and preserve the original key for further processing
                LuaLibrary.LuaPop(State, 2);
                yield return new KeyValuePair<string, object>(key, value);
            }
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() {return GetEnumerator();}

        /// <summary>
        ///     Inserts a range of key-value pairs into the table.
        /// </summary>
        /// <param name="keyValuePairs">The array of key-value pairs, which must not be <c>null</c>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="keyValuePairs" /> is <c>null</c>.</exception>
        public void AddRange(params KeyValuePair<string, object>[] keyValuePairs)
        {
            foreach (var kvp in keyValuePairs)
            {
                Add(kvp);
            }
        }

        /// <summary>
        ///     Sets the metatable for the current table instance.
        /// </summary>
        /// <param name="metatable">The metatable.</param>
        /// <returns>The modified table.</returns>
        public LuaTable WithMetatable(LuaTable metatable)
        {
            Metatable = metatable;
            return this;
        }
    }

    internal abstract class MyCollection<T> : ICollection<T>
    {
        protected readonly IDictionary<string, object> Dictionary; // This is actually the Lua table

        protected MyCollection(IDictionary<string, object> dictionary)
        {
            Dictionary = dictionary;
        }

        /// <inheritdoc />
        public abstract IEnumerator<T> GetEnumerator();

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() {return GetEnumerator();}

        /// <inheritdoc />
        public void Add(T item)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void Clear()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public abstract bool Contains(T item);

        /// <inheritdoc />
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }

            if (arrayIndex < 0 || arrayIndex >= array.Length)
            {
                throw new ArgumentOutOfRangeException("arrayIndex",
                    "The starting index must not be negative or greater than the size of the array.");
            }

            if (Count > array.Length - arrayIndex)
            {
                throw new ArgumentException(
                    "The starting index is too large and the content of the dictionary does not fit into the designated array.",
                    "arrayIndex");
            }

            foreach (var item in this)
            {
                array[arrayIndex++] = item;
            }
        }

        /// <inheritdoc />
        public bool Remove(T item) { throw new NotImplementedException();}

        /// <inheritdoc />
        public abstract int Count { get; }

        /// <inheritdoc />
        public bool IsReadOnly {get{return true;}}
    }

    internal class KeyCollection : MyCollection<string>
    {
        /// <inheritdoc />
        public KeyCollection(IDictionary<string, object> dictionary) : base(dictionary)
        {
        }

        /// <inheritdoc />
        public override int Count {get{return Dictionary.Count;}}

        /// <inheritdoc />
        public override bool Contains(string item)
        {
            return Dictionary.Keys.Any(key => EqualityComparer<string>.Default.Equals(key, item));
        }

        /// <inheritdoc />
        public override IEnumerator<string> GetEnumerator()
        {
            return Dictionary.Select(kvp => kvp.Key).GetEnumerator();
        }
    }

    internal class ValueCollection : MyCollection<object>
    {
        /// <inheritdoc />
        public ValueCollection(IDictionary<string, object> dictionary) : base(dictionary)
        {
        }

        /// <inheritdoc />
        public override int Count {get{return Dictionary.Count;}}

        /// <inheritdoc />
        public override bool Contains(object item)
        {
            return Dictionary.Values.Any(value => EqualityComparer<object>.Default.Equals(value, item));
        }

        /// <inheritdoc />
        public override IEnumerator<object> GetEnumerator()
        {
            return Dictionary.Select(kvp => kvp.Value).GetEnumerator();
        }
    }
}