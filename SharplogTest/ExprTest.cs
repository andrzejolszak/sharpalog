using System;
using System.Collections.Generic;
using NUnit.Framework;
using Sharpen;
using Sharplog.Engine;

namespace Sharplog
{
    public class ExprTest
    {
        [Test]
        public void TestEquals()
        {
            Expr e1 = new Expr("foo", "a", "b");
            Assert.IsTrue(e1.GetPredicate().Equals("foo"));
            Assert.IsTrue(e1.Arity == 2);
            Assert.IsFalse(e1.IsNegated());
            Expr e2 = new Expr("foo", "a", "b");
            Assert.IsTrue(e1.Equals(e2));
            Expr e3 = new Expr("bar", "a", "b");
            Assert.IsFalse(e1.Equals(e3));
            Expr e4 = new Expr("foo", "a", "b", "c");
            Assert.IsTrue(e4.Arity == 3);
            Assert.IsFalse(e1.Equals(e4));
            Assert.IsFalse(e1.Equals(null));
            Assert.IsFalse(e1.Equals(this));
        }

        [Test]
        public void TestGround()
        {
            Assert.IsTrue(Sharplog.Jatalog.IsVariable("X"));
            Assert.IsFalse(Sharplog.Jatalog.IsVariable("x"));
            Assert.IsTrue(Sharplog.Jatalog.IsVariable("Hello"));
            Assert.IsFalse(Sharplog.Jatalog.IsVariable("hello"));
            Expr e1 = Expr.Not("foo", "a", "b");
            Assert.IsTrue(e1.IsGround());
            Expr e2 = new Expr("foo", "A", "B");
            Assert.IsFalse(e2.IsGround());
        }

        [Test]
        public void TestNegation()
        {
            Expr e1 = Expr.Not("foo", "a", "b");
            Assert.IsTrue(e1.IsNegated());
            Expr e2 = new Expr("foo", "a", "b");
            Assert.IsFalse(e1.Equals(e2));
        }

        [Test]
        public void TestGoodUnification()
        {
            StackMap bindings = new StackMap(null);
            Expr e1 = new Expr("foo", "a", "b");
            Expr e2 = new Expr("foo", "a", "b");
            Assert.IsTrue(e1.GroundUnifyWith(e2, bindings, out bindings));
            bindings.Add("X", "b");
            Expr e3 = new Expr("foo", "a", "X");
            Assert.IsTrue(e1.GroundUnifyWith(e3, bindings, out bindings));
//            Assert.IsTrue(e3.Unify(e1, bindings));
            Expr e3a = new Expr("foo", "a", "X");
//            Assert.IsTrue(e3.Unify(e3a, bindings));
            bindings.ClearTest();
            Expr e4 = new Expr("foo", "Y", "X");
            Assert.IsTrue(e1.GroundUnifyWith(e4, bindings, out bindings));
            Assert.IsTrue(bindings.TryGetValue("Y", out string yVal) && yVal.Equals("a"));
            bindings.ClearTest();
            Assert.IsTrue(e1.GroundUnifyWith(e4, bindings, out bindings));
            Assert.IsTrue(bindings.TryGetValue("Y", out string yVal2) && yVal2.Equals("a"));
            Assert.IsTrue(bindings.TryGetValue("X", out string xVal) && xVal.Equals("b"));
        }

        [Test]
        public void TestBadUnification()
        {
            StackMap bindings = new StackMap(null);
            Expr e1 = new Expr("foo", "a", "b");
            Expr e2 = new Expr("foo", "a", "b", "c");
            Assert.IsFalse(e1.GroundUnifyWith(e2, bindings, out bindings));
            Assert.IsFalse(e2.GroundUnifyWith(e1, bindings, out bindings));
            Expr e3 = new Expr("bar", "a", "b");
            Assert.IsFalse(e1.GroundUnifyWith(e3, bindings, out bindings));
            Assert.IsFalse(e3.GroundUnifyWith(e1, bindings, out bindings));
            Expr e4 = new Expr("foo", "A", "b");
            Assert.IsTrue(e1.GroundUnifyWith(e4, bindings, out bindings));
            bindings.ClearTest();
            bindings.Add("A", "xxxx");
            Assert.IsFalse(e1.GroundUnifyWith(e4, bindings, out bindings));
            // Assert.IsFalse(e4.Unify(e1, bindings));
        }

        [Test]
        public void TestToString()
        {
            Expr e1 = new Expr("foo", "a", "b");
            Assert.IsTrue(e1.ToString().Equals("foo(a, b)"));
            Expr e2 = Expr.Not("foo", "a", "b");
            Assert.IsTrue(e2.ToString().Equals("not foo(a, b)"));
            Expr e3 = new Expr("<>", "X", "Y");
            Assert.IsTrue(e3.ToString().Equals("X <> Y"));
        }

        [Test]
        public void TestIsBuiltin()
        {
            Expr e1 = new Expr("<>", "A", "B");
            Assert.IsTrue(e1.IsBuiltIn());
            Expr e2 = new Expr("\"quoted predicate", "A", "B");
            Assert.IsFalse(e2.IsBuiltIn());
        }

        [Test]
        public void TestSubstitute()
        {
            Expr e1 = new Expr("foo", "X", "Y");
            StackMap bindings = new StackMap(null);
            bindings.Add("X", "a");
            Expr e2 = e1.Substitute(bindings);
            Assert.IsTrue(e2.GetTerms()[0].Equals("a"));
            Assert.IsTrue(e2.GetTerms()[1].Equals("Y"));
            Assert.IsFalse(e2.IsNegated());
            e1 = Expr.Not("foo", "X", "Y");
            e2 = e1.Substitute(bindings);
            Assert.IsTrue(e2.GetTerms()[0].Equals("a"));
            Assert.IsTrue(e2.GetTerms()[1].Equals("Y"));
            Assert.IsTrue(e2.IsNegated());
        }

        [Test]
        public void TestQuotedStrings()
        {
            Expr e1 = new Expr("foo", "\"This is a quoted string");
            StackMap bindings = new StackMap(null);
            Assert.IsTrue(e1.ToString().Equals("foo(\"This is a quoted string\")"));
            bindings.Add("X", "\"This is a quoted string");
            bindings.Add("Y", "random");
            Expr e2 = new Expr("foo", "X");
            Assert.IsTrue(e1.GroundUnifyWith(e2, bindings, out bindings));
            Expr e3 = new Expr("foo", "Y");
            Assert.IsFalse(e1.GroundUnifyWith(e3, bindings, out _));
            bindings.ClearTest();
            Assert.IsTrue(e1.GroundUnifyWith(e2, bindings, out bindings));
            Assert.IsTrue(bindings.TryGetValue("X", out string xVal) && xVal.Equals("\"This is a quoted string"));
            bindings.ClearTest();
            Assert.IsTrue(e1.GroundUnifyWith(e2, bindings, out bindings));
            Assert.IsTrue(bindings.TryGetValue("X", out string xVal2) && xVal2.Equals("\"This is a quoted string"));
        }

        /// <exception cref="System.Exception"/>
        [Test]
        public void TestEvalBuiltinEq()
        {
            StackMap bindings = new StackMap(null);
            Expr e1 = new Expr("=", "X", "Y");
            bindings.Add("X", "hello");
            bindings.Add("Y", "hello");
            Assert.IsTrue(e1.EvalBuiltIn(bindings, out bindings));
            bindings.ClearTest();
            bindings.Add("X", "hello");
            Assert.IsTrue(e1.EvalBuiltIn(bindings, out bindings));
            Assert.IsTrue(bindings.TryGetValue("Y", out string yVal) && yVal.Equals("hello"));
            bindings.ClearTest();
            bindings.Add("Y", "hello");
            Assert.IsTrue(e1.EvalBuiltIn(bindings, out bindings));
            Assert.IsTrue(bindings.TryGetValue("X", out string xVal) && xVal.Equals("hello"));
            bindings.ClearTest();
            bindings.Add("X", "hello");
            Assert.IsTrue(e1.EvalBuiltIn(bindings, out bindings));
            Assert.IsTrue(bindings.TryGetValue("Y", out string yVal2) && yVal2.Equals("hello"));
            try
            {
                bindings.ClearTest();
                e1.EvalBuiltIn(bindings, out bindings);
                Assert.IsFalse(true);
            }
            catch (DatalogException ex)
            {
                Assert.IsTrue(true);
            }
            bindings.Add("X", "100");
            bindings.Add("Y", "100.0000");
            Assert.IsTrue(e1.EvalBuiltIn(bindings, out bindings));
            bindings.ClearTest();
            bindings.Add("X", "100");
            bindings.Add("Y", "105");
            Assert.IsFalse(e1.EvalBuiltIn(bindings, out StackMap newBindings));
            bindings.ClearTest();
            bindings.Add("X", "100");
            bindings.Add("Y", "aaa");
            Assert.IsFalse(e1.EvalBuiltIn(bindings, out newBindings));
            bindings.ClearTest();
            bindings.Add("X", "aaa");
            bindings.Add("Y", "100");
            Assert.IsFalse(e1.EvalBuiltIn(bindings, out newBindings));
            e1 = new Expr("=", "X", "aaa");
            bindings.ClearTest();
            bindings.Add("X", "aaa");
            Assert.IsTrue(e1.EvalBuiltIn(bindings, out bindings));
            e1 = new Expr("=", "aaa", "Y");
            bindings.ClearTest();
            bindings.Add("Y", "aaa");
            Assert.IsTrue(e1.EvalBuiltIn(bindings, out bindings));
        }

        /// <exception cref="System.Exception"/>
        [Test]
        public void TestEvalBuiltinNe()
        {
            StackMap bindings = new StackMap(null);
            Expr e1 = new Expr("!=", "X", "Y");
            Assert.IsTrue(e1.GetPredicate().Equals("<>"));
            bindings.Add("X", "hello");
            bindings.Add("Y", "hello");
            Assert.IsFalse(e1.EvalBuiltIn(bindings, out StackMap newBindings));
            bindings.ClearTest();
            bindings.Add("X", "hello");
            bindings.Add("Y", "olleh");
            Assert.IsTrue(e1.EvalBuiltIn(bindings, out bindings));
            bindings.ClearTest();
            bindings.Add("X", "10");
            bindings.Add("Y", "10.000");
            Assert.IsFalse(e1.EvalBuiltIn(bindings, out newBindings));
            bindings.ClearTest();
            bindings.Add("X", "10");
            bindings.Add("Y", "10.0001");
            Assert.IsTrue(e1.EvalBuiltIn(bindings, out bindings));
            try
            {
                bindings.ClearTest();
                e1.EvalBuiltIn(bindings, out bindings);
                Assert.IsFalse(true);
            }
            catch (DatalogException ex)
            {
                Assert.IsTrue(true);
            }
            try
            {
                bindings.ClearTest();
                bindings.Add("X", "10");
                e1.EvalBuiltIn(bindings, out bindings);
                Assert.IsFalse(true);
            }
            catch (DatalogException ex)
            {
                Assert.IsTrue(true);
            }
            try
            {
                bindings.ClearTest();
                bindings.Add("Y", "10");
                e1.EvalBuiltIn(bindings, out bindings);
                Assert.IsFalse(true);
            }
            catch (DatalogException ex)
            {
                Assert.IsTrue(true);
            }
            bindings.ClearTest();
            bindings.Add("X", "100");
            bindings.Add("Y", "aaa");
            Assert.IsTrue(e1.EvalBuiltIn(bindings, out bindings));
            bindings.ClearTest();
            bindings.Add("X", "aaa");
            bindings.Add("Y", "100");
            Assert.IsTrue(e1.EvalBuiltIn(bindings, out bindings));
        }

        /// <exception cref="System.Exception"/>
        [Test]
        public void TestEvalBuiltinOther()
        {
            StackMap bindings = new StackMap(null);
            bindings.Add("X", "100");
            bindings.Add("Y", "200");
            Expr e1 = new Expr("=!=", "X", "Y");
            // Bad operator
            try
            {
                e1.EvalBuiltIn(bindings, out bindings);
                Assert.IsTrue(false);
            }
            catch (DatalogException ex)
            {
                Assert.IsTrue(true);
            }
            e1 = new Expr(">", "X", "Y");
            Assert.IsFalse(e1.EvalBuiltIn(bindings, out StackMap newBindings));
            e1 = new Expr(">", "X", "0");
            Assert.IsTrue(e1.EvalBuiltIn(bindings, out bindings));
            e1 = new Expr(">=", "X", "Y");
            Assert.IsFalse(e1.EvalBuiltIn(bindings, out newBindings));
            e1 = new Expr(">=", "X", "0");
            Assert.IsTrue(e1.EvalBuiltIn(bindings, out bindings));
            e1 = new Expr(">=", "X", "100");
            Assert.IsTrue(e1.EvalBuiltIn(bindings, out newBindings));
            e1 = new Expr("<", "X", "Y");
            Assert.IsTrue(e1.EvalBuiltIn(bindings, out bindings));
            e1 = new Expr("<", "X", "X");
            Assert.IsFalse(e1.EvalBuiltIn(bindings, out newBindings));
            bindings.ClearTest();
            bindings.Add("X", "100");
            bindings.Add("Y", "200");
            e1 = new Expr("<=", "X", "Y");
            Assert.IsTrue(e1.EvalBuiltIn(bindings, out bindings));
            bindings.ClearTest();
            bindings.Add("X", "100");
            bindings.Add("Y", "200");
            e1 = new Expr("<=", "X", "X");
            Assert.IsTrue(e1.EvalBuiltIn(bindings, out newBindings));
            bindings.ClearTest();
            bindings.Add("X", "100");
            bindings.Add("Y", "200");
            e1 = new Expr("<=", "Y", "X");
            Assert.IsFalse(e1.EvalBuiltIn(bindings, out newBindings));
            bindings.ClearTest();
            bindings.Add("X", "100");
            bindings.Add("Y", "aaa");
            e1 = new Expr("<", "X", "Y");
            Assert.IsFalse(e1.EvalBuiltIn(bindings, out newBindings));
            bindings.ClearTest();
            bindings.Add("X", "aaa");
            bindings.Add("Y", "100");
            e1 = new Expr("<", "X", "Y");
            Assert.IsTrue(e1.EvalBuiltIn(bindings, out bindings));
        }
    }
}