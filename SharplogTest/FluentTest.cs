using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Sharplog.Engine;

namespace Sharplog
{
    public class FluentTest
    {
        // TODO: Code coverage could be better...
        /// <exception cref="System.Exception"/>
        [Test]
        public void TestApp()
        {
            //This is how you would use the fluent API:
            Sharplog.Jatalog jatalog = TestUtils.CreateDatabase();
            jatalog.Validate();
            IEnumerable<StackMap> answers;
            // Run a query "who are siblings?"; print the answers
            answers = jatalog.Query(Expr.CreateExpr("sibling", "A", "B"));
            // Siblings are aaa-aab and aa-ab as well as the reverse
            Assert.IsTrue(TestUtils.AnswerContains(answers, "A", "aab", "B", "aaa"));
            Assert.IsTrue(TestUtils.AnswerContains(answers, "A", "aaa", "B", "aab"));
            Assert.IsTrue(TestUtils.AnswerContains(answers, "A", "ab", "B", "aa"));
            Assert.IsTrue(TestUtils.AnswerContains(answers, "A", "aa", "B", "ab"));
            // Run a query "who are aa's descendants?"; print the answers
            answers = jatalog.Query(Expr.CreateExpr("ancestor", "aa", "X"));
            Assert.IsTrue(TestUtils.AnswerContains(answers, "X", "aaa"));
            Assert.IsTrue(TestUtils.AnswerContains(answers, "X", "aab"));
            Assert.IsTrue(TestUtils.AnswerContains(answers, "X", "aaaa"));
            // Alternative way to execute the statement:
            var answers2 = jatalog.ExecuteAll("ancestor(aa, X)?");
            Assert.IsTrue(TestUtils.AnswerContains(answers2, "X", "aaa"));
            Assert.IsTrue(TestUtils.AnswerContains(answers2, "X", "aab"));
            Assert.IsTrue(TestUtils.AnswerContains(answers2, "X", "aaaa"));
            // This demonstrates how you would use a built-in predicate in the fluent API.
            answers = jatalog.Query(Expr.CreateExpr("parent", "aa", "A"), Expr.CreateExpr("parent", "aa", "B"), Expr.Ne("A", "B"));
            Assert.IsTrue(TestUtils.AnswerContains(answers, "A", "aab", "B", "aaa"));
            Assert.IsTrue(TestUtils.AnswerContains(answers, "A", "aaa", "B", "aab"));
            // Test deletion
            Assert.IsTrue(jatalog.GetEdbProvider().AllFacts().Contains(Expr.CreateExpr("parent", "aa", "aaa")));
            Assert.IsTrue(jatalog.GetEdbProvider().AllFacts().Contains(Expr.CreateExpr("parent", "aaa", "aaaa")));
            // This query deletes parent(aa,aaa) and parent(aaa,aaaa)
            jatalog.Delete(Expr.CreateExpr("parent", "aa", "X"), Expr.CreateExpr("parent", "X", "aaaa"));
            Assert.IsFalse(jatalog.GetEdbProvider().AllFacts().Contains(Expr.CreateExpr("parent", "aa", "aaa")));
            Assert.IsFalse(jatalog.GetEdbProvider().AllFacts().Contains(Expr.CreateExpr("parent", "aaa", "aaaa")));
            // "who are aa's descendants now?"
            answers = jatalog.Query(Expr.CreateExpr("ancestor", "aa", "X"));
            //Assert.IsFalse(answers.Contains(Expr.CreateExpr("parent", "aa", "aaa")));
            //Assert.IsFalse(answers.Contains(Expr.CreateExpr("parent", "aaa", "aaaa")));
            Assert.IsTrue(TestUtils.AnswerContains(answers, "X", "aab"));
        }

        /// <exception cref="System.Exception"/>
        [Test]
        public void TestMultiGoals()
        {
            // You can have multiple goals in queries.
            Sharplog.Jatalog jatalog = TestUtils.CreateDatabase();
            jatalog.Validate();
            // Run a query "who are siblings A, and A is not `aaa`?"
            var answers = jatalog.ExecuteAll("sibling(A, B), A <> aaa?");
            Assert.IsTrue(answers != null);
            // Only aab is a sibling of aaa
            Assert.IsTrue(TestUtils.AnswerContains(answers, "A", "aab", "B", "aaa"));
            Assert.IsFalse(TestUtils.AnswerContains(answers, "A", "aaa", "B", "aab"));
            Assert.IsTrue(TestUtils.AnswerContains(answers, "A", "ab", "B", "aa"));
            Assert.IsTrue(TestUtils.AnswerContains(answers, "A", "aa", "B", "ab"));
        }

        /// <exception cref="System.Exception"/>
        [Test]
        public void TestExecute()
        {
            // The Jatalog.executeAll(String) method runs queries directly.
            Sharplog.Jatalog jatalog = new Sharplog.Jatalog();
            // Insert some facts
            jatalog.ExecuteAll("foo(bar). foo(baz).");
            // Run a query:
            var answers = jatalog.ExecuteAll("foo(What)?");
            Assert.IsTrue(answers != null);
            Assert.IsTrue(TestUtils.AnswerContains(answers, "What", "baz"));
            Assert.IsTrue(TestUtils.AnswerContains(answers, "What", "bar"));
            Assert.IsFalse(TestUtils.AnswerContains(answers, "What", "fred"));
        }

        /// <exception cref="System.Exception"/>
        [Test]
        public void TestDemo()
        {
            // This is how you would use the fluent API:
            Sharplog.Jatalog jatalog = new Sharplog.Jatalog();
            jatalog.Fact("parent", "alice", "bob").Fact("parent", "bob", "carol");
            jatalog.Rule(Expr.CreateExpr("ancestor", "X", "Y"), Expr.CreateExpr("parent", "X", "Z"), Expr.CreateExpr("ancestor", "Z", "Y")).Rule(Expr.CreateExpr("ancestor", "X", "Y"), Expr.CreateExpr("parent", "X", "Y"));
            IEnumerable<StackMap> answers;
            answers = jatalog.Query(Expr.CreateExpr("ancestor", "X", "carol"));
            Assert.IsTrue(TestUtils.AnswerContains(answers, "X", "alice"));
            Assert.IsTrue(TestUtils.AnswerContains(answers, "X", "bob"));
        }
    }
}