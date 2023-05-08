using System.Collections.Generic;
using Sharplog.Engine;

namespace Sharplog.Statement
{
    internal class InsertRuleStatement : Statement
    {
        private readonly Rule rule;

        internal InsertRuleStatement(Rule rule)
        {
            this.rule = rule;
        }

        /// <exception cref="Sharplog.DatalogException"/>
        public IEnumerable<IDictionary<string, string>> Execute(Universe datalog)
        {
            Rule newRule = rule;
            datalog.Rule(newRule);
            return null;
        }
    }
}