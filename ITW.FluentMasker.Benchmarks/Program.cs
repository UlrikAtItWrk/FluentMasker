using BenchmarkDotNet.Running;
using System.Reflection;

namespace ITW.FluentMasker.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            // Use BenchmarkSwitcher to allow running all benchmarks or selecting specific ones
            // Run with: dotnet run -c Release
            // Or select specific benchmark: dotnet run -c Release --filter *MaskRule*
            var switcher = BenchmarkSwitcher.FromAssembly(Assembly.GetExecutingAssembly());
            switcher.Run(args);
        }
    }
}
