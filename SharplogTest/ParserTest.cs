﻿namespace Sharplog
{
    using System.Linq;
    using NUnit.Framework;

    public class ParserTest
    {
        [Test]
        public void Expressions()
        {
            Universe target = new Universe();
            string src = @"
foo(a).
foo().
foo ( ) .
foo (a).
 foo (a).
foo(X) :- X = a.
foo(X):- X = a.
foo(X): - X = a.
foo(X) : - X = a.

foo(X)?";
            var res = target.ExecuteAll(src);
            Assert.AreEqual(src.Count(x => x == '?'), res.Count);
            Assert.IsTrue(res.Values.All(x => x.All(y => y.Item2.Count > 0)));
        }

        [Test]
        public void Universes()
        {
            Universe target = new Universe();
            string src = @"
universe foo_base1 {foo(a1).}
universe foo_base2 {foo(a2).}
universe bar extends foo_base1, foo_base2
{
    fooR(X) :- X = a.

    fooR(X)?
    foo(Y)?
}

% implicit default module that does not extend anything
foo (a, b).

foo (a, Y)?
";
            var res = target.ExecuteAll(src);
            Assert.AreEqual(src.Count(x => x == '?'), res.Count);
            Assert.IsTrue(res.Values.All(x => x.All(y => y.Item2.Count > 0)));
        }

        [Test]
        public void Asserts()
        {
            Universe target = new Universe();
            string src = @"
foo (a, b).

foo (a, Y)?
assert: foo(a, Y)?
assert: not foo(c, Y)?
assert: foo(a, Y), foo(X, b), not goo(f)?
assert: foo(a, b)?

bar(d).
bar (X)?
";
            var res = target.ExecuteAll(src);
            Assert.AreEqual(2, res.Count);
            Assert.IsTrue(res.Values.All(x => x.All(y => y.Item2.Count > 0)));
        }

        [Test]
        public void AssertFail()
        {
            Universe target = new Universe();
            string src = @"
foo (a, b).

assert: foo(c, Y)?
";
            DatalogException exc = Assert.Throws<DatalogException>(() => target.ExecuteAll(src));
            Assert.IsTrue(exc.Message.Contains("Assertion"));
        }

        [Test]
        public void AssertFail2()
        {
            Universe target = new Universe();
            string src = @"
foo (a, b).

assert: not foo(a, b), foo(a,b)?
";
            DatalogException exc = Assert.Throws<DatalogException>(() => target.ExecuteAll(src));
            Assert.IsTrue(exc.Message.Contains("Assertion"));
        }

        [Test]
        public void AssertFail3()
        {
            Universe target = new Universe();
            string src = @"
foo (a, b).

assert: foo(a,b), not foo(a, X)?
";
            DatalogException exc = Assert.Throws<DatalogException>(() => target.ExecuteAll(src));
            Assert.IsTrue(exc.Message.Contains("Assertion"));
        }

        [Test]
        public void Ids()
        {
            Universe target = new Universe();
            string src = @"
foo (a, b).
@id123: foo(X) :- X = a.
@124as:
    foo(X, b) :- X = a.

foo(X, Y) :- X = a, Y = c.

foo(X)?";
            var res = target.ExecuteAll(src);
            Assert.AreEqual(src.Count(x => x == '?'), res.Count);
            Assert.IsTrue(res.Values.All(x => x.All(y => y.Item2.Count > 0)));
        }
    }
}