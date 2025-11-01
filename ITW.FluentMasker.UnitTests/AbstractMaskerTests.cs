using System;
using ITW.FluentMasker;
using ITW.FluentMasker.MaskRules;
using Newtonsoft.Json;
using Xunit;

namespace ITW.FluentMasker.UnitTests
{
    public class AbstractMaskerTests
    {
        #region Test Models

        public class PersonWithMixedTypes
        {
            public string Name { get; set; }
            public int Age { get; set; }
            public decimal Salary { get; set; }
            public DateTime BirthDate { get; set; }
            public double Height { get; set; }
        }

        public class PersonMasker : AbstractMasker<PersonWithMixedTypes>
        {
            public PersonMasker()
            {
                // Mask the Name property using existing string rule
                MaskFor(p => p.Name, (IMaskRule)new MaskFirstRule(2, "*"));
            }
        }

        public class PersonMaskerWithInclude : AbstractMasker<PersonWithMixedTypes>
        {
            public PersonMaskerWithInclude()
            {
                SetPropertyRuleBehavior(PropertyRuleBehavior.Include);
                // Only mask the Name property
                MaskFor(p => p.Name, (IMaskRule)new MaskFirstRule(2, "*"));
            }
        }

        #endregion

        #region Backward Compatibility Tests

        [Fact]
        public void Mask_ExistingStringBasedMasking_StillWorks()
        {
            // Arrange
            var masker = new PersonMasker();
            var person = new PersonWithMixedTypes
            {
                Name = "John",
                Age = 30,
                Salary = 50000m,
                BirthDate = new DateTime(1993, 1, 15),
                Height = 1.75
            };

            // Act
            var result = masker.Mask(person);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Empty(result.Errors);

            // Verify the JSON directly (default Exclude mode sets unmapped properties to null)
            Assert.Contains("\"Name\":\"**hn\"", result.MaskedData);
            Assert.Contains("\"Age\":null", result.MaskedData);
            Assert.Contains("\"Salary\":null", result.MaskedData);
        }

        #endregion

        #region PropertyRuleBehavior.Include Bug Fix Tests

        [Fact]
        public void Mask_IncludeMode_WithMixedTypes_NoInvalidCastException()
        {
            // Arrange - This is the critical bug fix test
            // Before the fix, this would throw InvalidCastException when trying to cast int/decimal/DateTime to string
            var masker = new PersonMaskerWithInclude();
            var person = new PersonWithMixedTypes
            {
                Name = "Alice",
                Age = 25,
                Salary = 75000.50m,
                BirthDate = new DateTime(1998, 5, 20),
                Height = 1.68
            };

            // Act & Assert - Should NOT throw InvalidCastException
            var result = masker.Mask(person);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Empty(result.Errors);

            var masked = JsonConvert.DeserializeObject<PersonWithMixedTypes>(result.MaskedData);
            Assert.NotNull(masked);
            Assert.Equal("**ice", masked.Name); // Name is masked
            Assert.Equal(25, masked.Age); // Age is included as-is
            Assert.Equal(75000.50m, masked.Salary); // Salary is included as-is
            Assert.Equal(new DateTime(1998, 5, 20), masked.BirthDate); // BirthDate is included as-is
            Assert.Equal(1.68, masked.Height); // Height is included as-is
        }

        [Fact]
        public void Mask_IncludeMode_PreservesAllNonMaskedPropertyTypes()
        {
            // Arrange
            var masker = new PersonMaskerWithInclude();
            var person = new PersonWithMixedTypes
            {
                Name = "Bob",
                Age = 45,
                Salary = 120000m,
                BirthDate = new DateTime(1978, 11, 3),
                Height = 1.82
            };

            // Act
            var result = masker.Mask(person);

            // Assert
            Assert.True(result.IsSuccess);

            // Verify the JSON contains all properties with correct types
            dynamic masked = JsonConvert.DeserializeObject(result.MaskedData);
            Assert.NotNull(masked);

            // Verify types are preserved in JSON
            Assert.Equal("**b", (string)masked.Name);
            Assert.Equal(45, (int)masked.Age);
            Assert.Equal(120000m, (decimal)masked.Salary);
            Assert.Equal(1.82, (double)masked.Height);
        }

        #endregion

        #region PropertyRuleBehavior Tests

        [Fact]
        public void Mask_ExcludeMode_UnmaskedPropertiesAreNull()
        {
            // Arrange
            var masker = new PersonMasker(); // Default is Exclude
            var person = new PersonWithMixedTypes
            {
                Name = "Charlie",
                Age = 35,
                Salary = 90000m,
                BirthDate = new DateTime(1988, 7, 12),
                Height = 1.77
            };

            // Act
            var result = masker.Mask(person);

            // Assert
            Assert.True(result.IsSuccess);

            // Verify the JSON directly (Exclude mode sets unmapped properties to null)
            Assert.Contains("\"Name\":\"**arlie\"", result.MaskedData); // Name is masked
            Assert.Contains("\"Age\":null", result.MaskedData); // Age is excluded (null)
            Assert.Contains("\"Salary\":null", result.MaskedData); // Salary is excluded (null)
            Assert.Contains("\"BirthDate\":null", result.MaskedData); // BirthDate is excluded (null)
            Assert.Contains("\"Height\":null", result.MaskedData); // Height is excluded (null)
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void Mask_ComplexScenario_MixedTypesWithInclude_Success()
        {
            // Arrange - Real-world scenario with multiple data types
            var masker = new PersonMaskerWithInclude();
            var person = new PersonWithMixedTypes
            {
                Name = "Jennifer",
                Age = 28,
                Salary = 85000.75m,
                BirthDate = new DateTime(1995, 3, 8),
                Height = 1.65
            };

            // Act
            var result = masker.Mask(person);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Empty(result.Errors);
            Assert.NotNull(result.MaskedData);
            Assert.Contains("**nnifer", result.MaskedData); // Masked name
            Assert.Contains("28", result.MaskedData); // Age as number
            Assert.Contains("85000.75", result.MaskedData); // Salary as number
        }

        #endregion
    }
}
