using System.Collections.Generic;
using Sharplog.Engine;

namespace Sharplog.Statement
{
    internal class InsertFactStatement : Sharplog.Statement.Statement
    {
        private readonly Expr fact;

        internal InsertFactStatement(Expr fact)
        {
            this.fact = fact;
        }

        /// <exception cref="Sharplog.DatalogException"/>
        public virtual IEnumerable<IDictionary<string, string>> Execute(Sharplog.Jatalog datalog, StackMap<string, string> bindings)
        {
            Expr newFact;
            if (bindings != null)
            {
                newFact = fact.Substitute(bindings.Map);
            }
            else
            {
                newFact = fact;
            }
            datalog.Fact(newFact);
            return null;
        }

        public IEnumerable<IDictionary<string, string>> Execute(Jatalog datalog)
        {
            return Execute(datalog, null);
        }
    }
}