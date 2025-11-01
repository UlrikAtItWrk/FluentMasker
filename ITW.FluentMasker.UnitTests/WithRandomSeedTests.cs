using System;
using System.Linq;
using ITW.FluentMasker.Builders;
using ITW.FluentMasker.Extensions;
using ITW.FluentMasker.MaskRules;
using Xunit;

namespace ITW.FluentMasker.UnitTests
{
    /// <summary>
    /// Unit tests for WithRandomSeed builder extension methods.
    /// Tests deterministic seeding functionality for both numeric and string builders.
    /// </summary>
    public class WithRandomSeedTests
    {
        #region NumericMaskingBuilder Tests

        [Fact]
        public void WithRandomSeed_NumericBuilder_WithSeedProvider_AppliesSeedException()
        {
            // Arrange
            SeedProvider<decimal> seedProvider = value => value.GetHashCode();

            // Act
            var builder = new NumericMaskingBuilder<decimal>()
                .WithRandomSeed(seedProvider)
                .NoiseAdditive(1000);

            var rules = builder.Build();
            var rule = rules[0] as ISeededMaskRule<decimal>;

            // Assert
            Assert.NotNull(rule);
            Assert.NotNull(rule.SeedProvider);
            Assert.Equal(seedProvider, rule.SeedProvider);
        }

        [Fact]
        public void WithRandomSeed_NumericBuilder_WithConstantSeed_AppliesConstantSeed()
        {
            // Arrange
            int constantSeed = 12345;

            // Act
            var builder = new NumericMaskingBuilder<decimal>()
                .WithRandomSeed(constantSeed)
                .NoiseAdditive(1000);

            var rules = builder.Build();
            var rule = rules[0] as ISeededMaskRule<decimal>;

            // Assert
            Assert.NotNull(rule);
            Assert.NotNull(rule.SeedProvider);

            // Verify that the seed provider returns the constant seed
            int resultSeed = rule.SeedProvider(75000m);
            Assert.Equal(constantSeed, resultSeed);
        }

        [Fact]
        public void WithRandomSeed_NumericBuilder_ProducesDeterministicOutput()
        {
            // Arrange
            var builder1 = new NumericMaskingBuilder<decimal>()
                .WithRandomSeed(value => 42) // Constant seed function
                .NoiseAdditive(5000);

            var builder2 = new NumericMaskingBuilder<decimal>()
                .WithRandomSeed(value => 42) // Same seed
                .NoiseAdditive(5000);

            var rule1 = builder1.Build()[0];
            var rule2 = builder2.Build()[0];

            decimal input = 75000m;

            // Act
            var result1 = rule1.Apply(input);
            var result2 = rule2.Apply(input);

            // Assert
            Assert.Equal(result1, result2); // Same seed produces same output
        }

        [Fact]
        public void WithRandomSeed_NumericBuilder_DifferentSeeds_ProduceDifferentOutput()
        {
            // Arrange
            var builder1 = new NumericMaskingBuilder<decimal>()
                .WithRandomSeed(100)
                .NoiseAdditive(5000);

            var builder2 = new NumericMaskingBuilder<decimal>()
                .WithRandomSeed(200)
                .NoiseAdditive(5000);

            var rule1 = builder1.Build()[0];
            var rule2 = builder2.Build()[0];

            decimal input = 75000m;

            // Act
            var result1 = rule1.Apply(input);
            var result2 = rule2.Apply(input);

            // Assert
            Assert.NotEqual(result1, result2); // Different seeds should produce different output
        }

        [Fact]
        public void WithRandomSeed_NumericBuilder_ChainsWithMultipleRules()
        {
            // Arrange & Act
            var builder = new NumericMaskingBuilder<decimal>()
                .WithRandomSeed(12345)
                .NoiseAdditive(5000)
                .RoundTo(1000m);

            var rules = builder.Build();

            // Assert
            Assert.Equal(2, rules.Count);

            // First rule should have seed provider
            var noiseRule = rules[0] as ISeededMaskRule<decimal>;
            Assert.NotNull(noiseRule);
            Assert.NotNull(noiseRule.SeedProvider);

            // Second rule (RoundTo) doesn't use seeding
            Assert.IsType<RoundToRule<decimal>>(rules[1]);
        }

        [Fact]
        public void WithRandomSeed_NumericBuilder_OnlyAppliesToNextSeededRule()
        {
            // Arrange & Act
            var builder = new NumericMaskingBuilder<decimal>()
                .WithRandomSeed(12345)
                .NoiseAdditive(5000)  // This gets the seed
                .NoiseAdditive(3000);  // This should NOT get the seed

            var rules = builder.Build();

            // Assert
            var firstRule = rules[0] as ISeededMaskRule<decimal>;
            var secondRule = rules[1] as ISeededMaskRule<decimal>;

            Assert.NotNull(firstRule.SeedProvider);  // First rule has seed
            Assert.Null(secondRule.SeedProvider);     // Second rule does NOT have seed
        }

        [Fact]
        public void WithRandomSeed_NumericBuilder_NullBuilder_ThrowsArgumentNullException()
        {
            // Arrange
            NumericMaskingBuilder<decimal> builder = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                builder.WithRandomSeed(12345));
        }

        [Fact]
        public void WithRandomSeed_NumericBuilder_NullSeedProvider_ThrowsArgumentNullException()
        {
            // Arrange
            var builder = new NumericMaskingBuilder<decimal>();
            SeedProvider<decimal> seedProvider = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                builder.WithRandomSeed(seedProvider));
        }

        [Fact]
        public void WithRandomSeed_NumericBuilder_ValueBasedSeeding_WorksCorrectly()
        {
            // Arrange
            SeedProvider<decimal> seedProvider = value => (int)value; // Use value itself as seed

            var builder = new NumericMaskingBuilder<decimal>()
                .WithRandomSeed(seedProvider)
                .NoiseAdditive(1000);

            var rule = builder.Build()[0];

            // Act - Same input should produce same output
            var result1a = rule.Apply(50000m);
            var result1b = rule.Apply(50000m);

            // Act - Different input should produce different output
            var result2a = rule.Apply(60000m);

            // Assert
            Assert.Equal(result1a, result1b);  // Same input = same output
            Assert.NotEqual(result1a, result2a);  // Different input = different output
        }

        #endregion

        #region StringMaskingBuilder Tests

        [Fact]
        public void WithRandomSeed_StringBuilder_WithConstantSeed_SetsBuilderProperty()
        {
            // Arrange
            int constantSeed = 54321;

            // Act
            var builder = new StringMaskingBuilder()
                .WithRandomSeed(constantSeed);

            // Assert
            Assert.NotNull(builder.PendingSeedProvider);
        }

        [Fact]
        public void WithRandomSeed_StringBuilder_WithSeedProvider_SetsBuilderProperty()
        {
            // Arrange
            SeedProvider<string> seedProvider = str => str.GetHashCode();

            // Act
            var builder = new StringMaskingBuilder()
                .WithRandomSeed(seedProvider);

            // Assert
            Assert.NotNull(builder.PendingSeedProvider);
            Assert.Equal(seedProvider, builder.PendingSeedProvider);
        }

        [Fact]
        public void WithRandomSeed_StringBuilder_NullBuilder_ThrowsArgumentNullException()
        {
            // Arrange
            StringMaskingBuilder builder = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                builder.WithRandomSeed(12345));
        }

        [Fact]
        public void WithRandomSeed_StringBuilder_NullSeedProvider_ThrowsArgumentNullException()
        {
            // Arrange
            var builder = new StringMaskingBuilder();
            SeedProvider<string> seedProvider = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                builder.WithRandomSeed(seedProvider));
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void WithRandomSeed_IntegrationTest_ComplexChaining()
        {
            // Arrange & Act - Complex chaining scenario
            var builder = new NumericMaskingBuilder<int>()
                .WithRandomSeed(999)
                .NoiseAdditive(100)      // Gets seed
                .RoundTo(10)              // No seed
                .WithRandomSeed(888)      // New seed for next rule
                .NoiseAdditive(50);       // Gets new seed

            var rules = builder.Build();

            // Assert
            Assert.Equal(3, rules.Count);

            var firstNoiseRule = rules[0] as ISeededMaskRule<int>;
            var roundRule = rules[1];
            var secondNoiseRule = rules[2] as ISeededMaskRule<int>;

            Assert.NotNull(firstNoiseRule.SeedProvider);
            Assert.IsType<RoundToRule<int>>(roundRule);
            Assert.NotNull(secondNoiseRule.SeedProvider);
        }

        [Fact]
        public void WithRandomSeed_IntegrationTest_NoiseAdditive_ProducesConsistentResults()
        {
            // Arrange
            var salaries = new[] { 50000m, 75000m, 100000m };

            var builder = new NumericMaskingBuilder<decimal>()
                .WithRandomSeed(42)
                .NoiseAdditive(5000);

            var rule = builder.Build()[0];

            // Act - Mask twice to verify consistency
            var firstPass = salaries.Select(s => rule.Apply(s)).ToArray();

            // Recreate rule with same seed
            var builder2 = new NumericMaskingBuilder<decimal>()
                .WithRandomSeed(42)
                .NoiseAdditive(5000);
            var rule2 = builder2.Build()[0];

            var secondPass = salaries.Select(s => rule2.Apply(s)).ToArray();

            // Assert - Same seed should produce identical results
            for (int i = 0; i < salaries.Length; i++)
            {
                Assert.Equal(firstPass[i], secondPass[i]);
            }
        }

        [Fact]
        public void WithRandomSeed_IntegrationTest_RoundAfterNoise_MaintainsRounding()
        {
            // Arrange
            var builder = new NumericMaskingBuilder<decimal>()
                .WithRandomSeed(42)
                .NoiseAdditive(5000)  // Add noise
                .RoundTo(1000m);       // Then round to nearest 1000

            var rules = builder.Build();
            decimal input = 75000m;

            // Act - Apply noise first, then round
            decimal afterNoise = rules[0].Apply(input);
            decimal afterRound = rules[1].Apply(afterNoise);

            // Assert - Result should be a multiple of 1000
            Assert.Equal(0, afterRound % 1000m);

            // And should be within reasonable range (70000-80000 after noise, then rounded)
            Assert.InRange(afterRound, 70000m, 80000m);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void WithRandomSeed_EdgeCase_MultipleSeeds_OnlyLastApplies()
        {
            // Arrange & Act
            var builder = new NumericMaskingBuilder<decimal>()
                .WithRandomSeed(100)
                .WithRandomSeed(200)  // This overwrites the first seed
                .NoiseAdditive(1000);

            var rules = builder.Build();
            var rule = rules[0] as ISeededMaskRule<decimal>;

            // Assert
            Assert.NotNull(rule.SeedProvider);

            // The seed provider should be the second one (200)
            int seed = rule.SeedProvider(75000m);
            Assert.Equal(200, seed);
        }

        [Fact]
        public void WithRandomSeed_EdgeCase_NoSeededRule_SeedProviderIgnored()
        {
            // Arrange & Act - Add WithRandomSeed but then only add non-seeded rules
            var builder = new NumericMaskingBuilder<decimal>()
                .WithRandomSeed(12345)
                .RoundTo(1000m);  // RoundToRule is NOT seeded

            var rules = builder.Build();

            // Assert - Rule should work normally, seed just not applied
            Assert.Single(rules);
            Assert.IsType<RoundToRule<decimal>>(rules[0]);
        }

        #endregion
    }
}
