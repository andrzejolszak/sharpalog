namespace Sharplog
{
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Newtonsoft.Json;
    using NUnit.Framework;

    public class ExamplesTest
    {
        private string ExamplesDir = Assembly.GetAssembly(typeof(ExamplesTest)).Location.Replace("SharplogTest.dll", "../../../examples/");

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void BuiltinExample(bool bottomUp)
        {
            Universe target = new Universe(bottomUp);
            string src = File.ReadAllText(ExamplesDir + "builtin.dl");
            var res = target.ExecuteAll(src);
            Assert.AreEqual(src.Count(x => x == '?'), res.Count);
            Assert.IsTrue(res.Values.All(x => x.All(y => y.Item2.Count > 0)));
        }

        [Test]
        public void VarVarUnification()
        {
            Universe target = new Universe();
            string src = @"
fooo(X, Y) :- barr(X), barr(Y), X = Y.
fooo2(X, Y) :- X = Y, barr(X), barr(Y).
barr(1).
barr(2).
fooo(X, Y)?
fooo2(X, Y)?";

            var res = target.ExecuteAll(src);
            Assert.AreEqual(2, res.Count);
            Assert.IsTrue(res.Values.All(x => x.Count == 2));
        }

        [Test]
        public void VarVarNegUnification()
        {
            Universe target = new Universe();
            string src = @"
fooo(X, Y) :- barr(X), barr(Y), not X = Y.
fooo2(X, Y) :- not X = Y, barr(X), barr(Y).
barr(1).
barr(2).
fooo(X, Y)?
fooo2(X, Y)?";

            var res = target.ExecuteAll(src);
            Assert.AreEqual(2, res.Count);
            Assert.IsTrue(res.Values.All(x => x.Count == 2));
        }

        [Test]
        public void VarVarNotUnification()
        {
            Universe target = new Universe();
            string src = @"
fooo(X, Y) :- barr(X), barr(Y), X <> Y.
fooo2(X, Y) :- X <> Y, barr(X), barr(Y).
barr(1).
barr(2).
fooo(X, Y)?
fooo2(X, Y)?";

            var res = target.ExecuteAll(src);
            Assert.AreEqual(2, res.Count);
            Assert.IsTrue(res.Values.All(x => x.Count == 2));
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void Noun(bool bottomUp)
        {
            Universe target = new Universe(bottomUp);
            string src = File.ReadAllText(ExamplesDir + "noun.dl");
            var res = target.ExecuteAll(src);
            Assert.AreEqual(src.Count(x => x == '?'), res.Count);
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
            Universe target = new Universe(true);
            string src = File.ReadAllText(ExamplesDir + "builtin.dl");
            src = string.Join("\r\n", src.Split(new char[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries).Where(x => !x.Contains("?")));
            var res = target.ExecuteAll(src + "\r\n" + query);
            Assert.AreEqual(results, res.Single().Value.Count);

            var resTD = new Universe(false).ExecuteAll(src + "\r\n" + query);
            string jsonBU = JsonConvert.SerializeObject(res);
            string jsonTD = JsonConvert.SerializeObject(resTD);
            Assert.AreEqual(jsonBU, jsonTD);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void CerinegExample(bool bottomUp)
        {
            Universe target = new Universe(bottomUp);
            string src = File.ReadAllText(ExamplesDir + "cerineg.dl");
            var res = target.ExecuteAll(src);
            Assert.AreEqual(src.Count(x => x == '?'), res.Count);
            Assert.IsTrue(res.Values.All(x => x.All(y => y.Item2.Count > 0)));
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void FamilyExample(bool bottomUp)
        {
            Universe target = new Universe(bottomUp);
            string src = File.ReadAllText(ExamplesDir + "family.dl");
            var res = target.ExecuteAll(src);
            Assert.AreEqual(src.Count(x => x == '?') - 1, res.Count);
            Assert.IsTrue(res.Values.All(x => x.All(y => y.Item2.Count > 0 || y.Item1.ToString().All(c => !char.IsUpper(c)))));
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void GensExample(bool bottomUp)
        {
            Universe target = new Universe(bottomUp);
            string src = File.ReadAllText(ExamplesDir + "gens.dl");
            var res = target.ExecuteAll(src);
            Assert.AreEqual(src.Count(x => x == '?'), res.Count);
            Assert.IsTrue(res.Values.All(x => x.All(y => y.Item2.Count > 0)));
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void NegateExample(bool bottomUp)
        {
            Universe target = new Universe(bottomUp);
            string src = File.ReadAllText(ExamplesDir + "negate.dl");
            var res = target.ExecuteAll(src);
            Assert.AreEqual(src.Count(x => x == '?'), res.Count);
            Assert.IsTrue(res.Values.All(x => x.All(y => y.Item2.Count > 0)));
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void NumbersExample(bool bottomUp)
        {
            Universe target = new Universe(bottomUp);
            string src = File.ReadAllText(ExamplesDir + "numbers.dl");
            var res = target.ExecuteAll(src);
            Assert.AreEqual(src.Count(x => x == '?') - 1, res.Count);
            Assert.IsTrue(res.Values.All(x => x.All(y => y.Item2.Count > 0)));
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void PathsExample(bool bottomUp)
        {
            Universe target = new Universe(bottomUp);
            string src = File.ReadAllText(ExamplesDir + "paths.dl");
            var res = target.ExecuteAll(src);
            Assert.AreEqual(src.Count(x => x == '?') - 1, res.Count);
            Assert.IsTrue(res.Values.All(x => x.All(y => y.Item2.Count > 0 || y.Item1.ToString().All(c => !char.IsUpper(c)))));
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void TumDeExample(bool bottomUp)
        {
            Universe target = new Universe(bottomUp);
            string src = File.ReadAllText(ExamplesDir + "tum.de.dl");
            var res = target.ExecuteAll(src);
            Assert.AreEqual(src.Count(x => x == '?') - 1, res.Count);
            Assert.IsTrue(res.Values.All(x => x.All(y => y.Item2.Count > 0)));
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void StratifiedExample(bool bottomUp)
        {
            Universe target = new Universe(bottomUp);
            string src = File.ReadAllText(ExamplesDir + "stratified.dl");
            var res = target.ExecuteAll(src);
            Assert.AreEqual(src.Count(x => x == '?') - 1, res.Count);
            Assert.IsTrue(res.Values.All(x => x.All(y => y.Item2.Count > 0)));
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void Stratified2Example(bool bottomUp)
        {
            /*
            Stratification:
            P1 = {link,linked,station,circumvent,connected}
            P2 = {cutpoint,has_icut_point}
            P3 = {safely_connected}
            */

            Universe target = new Universe(bottomUp);
            string src = File.ReadAllText(ExamplesDir + "stratified2.dl");
            var res = target.ExecuteAll(src);
            Assert.AreEqual(src.Count(x => x == '?') - 1, res.Count);
            Assert.IsTrue(res.Values.All(x => x.All(y => y.Item2.Count > 0)));
        }

        [Test]
        public void TopDown()
        {
            string src = File.ReadAllText(ExamplesDir + "topDown.dl");
            var resBU = new Universe(true).ExecuteAll(src);
            var resTD = new Universe(false).ExecuteAll(src);

            Assert.AreEqual(resBU.Count, resTD.Count);
            string jsonBU = JsonConvert.SerializeObject(resBU);
            string jsonTD = JsonConvert.SerializeObject(resTD);
            Assert.AreEqual(jsonBU, jsonTD);
        }
    }
}