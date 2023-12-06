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
        public void Objects()
        {
            Universe target = new Universe();
            string src = @"
object foo_base1 {foo(a1).}
object o123 {foo(a2).}
object gen_guid
{
    foo(abc).
    bar(abcx).
}

assert: foo(foo_base1, a1)?
assert: foo(o123, a2)?
assert: foo(ID, abc), bar(ID, abcx), ID <> gen_guid?
assert: object(X), count = 3?
";
            _ = target.ExecuteAll(src);
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
assert: 2 > 1?
assert: not 2 < 1?
assert: X = 2, Y = 1, X > Y?
assert: foo(a, b), count = 1?
assert: foo(a, b), count > 0?
assert: foo(a, b), count != 0?
assert: foo(a, b), count <> 0?
assert: foo(a, b), count < 2?
assert: foo(a, b), count >= 1?
assert: 2 < 1, count = 0?
assert: 2 < 1, count < 1?

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

f(c).
foo(X, Y), f(X)~
assert: not foo(c, d), not f(c)?
";
            target.ExecuteAll(src);
        }

        [Test]
        public void VariableBindingNameHandling()
        {
            Universe target = new Universe();
            string src = @"
foo(a, b).
bar(a, b).

baz(X, Y) :- taz(X, Y).
taz(Y, X) :- faz(Y, X).
faz(X, Y) :- foo(X, Y).

assert: faz(a, b)?
assert: taz(a, b)?
assert: baz(a, b)?

baz2(X, Y) :- taz2(Y, X).
taz2(X, Y) :- faz2(X, Y).
faz2(X, Y) :- foo(Y, X).

assert: faz2(b, a)?
assert: taz2(b, a)?
assert: baz2(a, b)?

foo2(c,c).

assert: foo2(X,X)?
assert: foo2(X,Y)?
assert: foo2(_,X)?
assert: foo2(X,_)?
assert: not foo(X,X)?
";
            target.ExecuteAll(src);
        }

        [Test]
        public void LongRule()
        {
            Universe target = new Universe();
            string src = @"
foo(a).
foo(b).
foo(c).
foo(d).
bar(b).
bar(c).
bar(d).
car(c).
car(d).
dar(d).

test(a, b, c, X) :- foo(X), bar(X), car(X), dar(X).
test2(X, a, b, c) :- foo(X), bar(X), car(X), dar(X), test(Y, Z, C, D).
test3(X, a, b, c) :- test(Y, Z, C, X), foo(X), bar(X), car(X), dar(X), test2(X, Y, Z, C).

assert: test(a, b, c, d)?
assert: test2(d, a, b, c)?
assert: test3(d, a, b, c)?
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
atom(a2SDA_213).
foo(a,b) :- a=b.
bar(c, X) :- X=t.

assert: not foo(X, Y)?
assert: bar(X, Y)?
";
            _ = target.ExecuteAll(src);
        }
    }
}