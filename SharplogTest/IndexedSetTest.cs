using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Sharplog.Engine;

namespace Sharplog
{
    public class IndexedSetTest
    {
        [Test]
        public void TestBase()
        {
            IndexedSet indexedSet = new IndexedSet();
            Assert.IsTrue((indexedSet.Count == 0));
            indexedSet.Add(Expr.CreateExpr("foo", "a"));
            indexedSet.Add(Expr.CreateExpr("foo", "b"));
            indexedSet.Add(Expr.CreateExpr("foo", "c"));
            indexedSet.Add(Expr.CreateExpr("bar", "a"));
            indexedSet.Add(Expr.CreateExpr("bar", "b"));
            Assert.IsFalse((indexedSet.Count == 0));
            Assert.IsTrue(indexedSet.GetIndexes().Count() == 2);
            Assert.IsTrue(indexedSet.GetIndexes().Contains("foo/1".GetHashCode()));
            Assert.IsTrue(indexedSet.GetIndexes().Contains("bar/1".GetHashCode()));
            Assert.IsFalse(indexedSet.GetIndexes().Contains("baz".GetHashCode()));
            Assert.IsFalse(indexedSet.GetIndexes().Contains("baz/1".GetHashCode()));
            HashSet<Expr> set = indexedSet.GetIndexed(new Expr("foo", "a"));
            Assert.IsTrue(set.Count == 3);
            Assert.IsTrue(set.Contains(Expr.CreateExpr("foo", "a")));
            Assert.IsTrue(set.Contains(Expr.CreateExpr("foo", "b")));
            Assert.IsTrue(set.Contains(Expr.CreateExpr("foo", "c")));
            Assert.IsFalse(set.Contains(Expr.CreateExpr("foo", "d")));
            Assert.IsTrue(indexedSet.Contains(Expr.CreateExpr("bar", "a")));
            indexedSet.RemoveTest(Expr.CreateExpr("bar", "a"));
            Assert.IsFalse(indexedSet.Contains(Expr.CreateExpr("bar", "a")));
            List<Expr> toRemove = new List<Expr>();
            toRemove.Add(Expr.CreateExpr("foo", "a"));
            toRemove.Add(Expr.CreateExpr("bar", "b"));
            Assert.IsTrue(indexedSet.ContainsAllTest(toRemove));
            toRemove.Add(Expr.CreateExpr("bar", "c"));
            Assert.IsFalse(indexedSet.ContainsAllTest(toRemove));
            indexedSet.RemoveAll(toRemove);
            Assert.IsFalse(indexedSet.GetIndexes().Contains("bar/1".GetHashCode()));
            Assert.IsFalse(indexedSet.Contains(Expr.CreateExpr("foo", "a")));
            Assert.IsFalse(indexedSet.Contains(Expr.CreateExpr("bar", "b")));
            Assert.IsFalse(indexedSet.RemoveAll(toRemove));
            indexedSet.ClearTest();
            Assert.IsTrue(indexedSet.Count == 0);
            Assert.IsTrue((indexedSet.Count == 0));
            Assert.IsTrue(indexedSet.GetIndexes().Count() == 0);
        }
    }
}