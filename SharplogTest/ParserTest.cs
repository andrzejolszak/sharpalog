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
universe bar
{
    import foo_base1.
    fooR(X) :- X = a.
    import foo_base2.

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
@Aas_1:
    foo(X, B) :- X = a, B = b.

foo(X, Y) :- X = a, Y = c.

foo(X)?";
            var res = target.ExecuteAll(src);
            Assert.AreEqual(src.Count(x => x == '?'), res.Count);
            Assert.IsTrue(res.Values.All(x => x.All(y => y.Item2.Count > 0)));
        }

        [Test]
        public void Remove()
        {
            Universe target = new Universe();
            string src = @"
foo (a, b).
foo (c, d).
@id123: bar(X) :- X = a.

assert: foo(X, Y)?
assert: bar(a)?

foo(a,b)~
assert: not foo(a,b)?

foo(X, Y)~
assert: not foo(c, d)?

@id123~
assert: not bar(a)?
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
    @id1: barR2(X) :- X = 2.
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

    @id1~
    assert: not barR2(2)?
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