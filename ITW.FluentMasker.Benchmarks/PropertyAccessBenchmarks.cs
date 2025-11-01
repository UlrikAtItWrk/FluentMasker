using System;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using ITW.FluentMasker.Compilation;

namespace ITW.FluentMasker.Benchmarks
{
    /// <summary>
    /// Benchmarks comparing reflection-based property access vs compiled expression tree access.
    /// Expected result: Compiled accessors should be 10x+ faster than reflection.
    /// </summary>
    [SimpleJob(RuntimeMoniker.Net80)]
    [MemoryDiagnoser]
    [MarkdownExporter]
    public class PropertyAccessBenchmarks
    {
        private TestPerson _person;
        private PropertyAccessor<TestPerson> _compiledAccessor;
        private PropertyInfo _namePropertyInfo;
        private PropertyInfo _agePropertyInfo;
        private PropertyInfo _salaryPropertyInfo;

        public class TestPerson
        {
            public string Name { get; set; }
            public int Age { get; set; }
            public decimal Salary { get; set; }
            public DateTime BirthDate { get; set; }
            public bool IsActive { get; set; }
        }

        [GlobalSetup]
        public void Setup()
        {
            _person = new TestPerson
            {
                Name = "John Doe",
                Age = 30,
                Salary = 75000.50m,
                BirthDate = new DateTime(1993, 5, 15),
                IsActive = true
            };

            // Setup compiled accessor
            _compiledAccessor = new PropertyAccessor<TestPerson>();
            _compiledAccessor.CompileAccessors();

            // Setup reflection PropertyInfo objects
            _namePropertyInfo = typeof(TestPerson).GetProperty("Name");
            _agePropertyInfo = typeof(TestPerson).GetProperty("Age");
            _salaryPropertyInfo = typeof(TestPerson).GetProperty("Salary");
        }

        #region GetValue Benchmarks

        [Benchmark(Baseline = true)]
        public object ReflectionGetValue_StringProperty()
        {
            return _namePropertyInfo.GetValue(_person);
        }

        [Benchmark]
        public object CompiledGetValue_StringProperty()
        {
            return _compiledAccessor.GetValue(_person, "Name");
        }

        [Benchmark]
        public object ReflectionGetValue_IntProperty()
        {
            return _agePropertyInfo.GetValue(_person);
        }

        [Benchmark]
        public object CompiledGetValue_IntProperty()
        {
            return _compiledAccessor.GetValue(_person, "Age");
        }

        [Benchmark]
        public object ReflectionGetValue_DecimalProperty()
        {
            return _salaryPropertyInfo.GetValue(_person);
        }

        [Benchmark]
        public object CompiledGetValue_DecimalProperty()
        {
            return _compiledAccessor.GetValue(_person, "Salary");
        }

        #endregion

        #region SetValue Benchmarks

        [Benchmark]
        public void ReflectionSetValue_StringProperty()
        {
            _namePropertyInfo.SetValue(_person, "Jane Doe");
        }

        [Benchmark]
        public void CompiledSetValue_StringProperty()
        {
            _compiledAccessor.SetValue(_person, "Name", "Jane Doe");
        }

        [Benchmark]
        public void ReflectionSetValue_IntProperty()
        {
            _agePropertyInfo.SetValue(_person, 35);
        }

        [Benchmark]
        public void CompiledSetValue_IntProperty()
        {
            _compiledAccessor.SetValue(_person, "Age", 35);
        }

        [Benchmark]
        public void ReflectionSetValue_DecimalProperty()
        {
            _salaryPropertyInfo.SetValue(_person, 80000m);
        }

        [Benchmark]
        public void CompiledSetValue_DecimalProperty()
        {
            _compiledAccessor.SetValue(_person, "Salary", 80000m);
        }

        #endregion

        #region Iteration Benchmarks (Multiple Properties)

        [Benchmark]
        public void ReflectionIteration_AllProperties()
        {
            foreach (var property in typeof(TestPerson).GetProperties())
            {
                _ = property.GetValue(_person);
            }
        }

        [Benchmark]
        public void CompiledIteration_AllProperties()
        {
            foreach (var propertyName in _compiledAccessor.GetPropertyNames())
            {
                _ = _compiledAccessor.GetValue(_person, propertyName);
            }
        }

        #endregion

        #region Realistic Reflection Benchmarks (Without Caching)

        /// <summary>
        /// This benchmark represents real uncached reflection - calling typeof().GetProperty() each time.
        /// This is what happens in real code that doesn't cache PropertyInfo objects.
        /// </summary>
        [Benchmark]
        public object UncachedReflectionGetValue_StringProperty()
        {
            var propertyInfo = typeof(TestPerson).GetProperty("Name");
            return propertyInfo.GetValue(_person);
        }

        /// <summary>
        /// Comparison: Compiled accessor with the same property access.
        /// Shows the true performance benefit when reflection isn't pre-cached.
        /// </summary>
        [Benchmark]
        public object CompiledGetValue_StringProperty_Comparison()
        {
            return _compiledAccessor.GetValue(_person, "Name");
        }

        #endregion
    }
}
