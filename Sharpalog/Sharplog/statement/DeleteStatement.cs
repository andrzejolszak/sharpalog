using System.Collections.Generic;
using Sharplog.Engine;

namespace Sharplog.Statement
{
    internal class DeleteStatement : Statement
    {
        private List<Expr> goals;

        internal DeleteStatement(List<Expr> goals)
        {
            this.goals = goals;
        }

        /// <exception cref="Sharplog.DatalogException"/>
        public IEnumerable<IDictionary<string, string>> Execute(Universe datalog)
        {
            if (goals != null)
            {
                datalog.Delete(goals);
            }

            return null;
        }
    }
}