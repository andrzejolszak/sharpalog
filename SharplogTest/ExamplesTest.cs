namespace Sharplog
{
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using NUnit.Framework;

    public class ExamplesTest
    {
        private string ExamplesDir = Assembly.GetAssembly(typeof(ExamplesTest)).Location.Replace("SharplogTest.dll", "../../../examples/");

        [Test]
        public void BuiltinExample()
        {
            Jatalog target = new Jatalog();
            string src = File.ReadAllText(ExamplesDir + "builtin.dl");
            var res = target.ExecuteAll(src);
            Assert.AreEqual(src.Count(x => x == '?'), res.Count);
            Assert.IsTrue(res.Values.All(x => x.All(y => y.Item2.Count > 0)));
        }

        [Test]
        [TestCase("parent(A,B)?", 3)]
        [TestCase("parent(A,bob)?", 1)]
        [TestCase("sibling(A,B)?", 6)]
        [TestCase("sibling(bob,Y)?", 2)]
        [TestCase("num(N, V), V = 5?", 1)]
        [TestCase("p(X), X = Y, q(Y)?", 1)]
        public void BuiltinExample(string query, int results)
        {
            Jatalog target = new Jatalog();
            string src = File.ReadAllText(ExamplesDir + "builtin.dl");
            src = string.Join("\r\n", src.Split(new char[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries).Where(x => !x.Contains("?")));
            var res = target.ExecuteAll(src + "\r\n" + query);
            Assert.AreEqual(results, res.Single().Value.Count);
        }

        [Test]
        public void CerinegExample()
        {
            Jatalog target = new Jatalog();
            string src = File.ReadAllText(ExamplesDir + "cerineg.dl");
            var res = target.ExecuteAll(src);
            Assert.AreEqual(src.Count(x => x == '?'), res.Count);
            Assert.IsTrue(res.Values.All(x => x.All(y => y.Item2.Count > 0)));
        }

        [Test]
        public void FamilyExample()
        {
            Jatalog target = new Jatalog();
            string src = File.ReadAllText(ExamplesDir + "family.dl");
            var res = target.ExecuteAll(src);
            Assert.AreEqual(src.Count(x => x == '?') - 1, res.Count);
            Assert.IsTrue(res.Values.All(x => x.All(y => y.Item2.Count > 0 || y.Item1.ToString().All(c => !char.IsUpper(c)))));
        }

        [Test]
        public void GensExample()
        {
            Jatalog target = new Jatalog();
            string src = File.ReadAllText(ExamplesDir + "gens.dl");
            var res = target.ExecuteAll(src);
            Assert.AreEqual(src.Count(x => x == '?'), res.Count);
            Assert.IsTrue(res.Values.All(x => x.All(y => y.Item2.Count > 0)));
        }

        [Test]
        public void NegateExample()
        {
            Jatalog target = new Jatalog();
            string src = File.ReadAllText(ExamplesDir + "negate.dl");
            var res = target.ExecuteAll(src);
            Assert.AreEqual(src.Count(x => x == '?'), res.Count);
            Assert.IsTrue(res.Values.All(x => x.All(y => y.Item2.Count > 0)));
        }

        [Test]
        public void NumbersExample()
        {
            Jatalog target = new Jatalog();
            string src = File.ReadAllText(ExamplesDir + "numbers.dl");
            var res = target.ExecuteAll(src);
            Assert.AreEqual(src.Count(x => x == '?') - 1, res.Count);
            Assert.IsTrue(res.Values.All(x => x.All(y => y.Item2.Count > 0)));
        }

        [Test]
        public void PathsExample()
        {
            Jatalog target = new Jatalog();
            string src = File.ReadAllText(ExamplesDir + "paths.dl");
            var res = target.ExecuteAll(src);
            Assert.AreEqual(src.Count(x => x == '?') - 1, res.Count);
            Assert.IsTrue(res.Values.All(x => x.All(y => y.Item2.Count > 0 || y.Item1.ToString().All(c => !char.IsUpper(c)))));
        }

        [Test]
        public void TumDeExample()
        {
            Jatalog target = new Jatalog();
            string src = File.ReadAllText(ExamplesDir + "tum.de.dl");
            var res = target.ExecuteAll(src);
            Assert.AreEqual(src.Count(x => x == '?') - 1, res.Count);
            Assert.IsTrue(res.Values.All(x => x.All(y => y.Item2.Count > 0)));
        }

        [Test]
        public void StratifiedExample()
        {
            Jatalog target = new Jatalog();
            string src = File.ReadAllText(ExamplesDir + "stratified.dl");
            var res = target.ExecuteAll(src);
            Assert.AreEqual(src.Count(x => x == '?') - 1, res.Count);
            Assert.IsTrue(res.Values.All(x => x.All(y => y.Item2.Count > 0)));
        }
    }
}