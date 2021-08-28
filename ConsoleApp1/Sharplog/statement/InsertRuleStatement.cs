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
        public virtual IEnumerable<IDictionary<string, string>> Execute(Sharplog.Jatalog datalog, StackMap<string, string> bindings)
        {
            Rule newRule;
            if (bindings != null)
            {
                newRule = rule.Substitute(bindings.Map);
            }
            else
            {
                newRule = rule;
            }
            datalog.Rule(newRule);
            return null;
        }

        public IEnumerable<IDictionary<string, string>> Execute(Jatalog datalog)
        {
            return Execute(datalog, null);
        }
    }
}