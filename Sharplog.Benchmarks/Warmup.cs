namespace Sharplog.Benchmarks
{
    using BenchmarkDotNet.Attributes;

    [Config(typeof(DefaultConfig))]
    public class Warmup
    {
        private const int Iterations = 10000;

        [Benchmark(OperationsPerInvoke = Iterations)]
        public int JustCreate()
        {
            Jatalog target = new Jatalog();
            return target.GetType().GetHashCode();
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public int CreateAndWarmup()
        {
            Jatalog target = new Jatalog();
            target.ExecuteAll("foo(bar).");
            return target.GetType().GetHashCode();
        }
    }
}