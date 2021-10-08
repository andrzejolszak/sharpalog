using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Sharplog.Engine;

namespace Sharplog
{
    public class RuleTest
    {
        private static Rule Rule(Expr head, params Expr[] body)
        {
            return new Rule(head, body.ToList(), null);
        }

        /// <exception cref="Sharplog.DatalogException"/>
        [Test]
        public void TestValidate()
        {
            Rule rule = Rule(new Expr("p", "A", "B"), new Expr("q", "A"), new Expr("q", "B"), new Expr("<>", "A", "B"));
            rule.Validate();
            Assert.IsTrue(true);
            rule = Rule(new Expr("p", "A", "B"), new Expr("q", "A"), new Expr("q", "B"), new Expr("q", "C"), new Expr("=", "C", "B"));
            rule.Validate();
            Assert.IsTrue(true);
            try
            {
                // The variable C must appear in the body - exception thrown
                rule = Rule(new Expr("p", "A", "C"), new Expr("q", "A"), new Expr("q", "B"), new Expr("<>", "A", "B"));
                rule.Validate();
                Assert.IsFalse(true);
            }
            catch (DatalogException)
            {
                Assert.IsTrue(true);
            }
            try
            {
                // Variable B must appear in a positive expression - exception
                // thrown
                rule = Rule(new Expr("p", "A", "B"), new Expr("q", "A"), new Expr("<>", "A", "B"));
                rule.Validate();
                Assert.IsFalse(true);
            }
            catch (DatalogException)
            {
                Assert.IsTrue(true);
            }
            try
            {
                // Again, variable B must appear in a positive expression -
                // exception thrown
                rule = Rule(new Expr("p", "A", "B"), new Expr("q", "A"), Expr.Not("q", "B"));
                rule.Validate();
                Assert.IsFalse(true);
            }
            catch (DatalogException)
            {
                Assert.IsTrue(true);
            }
            rule = Rule(new Expr("p", "A", "B"), new Expr("q", "A"), Expr.Eq("A", "B"));
            rule.Validate();
            Assert.IsTrue(true);
            rule = Rule(new Expr("p", "A", "B"), new Expr("q", "A"), Expr.Eq("a", "B"));
            rule.Validate();
            Assert.IsTrue(true);
            rule = Rule(new Expr("p", "A", "B"), new Expr("q", "B"), Expr.Eq("A", "b"));
            rule.Validate();
            Assert.IsTrue(true);
            try
            {
                // Invalid number of operands
                rule = Rule(new Expr("p", "A", "B"), Expr.CreateExpr("=", "A", "B", "C"));
                rule.Validate();
                Assert.IsFalse(true);
            }
            catch (DatalogException)
            {
                Assert.IsTrue(true);
            }
            try
            {
                // Both operands of '=' unbound - exception thrown
                rule = Rule(new Expr("p", "A", "B"), new Expr("q", "A"), Expr.Eq("C", "D"));
                rule.Validate();
                Assert.IsFalse(true);
            }
            catch (DatalogException)
            {
                Assert.IsTrue(true);
            }
            try
            {
                // Left operand unbound - exception thrown
                rule = Rule(new Expr("p", "A", "B"), Expr.Ne("C", "B"));
                rule.Validate();
                Assert.IsFalse(true);
            }
            catch (DatalogException)
            {
                Assert.IsTrue(true);
            }
            try
            {
                // Right operand unbound - exception thrown
                rule = Rule(new Expr("p", "A", "B"), Expr.Ne("A", "C"));
                rule.Validate();
                Assert.IsFalse(true);
            }
            catch (DatalogException)
            {
                Assert.IsTrue(true);
            }
            rule = Rule(new Expr("p", "A"), new Expr("q", "A"), Expr.Ne("A", "a"));
            rule.Validate();
            Assert.IsTrue(true);
            rule = Rule(new Expr("p", "A"), new Expr("q", "A"), Expr.Ne("a", "A"));
            rule.Validate();
            Assert.IsTrue(true);
            rule = Rule(new Expr("p", "A", "B"), new Expr("q", "A", "B"), Expr.Not("r", "A", "B"));
            rule.Validate();
            Assert.IsTrue(true);
            try
            {
                // Right operand unbound - exception thrown
                rule = Rule(new Expr("p", "A", "B"), Expr.CreateExpr("q", "a", "A"), Expr.Not("q", "b", "B"));
                rule.Validate();
                Assert.IsFalse(true);
            }
            catch (DatalogException)
            {
                Assert.IsTrue(true);
            }
            try
            {
                // Right operand unbound - exception thrown
                rule = Rule(new Expr("p", "a"), Expr.CreateExpr("q", "A"));
                rule.Validate();
                Assert.IsFalse(true);
            }
            catch (DatalogException)
            {
                Assert.IsTrue(true);
            }
        }

        /// <exception cref="Sharplog.DatalogException"/>
        [Test]
        public void TestToString()
        {
            Rule rule = Rule(new Expr("p", "A", "B"), new Expr("q", "A"), new Expr("q", "B"), new Expr("<>", "A", "B"));
            Assert.IsTrue(rule.ToString().Equals("p(A, B) :- q(A), q(B), A <> B"));
        }

        /// <exception cref="Sharplog.DatalogException"/>
        [Test]
        public void TestSubstitute()
        {
            Rule rule = Rule(new Expr("p", "A", "B"), new Expr("q", "A"), new Expr("q", "B"), new Expr("<>", "A", "B"));
            StackMap bindings = new StackMap();
            bindings.Add("A", "aa");
            bindings.Add("B", "bb");
            Rule subsRule = rule.Substitute(bindings);
            Assert.IsTrue(subsRule.ToString().Equals(Rule(new Expr("p", "aa", "bb"), new Expr("q", "aa"), new Expr("q", "bb"), new Expr("<>", "aa", "bb")).ToString()));
            // Original rule unchanged?
            Assert.IsTrue(rule.ToString().Equals(Rule(new Expr("p", "A", "B"), new Expr("q", "A"), new Expr("q", "B"), new Expr("<>", "A", "B")).ToString()));
        }
    }
}