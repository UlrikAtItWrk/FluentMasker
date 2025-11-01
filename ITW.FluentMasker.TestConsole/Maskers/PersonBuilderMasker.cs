using ITW.FluentMasker.MaskRules;
using ITW.FluentMasker.TestConsole.Models;
using ITW.FluentMasker.Extensions;

namespace ITW.FluentMasker.TestConsole.Maskers
{
    /// <summary>
    /// Demonstrates both the old API and new builder API working side-by-side.
    /// This masker uses the fluent builder API introduced in Task 1.2.3.
    /// </summary>
    public class PersonBuilderMasker : AbstractMasker<Person>
    {
        public void Initialize()
        {
            // OLD API - Direct rule instantiation (still works!)
            MaskFor(x => x.FirstName, (IMaskRule)new MaskFirstRule(2, "*"));

            // NEW API - Fluent builder with chaining
            MaskFor(x => x.LastName, m => m
                .MaskStart(2)
                .MaskEnd(2));

            // NEW API - More complex chaining
            MaskFor(x => x.Address, m => m
                .KeepFirst(4)
                .KeepLast(3));

            // NEW API - Single rule via builder
            MaskFor(x => x.ZipCode, m => m.MaskMiddle(1, 1));

            // NEW API - Multiple rules applied sequentially
            MaskFor(x => x.City, m => m
                .MaskStart(1)
                .MaskEnd(1)
                .KeepFirst(2));
        }
    }
}
