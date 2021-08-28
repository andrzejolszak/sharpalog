using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sharpen;

namespace Sharplog.Engine
{
    /// <summary>
    /// Subclass of
    /// <see cref="System.Collections.Generic.HashSet<object>{E}"/>
    /// that can quickly access a subset of its elements through an index.
    /// Jatalog uses it to quickly retrieve the facts with a specific predicate.
    /// </summary>
    /// <?/>
    /// <?/>
    public class IndexedSet<E> : IEnumerable<E>
        where E : Indexable
    {
        private HashSet<E> contents;

        private IDictionary<int, HashSet<E>> index;

        /// <summary>Default constructor.</summary>
        public IndexedSet()
        {
            index = new Dictionary<int, HashSet<E>>();
            contents = new HashSet<E>();
        }

        public virtual int Count
        {
            get
            {
                return contents.Count;
            }
        }

        /// <summary>
        /// Retrieves the subset of the elements in the set with the
        /// specified index.
        /// </summary>
        /// <param name="key">The indexed element</param>
        /// <returns>The specified subset</returns>
        public virtual HashSet<E> GetIndexed(int key)
        {
            HashSet<E> elements = index.GetOrNull(key);
            if (elements == null)
            {
                return new System.Collections.Generic.HashSet<E>();
            }
            return elements;
        }

        public virtual IEnumerable<int> GetIndexes()
        {
            return index.Keys;
        }

        public virtual bool Add(E element)
        {
            if (contents.Add(element))
            {
                HashSet<E> elements = index.GetOrNull(element.Index());
                if (elements == null)
                {
                    elements = new HashSet<E>();
                    index[element.Index()] = elements;
                }
                elements.Add(element);
                return true;
            }
            return false;
        }

        public virtual bool AddAll(IEnumerable<E> elements)
        {
            bool result = false;
            foreach (var element in elements)
            {
                if (Add(element))
                {
                    result = true;
                }
            }
            return result;
        }

        public virtual void Clear()
        {
            contents.Clear();
            index.Clear();
        }

        public virtual bool Contains(E o)
        {
            return contents.Contains(o);
        }

        public virtual bool ContainsAll(IEnumerable<E> c)
        {
            return c.All(x => contents.Contains(x));
        }

        public IEnumerator<E> GetEnumerator()
        {
            return contents.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return contents.GetEnumerator();
        }

        public virtual bool Remove(E o)
        {
            if (contents.Remove(o))
            {
                // This makes the remove O(n), but you need it like this if remove()
                // is to work through an iterator.
                // It doesn't really matter, since Jatalog doesn't use this method
                Reindex();
            }
            return false;
        }

        public virtual bool RemoveAll(IEnumerable<E> c)
        {
            bool changed = false;
            foreach (E t in c)
            {
                changed |= contents.Remove(t);
            }

            if (changed)
            {
                Reindex();
            }
            return changed;
        }

        private void Reindex()
        {
            index = new Dictionary<int, HashSet<E>>();
            foreach (E element in contents)
            {
                HashSet<E> elements = index.GetOrNull(element.Index());
                if (elements == null)
                {
                    elements = new HashSet<E>();
                    index[element.Index()] = elements;
                }
                elements.Add(element);
            }
        }
    }
}