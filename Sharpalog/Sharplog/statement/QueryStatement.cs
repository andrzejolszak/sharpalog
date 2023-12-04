using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
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
            if (this.IsAssert && goals.Count > 1 && goals[goals.Count - 1].Terms[0] == "count" && int.TryParse(goals[goals.Count - 1].Terms[1], out int arg2))
            {
                // Special assert count syntax, e.g.: 'assert: foo(X), count > 1.'
                var prefixRes = datalog.Query(goals.Take(goals.Count - 1).ToList());
                string pred = goals[goals.Count - 1].Predicate;
                var res = pred switch
                {
                    ">" when prefixRes.Count > arg2 => prefixRes,
                    "<" when prefixRes.Count < arg2 => prefixRes,
                    ">=" when prefixRes.Count >= arg2 => prefixRes,
                    "<=" when prefixRes.Count <= arg2 => prefixRes,
                    "=" when prefixRes.Count == arg2 => prefixRes,
                    "<>" when prefixRes.Count != arg2 => prefixRes,
                    "!=" when prefixRes.Count != arg2 => prefixRes,
                    _ => throw new DatalogException($"Expected the assert result count {prefixRes.Count} to be '{pred} {arg2}'")
                };

                if (res == prefixRes && res.Count == 0)
                {
                    // We expeted 0 answers, and this is what we got - hance the count assertion succeeded
                    res.Add(new Dictionary<string, string>());
                }

                return res;
            }
            else
            {
                return datalog.Query(goals);
            }
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