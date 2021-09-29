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
    /// foo(bar, car);
    /// foo(zar);
    /// ---------------
    /// foo->{foo(bar, car), foo(zar)}
    /// </summary>
    /// <?/>
    /// <?/>
public class IndexedSet
    {
        private HashSet<Expr> contents;

        private IDictionary<int, HashSet<Expr>> index;

        /// <summary>Default constructor.</summary>
        public IndexedSet()
        {
            index = new Dictionary<int, HashSet<Expr>>();
            contents = new HashSet<Expr>();
        }

        public int Count
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
        public HashSet<Expr> GetIndexed(Expr key)
        {
            if (!index.TryGetValue(key.Index(), out HashSet<Expr> elements))
            {
                return new HashSet<Expr>();
            }
            return elements;
        }

        public IEnumerable<int> GetIndexes()
        {
            return index.Keys;
        }

        public bool Add(Expr element)
        {
            if (contents.Add(element))
            {
                if (!index.TryGetValue(element.Index(), out HashSet<Expr> elements))
                {
                    elements = new HashSet<Expr>();
                    index[element.Index()] = elements;
                }

                elements.Add(element);
                return true;
            }
            return false;
        }

        public bool AddAll(IEnumerable<Expr> elements)
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

        public HashSet<Expr> All => this.contents;

        public void ClearTest()
        {
            contents.Clear();
            index.Clear();
        }

        public bool Contains(Expr o)
        {
            return contents.Contains(o);
        }

        public bool ContainsIndex(Expr o)
        {
            return index.ContainsKey(o.Index());
        }

        public bool ContainsAllTest(IEnumerable<Expr> c)
        {
            return c.All(x => contents.Contains(x));
        }

        public bool RemoveTest(Expr o)
        {
            if (contents.Remove(o))
            {
                index[o.Index()].Remove(o);
            }
            return false;
        }

        public bool RemoveAll(List<Expr> c)
        {
            bool changed = false;
            foreach (Expr t in c)
            {
                bool chg = contents.Remove(t);
                if (chg)
                {
                    changed = true;
                    HashSet<Expr> set = index[t.Index()];
                    set.Remove(t);

                    // Maybe not needed?
                    if (set.Count == 0)
                    {
                        index.Remove(t.Index());
                    }
                }
            }

            return changed;
        }
    }
}