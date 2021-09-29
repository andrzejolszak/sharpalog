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
universe foo_base1 {foo(a).}
universe bar extends foo_base1, foo_base1
{
    foo(X) :- X = a.
}

% implicit default module that does not extend anything
foo (a, b).

foo(X)?";
            var res = target.ExecuteAll(src);
            Assert.AreEqual(src.Count(x => x == '?'), res.Count);
            Assert.IsTrue(res.Values.All(x => x.All(y => y.Item2.Count > 0)));
        }

        [Test]
        public void Ids()
        {
            Universe target = new Universe();
            string src = @"
foo (a, b).
@ID123 foo(X) :- X = a.
@ID124
    foo(X, b) :- X = a.

foo(X, Y) :- X = a, @foo_1 Y = c.

foo(X)?";
            var res = target.ExecuteAll(src);
            Assert.AreEqual(src.Count(x => x == '?'), res.Count);
            Assert.IsTrue(res.Values.All(x => x.All(y => y.Item2.Count > 0)));
        }
    }
}