using System.Collections.Generic;
using Sharplog.Engine;

namespace Sharplog.Statement
{
    internal class InsertFactStatement : Statement
    {
        private readonly Expr fact;

        internal InsertFactStatement(Expr fact)
        {
            this.fact = fact;
        }

        /// <exception cref="Sharplog.DatalogException"/>
        public IEnumerable<IDictionary<string, string>> Execute(Universe datalog)
        {
            Expr newFact = fact;
            datalog.Fact(newFact);
            return null;
        }

        public void PrependObjectId(string id) => this.fact.PrependTerm(id);
    }
}