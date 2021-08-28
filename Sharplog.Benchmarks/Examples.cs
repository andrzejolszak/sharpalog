namespace Sharplog.Benchmarks
{
    using System.Collections.Generic;
    using System.IO;
    using BenchmarkDotNet.Attributes;

    [Config(typeof(DefaultConfig))]
    public class Examples
    {
        private const string ExamplesDir = "./../../../examples/";
        private const int Iterations = 1000;

        private Dictionary<string, string> _exampleSources = new Dictionary<string, string>();

        [Params("builtin.dl", "cerineg.dl", "family.dl", "gens.dl", "negate.dl", "numbers.dl", "paths.dl", "stratified.dl", "tum.de.dl")]
        public string DataSource { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            if (_exampleSources.Count == 0)
            {
                _exampleSources["builtin.dl"] = File.ReadAllText(ExamplesDir + "builtin.dl");
                _exampleSources["cerineg.dl"] = File.ReadAllText(ExamplesDir + "cerineg.dl");
                _exampleSources["family.dl"] = File.ReadAllText(ExamplesDir + "family.dl");
                _exampleSources["gens.dl"] = File.ReadAllText(ExamplesDir + "gens.dl");
                _exampleSources["negate.dl"] = File.ReadAllText(ExamplesDir + "negate.dl");
                _exampleSources["numbers.dl"] = File.ReadAllText(ExamplesDir + "numbers.dl");
                _exampleSources["paths.dl"] = File.ReadAllText(ExamplesDir + "paths.dl");
                _exampleSources["stratified.dl"] = File.ReadAllText(ExamplesDir + "stratified.dl");
                _exampleSources["tum.de.dl"] = File.ReadAllText(ExamplesDir + "tum.de.dl");
            }
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public int RunExample()
        {
            string data = this._exampleSources[this.DataSource];
            Jatalog target = new Jatalog();
            target.ExecuteAll(data);
            return target.GetType().GetHashCode();
        }
    }
}