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
            Universe target = new Universe();
            return target.GetType().GetHashCode();
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public int CreateAndWarmup()
        {
            Universe target = new Universe();
            target.ExecuteAll("foo(bar).");
            return target.GetType().GetHashCode();
        }
    }
}