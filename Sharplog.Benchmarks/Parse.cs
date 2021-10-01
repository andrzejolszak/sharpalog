namespace Sharplog.Benchmarks
{
    using System.Collections.Generic;
    using System.IO;
    using BenchmarkDotNet.Attributes;

    [Config(typeof(DefaultConfig))]
    public class Parse
    {
        private const string ExamplesDir = "./../../../examples/";
        private const int Iterations = 100;

        private Dictionary<string, string> _exampleSources = new Dictionary<string, string>();

        [Params("family.dl", "gens.dl", "tum.de.dl", "noun.dl")]
        public string DataSource { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            if (_exampleSources.Count == 0)
            {
                _exampleSources["family.dl"] = File.ReadAllText(ExamplesDir + "family.dl");
                _exampleSources["gens.dl"] = File.ReadAllText(ExamplesDir + "gens.dl");
                _exampleSources["tum.de.dl"] = File.ReadAllText(ExamplesDir + "tum.de.dl");
                _exampleSources["noun.dl"] = File.ReadAllText(ExamplesDir + "noun.dl");
            }
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public int RunExample()
        {
            string data = this._exampleSources[this.DataSource];
            Universe target = new Universe();
            target.ExecuteAll(data, parseOnly: true);
            return target.GetType().GetHashCode();
        }
    }
}