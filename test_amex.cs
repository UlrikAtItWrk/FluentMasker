using System;
using ITW.FluentMasker.MaskRules;

class TestAmex
{
    static void Main()
    {
        var rule1 = new CardMaskRule(preserveGrouping: true);
        var rule2 = new CardMaskRule(keepFirst: 6, keepLast: 4, preserveGrouping: true);
        
        Console.WriteLine("Test 1: '" + rule1.Apply("3782 822463 10005") + "'");
        Console.WriteLine("Expected: '*********** 10005'");
        
        Console.WriteLine("\nTest 2: '" + rule2.Apply("3782 822463 10005") + "'");
        Console.WriteLine("Expected: '3782 82**** 10005'");
    }
}
