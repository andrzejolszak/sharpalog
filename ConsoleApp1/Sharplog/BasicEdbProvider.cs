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
        private IndexedSet<Expr> edb;

        public BasicEdbProvider()
        {
            edb = new IndexedSet<Expr>();
        }

        public virtual IEnumerable<Expr> AllFacts()
        {
            return edb;
        }

        public virtual void Add(Expr fact)
        {
            edb.Add(fact);
        }

        public virtual bool RemoveAll(IEnumerable<Expr> facts)
        {
            return edb.RemoveAll(facts);
        }

        public virtual IEnumerable<Expr> GetFacts(string predicate)
        {
            return edb.GetIndexed(predicate.GetHashCode());
        }
    }
}