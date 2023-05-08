using System.Collections.Generic;
using System.Text;
using Sharplog.Engine;

namespace Sharplog.Statement
{
    internal class QueryStatement : Statement
    {
        private List<Expr> goals;

        public bool IsAssert { get; }

        internal QueryStatement(List<Expr> goals, bool isAssert)
        {
            this.goals = goals;
            this.IsAssert = isAssert;
        }

        /// <exception cref="Sharplog.DatalogException"/>
        public IEnumerable<IDictionary<string, string>> Execute(Universe datalog)
        {
            return datalog.Query(goals);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < goals.Count; i++)
            {
                sb.Append(goals[i].ToString());
                if (i < goals.Count - 1)
                {
                    sb.Append(", ");
                }
            }
            sb.Append("?");
            return sb.ToString();
        }
    }
}