namespace Sharplog
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
foo(X) :- X = b.
foo(X):- X = c.
foo(X):- X = d.
foo(X) :- X = e.
foo(X) :- bar(X).
bar(X) :- X = 2, foo(a).

foo(X)?";
            var res = target.ExecuteAll(src);
            Assert.AreEqual(src.Count(x => x == '?'), res.Count);
            Assert.IsTrue(res.Values.All(x => x.All(y => y.Item2.Count > 0)));
            Assert.AreEqual(7, target.CurrentFactExpansionCacheSize);
        }

        [Test]
        public void Universes()
        {
            Universe target = new Universe();
            string src = @"
universe foo_base1 {foo(a1).}
universe foo_base2 {foo(a2).}
universe bar
{
    import foo_base1.
    fooR(X) :- X = a.
    import foo_base2.

    assert: fooR(X)?
    assert: foo(Y)?
}

% implicit default module that does not extend anything
foo (a, b).

assert: foo(X, Y)?
assert: not foo(X)?

import bar.
assert: fooR(X)?
assert: foo(X)?
assert: foo(X, _), fooR(X)?
assert: foo(X), not foo(X, Y)?
";
            _ = target.ExecuteAll(src);
            Assert.AreEqual(4, target.CurrentFactExpansionCacheSize);
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
        public void Remove()
        {
            Universe target = new Universe();
            string src = @"
foo (a, b).
foo (c, d).
bar(X) :- X = a.

assert: foo(X, Y)?
assert: bar(a)?

foo(a,b)~
assert: not foo(a,b)?

foo(X, Y)~
assert: not foo(c, d)?
";
            target.ExecuteAll(src);
        }

        [Test]
        public void RemoveImported()
        {
            Universe target = new Universe();
            string src = @"
root(g).

universe foo_base
{
    barF(a1).
    barR1(X) :- X = a.
    barR2(X) :- X = 2.
}
universe foo
{
    import foo_base.

    assert: barF(X)?
    assert: barR1(a)?
    assert: barR2(2)?
    assert: not root(S)?

    barF(a1)~
    assert: not barF(X)?
}
";
            _ = target.ExecuteAll(src);
        }

        [Test]
        public void Underscore()
        {
            Universe target = new Universe();
            string src = @"
bar(c, X) :- X=t, _ = a, _ = b.
car(X) :- _ = X, X=1, bar(_, _), _ = t.

assert: bar(X, _)?
assert: bar(_, _)?
assert: car(Y)?
";
            _ = target.ExecuteAll(src);
        }

        [Test]
        public void AtomArgs()
        {
            Universe target = new Universe();
            string src = @"
foo(a,b) :- a=b.
bar(c, X) :- X=t.

assert: not foo(X, Y)?
assert: bar(X, Y)?
";
            _ = target.ExecuteAll(src);
        }
    }
}