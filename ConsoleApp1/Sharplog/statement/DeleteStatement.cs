using System.Collections.Generic;
using Sharplog.Engine;

namespace Sharplog.Statement
{
    internal class DeleteStatement : Sharplog.Statement.Statement
    {
        private IList<Expr> goals;

        internal DeleteStatement(IList<Expr> goals)
        {
            this.goals = goals;
        }

        /// <exception cref="Sharplog.DatalogException"/>
        public IEnumerable<IDictionary<string, string>> Execute(Sharplog.Jatalog datalog, StackMap bindings)
        {
            datalog.Delete(goals, bindings);
            return null;
        }
    }
}