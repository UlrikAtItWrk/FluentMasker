using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ITW.FluentMasker.Builders;
using Xunit;

namespace ITW.FluentMasker.UnitTests
{
    public class MaskingBuilderTests
    {
        #region Test Helper Classes

        /// <summary>
        /// Mock rule for testing that appends a suffix to demonstrate order of execution
        /// </summary>
        private class MockStringRule : IMaskRule<string, string>
        {
            private readonly string _suffix;

            public MockStringRule(string suffix)
            {
                _suffix = suffix;
            }

            public string Apply(string input)
            {
                return input + _suffix;
            }
        }

        /// <summary>
        /// Mock rule for testing generic builder with integers
        /// </summary>
        private class MockIntRule : IMaskRule<int, int>
        {
            private readonly int _increment;

            public MockIntRule(int increment)
            {
                _increment = increment;
            }

            public int Apply(int input)
            {
                return input + _increment;
            }
        }

        #endregion

        #region Generic MaskingBuilder Tests

        [Fact]
        public void Constructor_CreatesEmptyBuilder()
        {
            // Arrange & Act
            var builder = new MaskingBuilder<string, string>();

            // Assert
            var rules = builder.Build();
            Assert.NotNull(rules);
            Assert.Empty(rules);
        }

        [Fact]
        public void AddRule_SingleRule_AddsSuccessfully()
        {
            // Arrange
            var builder = new MaskingBuilder<string, string>();
            var rule = new MockStringRule("_A");

            // Act
            var result = builder.AddRule(rule);

            // Assert
            Assert.NotNull(result);
            Assert.Same(builder, result); // Verify it returns the same instance for chaining

            var rules = builder.Build();
            Assert.Single(rules);
            Assert.Same(rule, rules[0]);
        }

        [Fact]
        public void AddRule_MultipleRules_AccumulatesInOrder()
        {
            // Arrange
            var builder = new MaskingBuilder<string, string>();
            var rule1 = new MockStringRule("_A");
            var rule2 = new MockStringRule("_B");
            var rule3 = new MockStringRule("_C");

            // Act
            builder.AddRule(rule1);
            builder.AddRule(rule2);
            builder.AddRule(rule3);

            // Assert
            var rules = builder.Build();
            Assert.Equal(3, rules.Count);
            Assert.Same(rule1, rules[0]);
            Assert.Same(rule2, rules[1]);
            Assert.Same(rule3, rules[2]);
        }

        [Fact]
        public void AddRule_ThreeRules_PreservesOrderWhenApplied()
        {
            // Arrange
            var builder = new MaskingBuilder<string, string>();
            var rule1 = new MockStringRule("_First");
            var rule2 = new MockStringRule("_Second");
            var rule3 = new MockStringRule("_Third");

            // Act
            builder.AddRule(rule1);
            builder.AddRule(rule2);
            builder.AddRule(rule3);

            // Apply rules in order to verify execution order
            var rules = builder.Build();
            string result = "Start";
            foreach (var rule in rules)
            {
                result = rule.Apply(result);
            }

            // Assert
            Assert.Equal("Start_First_Second_Third", result);
        }

        [Fact]
        public void AddRule_ChainableCalls_WorksCorrectly()
        {
            // Arrange
            var builder = new MaskingBuilder<string, string>();
            var rule1 = new MockStringRule("_A");
            var rule2 = new MockStringRule("_B");
            var rule3 = new MockStringRule("_C");

            // Act - Chain all calls in a single statement
            var result = builder
                .AddRule(rule1)
                .AddRule(rule2)
                .AddRule(rule3);

            // Assert
            Assert.NotNull(result);
            Assert.Same(builder, result); // Verify fluent interface

            var rules = builder.Build();
            Assert.Equal(3, rules.Count);
            Assert.Same(rule1, rules[0]);
            Assert.Same(rule2, rules[1]);
            Assert.Same(rule3, rules[2]);
        }

        [Fact]
        public void Build_ReturnsImmutableList()
        {
            // Arrange
            var builder = new MaskingBuilder<string, string>();
            builder.AddRule(new MockStringRule("_A"));
            builder.AddRule(new MockStringRule("_B"));

            // Act
            var rules = builder.Build();

            // Assert
            Assert.IsAssignableFrom<IReadOnlyList<IMaskRule<string, string>>>(rules);
            Assert.Equal(2, rules.Count);

            // Verify it's a ReadOnlyCollection (immutable wrapper)
            Assert.IsType<ReadOnlyCollection<IMaskRule<string, string>>>(rules);
        }

        [Fact]
        public void Build_CalledMultipleTimes_ReturnsNewInstanceEachTime()
        {
            // Arrange
            var builder = new MaskingBuilder<string, string>();
            builder.AddRule(new MockStringRule("_A"));
            builder.AddRule(new MockStringRule("_B"));

            // Act
            var rules1 = builder.Build();
            var rules2 = builder.Build();

            // Assert
            Assert.NotSame(rules1, rules2); // Different instances
            Assert.Equal(rules1.Count, rules2.Count); // Same content
        }

        [Fact]
        public void Build_AddRulesAfterBuild_NewRulesNotInPreviousBuild()
        {
            // Arrange
            var builder = new MaskingBuilder<string, string>();
            builder.AddRule(new MockStringRule("_A"));

            // Act
            var rules1 = builder.Build();
            builder.AddRule(new MockStringRule("_B")); // Add after first build
            var rules2 = builder.Build();

            // Assert
            Assert.Single(rules1); // First build should have 1 rule
            Assert.Equal(2, rules2.Count); // Second build should have 2 rules
        }

        #endregion

        #region StringMaskingBuilder Tests

        [Fact]
        public void StringMaskingBuilder_InheritsFromGenericBuilder()
        {
            // Arrange & Act
            var builder = new StringMaskingBuilder();

            // Assert
            Assert.IsAssignableFrom<MaskingBuilder<string, string>>(builder);
        }

        [Fact]
        public void StringMaskingBuilder_AddRule_WorksCorrectly()
        {
            // Arrange
            var builder = new StringMaskingBuilder();
            var rule = new MockStringRule("_Test");

            // Act
            var result = builder.AddRule(rule);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<StringMaskingBuilder>(result); // Should return StringMaskingBuilder, not base class

            var rules = builder.Build();
            Assert.Single(rules);
            Assert.Same(rule, rules[0]);
        }

        [Fact]
        public void StringMaskingBuilder_ChainableMethodCalls_WorksCorrectly()
        {
            // Arrange
            var builder = new StringMaskingBuilder();
            var rule1 = new MockStringRule("_A");
            var rule2 = new MockStringRule("_B");

            // Act
            var result = builder
                .AddRule(rule1)
                .AddRule(rule2);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<StringMaskingBuilder>(result);

            var rules = builder.Build();
            Assert.Equal(2, rules.Count);
        }

        #endregion

        #region Generic Type Tests

        [Fact]
        public void MaskingBuilder_WithIntegerTypes_WorksCorrectly()
        {
            // Arrange
            var builder = new MaskingBuilder<int, int>();
            var rule1 = new MockIntRule(10);
            var rule2 = new MockIntRule(5);

            // Act
            builder.AddRule(rule1).AddRule(rule2);
            var rules = builder.Build();

            // Apply rules
            int result = 0;
            foreach (var rule in rules)
            {
                result = rule.Apply(result);
            }

            // Assert
            Assert.Equal(15, result); // 0 + 10 + 5 = 15
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void AddRule_SameRuleTwice_BothInstancesAdded()
        {
            // Arrange
            var builder = new MaskingBuilder<string, string>();
            var rule = new MockStringRule("_A");

            // Act
            builder.AddRule(rule);
            builder.AddRule(rule);

            // Assert
            var rules = builder.Build();
            Assert.Equal(2, rules.Count);
            Assert.Same(rule, rules[0]);
            Assert.Same(rule, rules[1]);
        }

        [Fact]
        public void Build_EmptyBuilder_ReturnsEmptyImmutableList()
        {
            // Arrange
            var builder = new MaskingBuilder<string, string>();

            // Act
            var rules = builder.Build();

            // Assert
            Assert.NotNull(rules);
            Assert.Empty(rules);
            Assert.IsAssignableFrom<IReadOnlyList<IMaskRule<string, string>>>(rules);
        }

        #endregion
    }
}
