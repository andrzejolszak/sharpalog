using System.Collections.Generic;
using Sharplog.Engine;

namespace Sharplog.Statement
{
    internal class DeleteStatement : Statement
    {
        private List<Expr> goals;
        private string _ruleId;

        internal DeleteStatement(List<Expr> goals, string ruleId)
        {
            this.goals = goals;
            this._ruleId = ruleId;
        }

        /// <exception cref="Sharplog.DatalogException"/>
        public IEnumerable<IDictionary<string, string>> Execute(Universe datalog)
        {
            if (goals != null)
            {
                datalog.Delete(goals);
            }

            if (_ruleId != null)
            {
                datalog.Delete(_ruleId);
            }

            return null;
        }
    }
}