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
        public IEnumerable<StackMap> Execute(Sharplog.Jatalog datalog, StackMap bindings)
        {
            Expr newFact;
            if (bindings != null)
            {
                newFact = fact.Substitute(bindings);
            }
            else
            {
                newFact = fact;
            }
            datalog.Fact(newFact);
            return null;
        }
    }
}