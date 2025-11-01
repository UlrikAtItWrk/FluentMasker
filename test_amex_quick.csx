#r "ITW.FluentMasker/bin/Debug/net8.0/ITW.FluentMasker.dll"
using ITW.FluentMasker.MaskRules;

var rule1 = new CardMaskRule(preserveGrouping: true);
var result1 = rule1.Apply("3782 822463 10005");
Console.WriteLine($"Actual  : '{result1}'");
Console.WriteLine("Expected: '*********** 10005'");

var rule2 = new CardMaskRule(keepFirst: 6, keepLast: 4, preserveGrouping: true);
var result2 = rule2.Apply("3782 822463 10005");
Console.WriteLine($"\nActual  : '{result2}'");
Console.WriteLine("Expected: '3782 82**** 10005'");
