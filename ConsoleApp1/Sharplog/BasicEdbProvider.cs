using System.Collections.Generic;
using Sharplog.Engine;

namespace Sharplog
{
    /// <summary>
    /// Implementation of
    /// <see cref="EdbProvider"/>
    /// that wraps around an
    /// <see cref="Sharplog.Engine.IndexedSet{E, I}"/>
    /// for an in-memory EDB.
    /// </summary>
    public class BasicEdbProvider : EdbProvider
    {
        private IndexedSet edb;

        public BasicEdbProvider()
        {
            edb = new IndexedSet();
        }

        public IndexedSet AllFacts()
        {
            return edb;
        }

        public void Add(Expr fact)
        {
            edb.Add(fact);
        }

        public bool RemoveAll(List<Expr> facts)
        {
            return edb.RemoveAll(facts);
        }

        public IEnumerable<Expr> GetFacts(string predicate)
        {
            return edb.GetIndexed(predicate.GetHashCode());
        }
    }
}