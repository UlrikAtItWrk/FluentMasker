using System;
using System.Diagnostics;
using ITW.FluentMasker.MaskRules;

class Program
{
    static void Main()
    {
        var rule = new BlacklistCharsRule("@.", "*");
        var testString = "Email: test@example.com";
        var iterations = 1000000;

        // Warmup
        for (int i = 0; i < 10000; i++)
        {
            rule.Apply(testString);
        }

        // Measure
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            rule.Apply(testString);
        }
        sw.Stop();

        var opsPerSecond = iterations / sw.Elapsed.TotalSeconds;
        Console.WriteLine($"BlacklistCharsRule Performance:");
        Console.WriteLine($"  Iterations: {iterations:N0}");
        Console.WriteLine($"  Time: {sw.Elapsed.TotalMilliseconds:F2} ms");
        Console.WriteLine($"  Operations/second: {opsPerSecond:N0}");
        Console.WriteLine($"  Time per operation: {sw.Elapsed.TotalMilliseconds / iterations * 1000:F2} µs");
        Console.WriteLine($"  Target: 100,000 ops/sec");
        Console.WriteLine($"  Result: {(opsPerSecond >= 100000 ? "PASS" : "FAIL")}");
    }
}
