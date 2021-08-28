using System.Collections.Generic;
using NUnit.Framework;
using Sharplog.Engine;

namespace Sharplog
{
    public class RuleTest
    {
        /// <exception cref="Sharplog.DatalogException"/>
        [Test]
        public virtual void TestValidate()
        {
            Rule rule = new Rule(new Expr("p", "A", "B"), new Expr("q", "A"), new Expr("q", "B"), new Expr("<>", "A", "B"));
            rule.Validate();
            Assert.IsTrue(true);
            rule = new Rule(new Expr("p", "A", "B"), new Expr("q", "A"), new Expr("q", "B"), new Expr("q", "C"), new Expr("=", "C", "B"));
            rule.Validate();
            Assert.IsTrue(true);
            try
            {
                // The variable C must appear in the body - exception thrown
                rule = new Rule(new Expr("p", "A", "C"), new Expr("q", "A"), new Expr("q", "B"), new Expr("<>", "A", "B"));
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
                rule = new Rule(new Expr("p", "A", "B"), new Expr("q", "A"), new Expr("<>", "A", "B"));
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
                rule = new Rule(new Expr("p", "A", "B"), new Expr("q", "A"), Expr.Not("q", "B"));
                rule.Validate();
                Assert.IsFalse(true);
            }
            catch (DatalogException)
            {
                Assert.IsTrue(true);
            }
            rule = new Rule(new Expr("p", "A", "B"), new Expr("q", "A"), Expr.Eq("A", "B"));
            rule.Validate();
            Assert.IsTrue(true);
            rule = new Rule(new Expr("p", "A", "B"), new Expr("q", "A"), Expr.Eq("a", "B"));
            rule.Validate();
            Assert.IsTrue(true);
            rule = new Rule(new Expr("p", "A", "B"), new Expr("q", "B"), Expr.Eq("A", "b"));
            rule.Validate();
            Assert.IsTrue(true);
            try
            {
                // Invalid number of operands
                rule = new Rule(new Expr("p", "A", "B"), Expr.CreateExpr("=", "A", "B", "C"));
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
                rule = new Rule(new Expr("p", "A", "B"), new Expr("q", "A"), Expr.Eq("C", "D"));
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
                rule = new Rule(new Expr("p", "A", "B"), Expr.Ne("C", "B"));
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
                rule = new Rule(new Expr("p", "A", "B"), Expr.Ne("A", "C"));
                rule.Validate();
                Assert.IsFalse(true);
            }
            catch (DatalogException)
            {
                Assert.IsTrue(true);
            }
            rule = new Rule(new Expr("p", "A"), new Expr("q", "A"), Expr.Ne("A", "a"));
            rule.Validate();
            Assert.IsTrue(true);
            rule = new Rule(new Expr("p", "A"), new Expr("q", "A"), Expr.Ne("a", "A"));
            rule.Validate();
            Assert.IsTrue(true);
            rule = new Rule(new Expr("p", "A", "B"), new Expr("q", "A", "B"), Expr.Not("r", "A", "B"));
            rule.Validate();
            Assert.IsTrue(true);
            try
            {
                // Right operand unbound - exception thrown
                rule = new Rule(new Expr("p", "A", "B"), Expr.CreateExpr("q", "a", "A"), Expr.Not("q", "b", "B"));
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
                rule = new Rule(new Expr("p", "a"), Expr.CreateExpr("q", "A"));
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
        public virtual void TestToString()
        {
            Rule rule = new Rule(new Expr("p", "A", "B"), new Expr("q", "A"), new Expr("q", "B"), new Expr("<>", "A", "B"));
            Assert.IsTrue(rule.ToString().Equals("p(A, B) :- q(A), q(B), A <> B"));
        }

        /// <exception cref="Sharplog.DatalogException"/>
        [Test]
        public virtual void TestSubstitute()
        {
            Rule rule = new Rule(new Expr("p", "A", "B"), new Expr("q", "A"), new Expr("q", "B"), new Expr("<>", "A", "B"));
            StackMap<string, string> bindings = new StackMap<string, string>();
            bindings.Add("A", "aa");
            Rule subsRule = rule.Substitute(bindings.Map);
            Assert.IsTrue(subsRule.Equals(new Rule(new Expr("p", "aa", "B"), new Expr("q", "aa"), new Expr("q", "B"), new Expr("<>", "aa", "B"))));
            // Original rule unchanged?
            Assert.IsTrue(rule.Equals(new Rule(new Expr("p", "A", "B"), new Expr("q", "A"), new Expr("q", "B"), new Expr("<>", "A", "B"))));
        }
    }
}