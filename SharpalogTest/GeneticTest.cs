using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Sharplog.Engine;
using Sharplog.Genetic;

namespace Sharplog
{
    public class GeneticTest
    {
        [Test]
        public void Clone()
        {
            Universe proto = new Universe();
            string src = string.Join("\r\n", File.ReadAllLines(ExamplesTest.ExamplesDir + "builtin.dl").Where(x => !x.Contains("?")));
            _ = proto.ExecuteAll(src);

            Assert.AreEqual(29, proto.Edb.Count);
            Assert.AreEqual(3, proto.Idb.Count);

            GeneticAlgorithm gen = new GeneticAlgorithm();
            Universe clone = gen.Clone(proto);

            Assert.AreEqual(29, clone.Edb.Count);
            Assert.AreEqual(3, clone.Idb.Count);
        }
    }
}