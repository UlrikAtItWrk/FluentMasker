using System;
using ITW.FluentMasker.MaskRules;

class TestMaskStart
{
    static void Main()
    {
        var rule = new MaskStartRule(3, "*");
        
        // Test cases
        Console.WriteLine("Test 1 - Normal: " + rule.Apply("HelloWorld")); // Expected: ***loWorld
        Console.WriteLine("Test 2 - Count = 0: " + rule.Apply("Hello")); // Expected: Hello
        Console.WriteLine("Test 3 - Count >= Length: " + rule.Apply("Hi")); // Expected: **
        Console.WriteLine("Test 4 - Empty string: " + (rule.Apply("") == "" ? "PASS" : "FAIL"));
        Console.WriteLine("Test 5 - Null string: " + (rule.Apply(null) == null ? "PASS" : "FAIL"));
        
        // Test MaskFirstRule backward compatibility
        var oldRule = new MaskFirstRule(2, "#");
        Console.WriteLine("Test 6 - MaskFirstRule (backward compat): " + oldRule.Apply("Test")); // Expected: ##st
        
        Console.WriteLine("\nAll tests completed!");
    }
}
