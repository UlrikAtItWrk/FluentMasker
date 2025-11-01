using System;
using ITW.FluentMasker.MaskRules;

var rule1 = new MaskStartRule(3, "XX");
Console.WriteLine("Test 'TestString' with mask 'XX', count 3:");
Console.WriteLine($"Result: '{rule1.Apply("TestString")}'");
Console.WriteLine($"Expected in test: 'XXXXXXString'");
Console.WriteLine();

var rule2 = new MaskStartRule(2, "🎭");
Console.WriteLine("Test 'Hello' with emoji mask, count 2:");
try {
    var result = rule2.Apply("Hello");
    Console.WriteLine($"Result: '{result}'");
    Console.WriteLine($"Result length: {result.Length}");
} catch (Exception ex) {
    Console.WriteLine($"Error: {ex.Message}");
}
