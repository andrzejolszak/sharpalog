using System;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;

namespace Sharplog.Benchmarks
{
    internal class Program
    {
        public static void Main(string[] args)
        {
#if DEBUG
            throw new InvalidOperationException("Only run from Release builds!");
#endif

            if (System.Diagnostics.Debugger.IsAttached)
            {
                throw new InvalidOperationException("Only run without a debugger attached!");
            }

            Console.SetWindowSize(140, 30);

            Type[] benchmarks = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Public).Any(m => m.GetCustomAttributes(typeof(BenchmarkAttribute), false).Any()))
                .OrderBy(t => t.Namespace)
                .ThenBy(t => t.Name)
                .ToArray();

            BenchmarkSwitcher benchmarkSwitcher = new BenchmarkSwitcher(benchmarks);
            benchmarkSwitcher.Run();
        }
    }

    public class DefaultConfig : ManualConfig
    {
        public DefaultConfig()
        {
            this.UnionRule = ConfigUnionRule.AlwaysUseLocal;

            Add(JitOptimizationsValidator.FailOnError);
            Add(ExecutionValidator.FailOnError);

            Add(TargetMethodColumn.Method);
            Add(new ParamColumn("DataSource"));
            Add(BaselineScaledColumn.Scaled);
            Add(StatisticColumn.Median);
            Add(StatisticColumn.P90);

            Add(BenchmarkDotNet.Diagnosers.MemoryDiagnoser.Default);
            Add(BenchmarkDotNet.Diagnosers.MemoryDiagnoser.Default.GetColumnProvider());

            Add(Job.Dry
                .With(Platform.X64)
                .With(Jit.RyuJit)
                .With(Runtime.Clr)
                .WithGcServer(true)
                .WithWarmupCount(1)
                .WithLaunchCount(1)
                .WithTargetCount(100)
                .WithRemoveOutliers(true)
                .WithAnalyzeLaunchVariance(true)
                .WithEvaluateOverhead(true));

            Add(ConsoleLogger.Default);
            Add(AsciiDocExporter.Default);
        }
    }
}