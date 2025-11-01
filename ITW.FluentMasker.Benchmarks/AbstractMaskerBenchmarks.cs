using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using ITW.FluentMasker;
using ITW.FluentMasker.MaskRules;
using ITW.FluentMasker.Extensions;

namespace ITW.FluentMasker.Benchmarks
{
    /// <summary>
    /// Benchmarks for the full AbstractMasker flow with object masking.
    /// Tests end-to-end performance of the masking pipeline.
    /// </summary>
    [SimpleJob(RuntimeMoniker.Net80)]
    [MemoryDiagnoser]
    [MarkdownExporter]
    public class AbstractMaskerBenchmarks
    {
        private Person _person;
        private PersonMasker _masker;
        private PersonWithBuilderMasker _builderMasker;

        public class Person
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }
            public int Age { get; set; }
            public decimal Salary { get; set; }
            public DateTime BirthDate { get; set; }
        }

        public class PersonMasker : AbstractMasker<Person>
        {
            public PersonMasker()
            {
                // Old API - direct rule passing
                MaskFor(x => x.FirstName, (IMaskRule)new MaskStartRule(2, "*"));
                MaskFor(x => x.LastName, (IMaskRule)new MaskEndRule(2, "*"));
                MaskFor(x => x.Email, (IMaskRule)new MaskMiddleRule(3, 3, "*"));
            }
        }

        public class PersonWithBuilderMasker : AbstractMasker<Person>
        {
            public PersonWithBuilderMasker()
            {
                // New Builder API
                MaskFor(x => x.FirstName, builder => builder
                    .MaskStart(2, "*"));

                MaskFor(x => x.LastName, builder => builder
                    .MaskEnd(2, "*"));

                MaskFor(x => x.Email, builder => builder
                    .MaskMiddle(3, 3, "*"));
            }
        }

        [GlobalSetup]
        public void Setup()
        {
            _person = new Person
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                Age = 35,
                Salary = 75000.50m,
                BirthDate = new DateTime(1988, 6, 15)
            };

            _masker = new PersonMasker();
            _builderMasker = new PersonWithBuilderMasker();

            // Warmup
            _masker.Mask(_person);
            _builderMasker.Mask(_person);
        }

        [Benchmark(Baseline = true, Description = "Mask Person with old API (direct rules)")]
        public MaskingResult Mask_Person_OldAPI() => _masker.Mask(_person);

        [Benchmark(Description = "Mask Person with Builder API")]
        public MaskingResult Mask_Person_BuilderAPI() => _builderMasker.Mask(_person);

        [Benchmark(Description = "Mask Person - measure serialization overhead")]
        public string Mask_Person_WithSerialization()
        {
            var result = _masker.Mask(_person);
            return result.MaskedData;
        }
    }
}
