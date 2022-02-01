using System;
using System.Collections.Generic;
using System.Linq;
using Sharplog.Engine;

namespace Sharplog
{
    public class TestUtils
    {
        /// <exception cref="Sharplog.DatalogException"/>
        public static Sharplog.Universe CreateDatabase()
        {
            Sharplog.Universe jatalog = new Sharplog.Universe();
            jatalog
                .Fact("parent", "a", "aa")
                .Fact("parent", "a", "ab")
                .Fact("parent", "aa", "aaa")
                .Fact("parent", "aa", "aab")
                .Fact("parent", "aaa", "aaaa")
                .Fact("parent", "c", "ca");
            jatalog
                .RuleTest(Expr.CreateExpr("ancestor", "X", "Y"), Expr.CreateExpr("parent", "X", "Z"), Expr.CreateExpr("ancestor", "Z", "Y"))
                .RuleTest(Expr.CreateExpr("ancestor", "X", "Y"), Expr.CreateExpr("parent", "X", "Y"))
                .RuleTest(Expr.CreateExpr("sibling", "X", "Y"), Expr.CreateExpr("parent", "Z", "X"), Expr.CreateExpr("parent", "Z", "Y"), Expr.Ne("X", "Y"))
                .RuleTest(Expr.CreateExpr("related", "X", "Y"), Expr.CreateExpr("ancestor", "Z", "X"), Expr.CreateExpr("ancestor", "Z", "Y"));
            return jatalog;
        }

        public static bool MapContains(IDictionary<string, string> map, string key, string value)
        {
            if (map.TryGetValue(key, out var val))
            {
                return val.Equals(value);
            }

            return false;
        }

        public static bool MapContains(IDictionary<string, string> haystack, IDictionary<string, string> needle)
        {
            foreach (string key in needle.Keys)
            {
                if (!haystack.TryGetValue(key, out var val) || !needle.TryGetValue(key, out var val2) || !val.Equals(val2))
                {
                    return false;
                }
            }
            return true;
        }

        /// <exception cref="System.Exception"/>
        public static bool AnswerContains(IDictionary<Statement.Statement, List<(Statement.Statement, IDictionary<string, string>)>> answers, params string[] kvPairs)
        {
            IDictionary<string, string> needle = new Dictionary<string, string>();
            if (kvPairs.Length % 2 != 0)
            {
                throw new Exception("kvPairs must be even");
            }
            for (int i = 0; i < kvPairs.Length / 2; i++)
            {
                string k = kvPairs[i * 2];
                string v = kvPairs[i * 2 + 1];
                needle.Add(k, v);
            }
            foreach ((Statement.Statement, IDictionary<string, string>) answer in answers.Values.SelectMany(x => x))
            {
                if (MapContains(answer.Item2, needle))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool AnswerContains(IEnumerable<IDictionary<string, string>> answers, params string[] kvPairs)
        {
            IDictionary<string, string> needle = new Dictionary<string, string>();
            if (kvPairs.Length % 2 != 0)
            {
                throw new Exception("kvPairs must be even");
            }
            for (int i = 0; i < kvPairs.Length / 2; i++)
            {
                string k = kvPairs[i * 2];
                string v = kvPairs[i * 2 + 1];
                needle.Add(k, v);
            }
            foreach (IDictionary<string, string> answer in answers)
            {
                if (MapContains(answer, needle))
                {
                    return true;
                }
            }
            return false;
        }
    }
}