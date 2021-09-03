using System;
using System.Collections.Generic;
using System.Linq;
using Sharpen;
using Sharplog.Engine;

namespace Sharplog
{
    public class TestUtils
    {
        /// <exception cref="Sharplog.DatalogException"/>
        public static Sharplog.Jatalog CreateDatabase()
        {
            Sharplog.Jatalog jatalog = new Sharplog.Jatalog();
            jatalog
                .Fact("parent", "a", "aa")
                .Fact("parent", "a", "ab")
                .Fact("parent", "aa", "aaa")
                .Fact("parent", "aa", "aab")
                .Fact("parent", "aaa", "aaaa")
                .Fact("parent", "c", "ca");
            jatalog
                .Rule(Expr.CreateExpr("ancestor", "X", "Y"), Expr.CreateExpr("parent", "X", "Z"), Expr.CreateExpr("ancestor", "Z", "Y"))
                .Rule(Expr.CreateExpr("ancestor", "X", "Y"), Expr.CreateExpr("parent", "X", "Y"))
                .Rule(Expr.CreateExpr("sibling", "X", "Y"), Expr.CreateExpr("parent", "Z", "X"), Expr.CreateExpr("parent", "Z", "Y"), Expr.Ne("X", "Y"))
                .Rule(Expr.CreateExpr("related", "X", "Y"), Expr.CreateExpr("ancestor", "Z", "X"), Expr.CreateExpr("ancestor", "Z", "Y"));
            return jatalog;
        }

        public static bool MapContains(StackMap map, string key, string value)
        {
            if (map.TryGetValue(key, out var val))
            {
                return val.Equals(value);
            }

            return false;
        }

        public static bool MapContains(StackMap haystack, StackMap needle)
        {
            foreach (string key in needle.KeysTest())
            {
                if (!haystack.TryGetValue(key, out var val) || !needle.TryGetValue(key, out var val2) || !val.Equals(val2))
                {
                    return false;
                }
            }
            return true;
        }

        /// <exception cref="System.Exception"/>
        public static bool AnswerContains(IDictionary<Statement.Statement, List<(Statement.Statement, StackMap)>> answers, params string[] kvPairs)
        {
            StackMap needle = new StackMap(null);
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
            foreach ((Statement.Statement, StackMap) answer in answers.Values.SelectMany(x => x))
            {
                if (MapContains(answer.Item2, needle))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool AnswerContains(IEnumerable<StackMap> answers, params string[] kvPairs)
        {
            StackMap needle = new StackMap(null);
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
            foreach (StackMap answer in answers)
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