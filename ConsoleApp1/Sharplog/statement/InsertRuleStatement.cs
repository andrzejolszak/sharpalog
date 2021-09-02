using System.Collections.Generic;
using Sharplog.Engine;

namespace Sharplog.Statement
{
    internal class InsertRuleStatement : Sharplog.Statement.Statement
    {
        private readonly Rule rule;

        internal InsertRuleStatement(Rule rule)
        {
            this.rule = rule;
        }

        /// <exception cref="Sharplog.DatalogException"/>
        public IEnumerable<StackMap> Execute(Sharplog.Jatalog datalog, StackMap bindings)
        {
            Rule newRule;
            if (bindings != null)
            {
                newRule = rule.Substitute(bindings);
            }
            else
            {
                newRule = rule;
            }
            datalog.Rule(newRule);
            return null;
        }
    }
}