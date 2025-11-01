using System;
using System.Collections.Generic;
using ITW.FluentMasker.Compilation;
using Xunit;

namespace ITW.FluentMasker.UnitTests
{
    /// <summary>
    /// Tests for PropertyAccessor<T> to verify compiled expression tree functionality
    /// </summary>
    public class PropertyAccessorTests
    {
        #region Test Models

        public class TestPerson
        {
            public string Name { get; set; }
            public int Age { get; set; }
            public decimal Salary { get; set; }
            public DateTime BirthDate { get; set; }
            public bool IsActive { get; set; }
            public double Height { get; set; }
            public string ReadOnlyProperty { get; }

            public TestPerson(string readOnlyValue = "ReadOnly")
            {
                ReadOnlyProperty = readOnlyValue;
            }
        }

        public class TestPersonWithNestedObject
        {
            public string Name { get; set; }
            public Address Address { get; set; }
        }

        public class Address
        {
            public string Street { get; set; }
            public string City { get; set; }
        }

        #endregion

        #region Compilation Tests

        [Fact]
        public void CompileAccessors_ShouldCompileWithoutError()
        {
            // Arrange
            var accessor = new PropertyAccessor<TestPerson>();

            // Act & Assert - Should not throw
            accessor.CompileAccessors();
        }

        [Fact]
        public void CompileAccessors_ShouldCompileForEmptyClass()
        {
            // Arrange
            var accessor = new PropertyAccessor<object>();

            // Act & Assert - Should not throw
            accessor.CompileAccessors();
        }

        #endregion

        #region GetValue Tests

        [Fact]
        public void GetValue_StringProperty_ReturnsCorrectValue()
        {
            // Arrange
            var accessor = new PropertyAccessor<TestPerson>();
            accessor.CompileAccessors();
            var person = new TestPerson { Name = "John Doe" };

            // Act
            var value = accessor.GetValue(person, "Name");

            // Assert
            Assert.Equal("John Doe", value);
        }

        [Fact]
        public void GetValue_IntProperty_ReturnsCorrectValue()
        {
            // Arrange
            var accessor = new PropertyAccessor<TestPerson>();
            accessor.CompileAccessors();
            var person = new TestPerson { Age = 30 };

            // Act
            var value = accessor.GetValue(person, "Age");

            // Assert
            Assert.Equal(30, value);
        }

        [Fact]
        public void GetValue_DecimalProperty_ReturnsCorrectValue()
        {
            // Arrange
            var accessor = new PropertyAccessor<TestPerson>();
            accessor.CompileAccessors();
            var person = new TestPerson { Salary = 75000.50m };

            // Act
            var value = accessor.GetValue(person, "Salary");

            // Assert
            Assert.Equal(75000.50m, value);
        }

        [Fact]
        public void GetValue_DateTimeProperty_ReturnsCorrectValue()
        {
            // Arrange
            var accessor = new PropertyAccessor<TestPerson>();
            accessor.CompileAccessors();
            var birthDate = new DateTime(1990, 5, 15);
            var person = new TestPerson { BirthDate = birthDate };

            // Act
            var value = accessor.GetValue(person, "BirthDate");

            // Assert
            Assert.Equal(birthDate, value);
        }

        [Fact]
        public void GetValue_BoolProperty_ReturnsCorrectValue()
        {
            // Arrange
            var accessor = new PropertyAccessor<TestPerson>();
            accessor.CompileAccessors();
            var person = new TestPerson { IsActive = true };

            // Act
            var value = accessor.GetValue(person, "IsActive");

            // Assert
            Assert.Equal(true, value);
        }

        [Fact]
        public void GetValue_DoubleProperty_ReturnsCorrectValue()
        {
            // Arrange
            var accessor = new PropertyAccessor<TestPerson>();
            accessor.CompileAccessors();
            var person = new TestPerson { Height = 1.75 };

            // Act
            var value = accessor.GetValue(person, "Height");

            // Assert
            Assert.Equal(1.75, value);
        }

        [Fact]
        public void GetValue_NonExistentProperty_ThrowsKeyNotFoundException()
        {
            // Arrange
            var accessor = new PropertyAccessor<TestPerson>();
            accessor.CompileAccessors();
            var person = new TestPerson();

            // Act & Assert
            Assert.Throws<KeyNotFoundException>(() => accessor.GetValue(person, "NonExistent"));
        }

        [Fact]
        public void GetValue_ReadOnlyProperty_ReturnsCorrectValue()
        {
            // Arrange
            var accessor = new PropertyAccessor<TestPerson>();
            accessor.CompileAccessors();
            var person = new TestPerson("TestValue");

            // Act
            var value = accessor.GetValue(person, "ReadOnlyProperty");

            // Assert
            Assert.Equal("TestValue", value);
        }

        [Fact]
        public void GetValue_NestedObject_ReturnsCorrectValue()
        {
            // Arrange
            var accessor = new PropertyAccessor<TestPersonWithNestedObject>();
            accessor.CompileAccessors();
            var address = new Address { Street = "Main St", City = "New York" };
            var person = new TestPersonWithNestedObject { Name = "John", Address = address };

            // Act
            var value = accessor.GetValue(person, "Address");

            // Assert
            Assert.Equal(address, value);
            Assert.Equal("Main St", ((Address)value).Street);
        }

        #endregion

        #region SetValue Tests

        [Fact]
        public void SetValue_StringProperty_SetsCorrectValue()
        {
            // Arrange
            var accessor = new PropertyAccessor<TestPerson>();
            accessor.CompileAccessors();
            var person = new TestPerson { Name = "John" };

            // Act
            accessor.SetValue(person, "Name", "Jane");

            // Assert
            Assert.Equal("Jane", person.Name);
        }

        [Fact]
        public void SetValue_IntProperty_SetsCorrectValue()
        {
            // Arrange
            var accessor = new PropertyAccessor<TestPerson>();
            accessor.CompileAccessors();
            var person = new TestPerson { Age = 30 };

            // Act
            accessor.SetValue(person, "Age", 35);

            // Assert
            Assert.Equal(35, person.Age);
        }

        [Fact]
        public void SetValue_DecimalProperty_SetsCorrectValue()
        {
            // Arrange
            var accessor = new PropertyAccessor<TestPerson>();
            accessor.CompileAccessors();
            var person = new TestPerson { Salary = 50000m };

            // Act
            accessor.SetValue(person, "Salary", 60000m);

            // Assert
            Assert.Equal(60000m, person.Salary);
        }

        [Fact]
        public void SetValue_DateTimeProperty_SetsCorrectValue()
        {
            // Arrange
            var accessor = new PropertyAccessor<TestPerson>();
            accessor.CompileAccessors();
            var person = new TestPerson { BirthDate = new DateTime(1990, 1, 1) };
            var newDate = new DateTime(1995, 5, 15);

            // Act
            accessor.SetValue(person, "BirthDate", newDate);

            // Assert
            Assert.Equal(newDate, person.BirthDate);
        }

        [Fact]
        public void SetValue_ReadOnlyProperty_ThrowsInvalidOperationException()
        {
            // Arrange
            var accessor = new PropertyAccessor<TestPerson>();
            accessor.CompileAccessors();
            var person = new TestPerson();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => accessor.SetValue(person, "ReadOnlyProperty", "NewValue"));
        }

        [Fact]
        public void SetValue_NonExistentProperty_ThrowsKeyNotFoundException()
        {
            // Arrange
            var accessor = new PropertyAccessor<TestPerson>();
            accessor.CompileAccessors();
            var person = new TestPerson();

            // Act & Assert
            Assert.Throws<KeyNotFoundException>(() => accessor.SetValue(person, "NonExistent", "value"));
        }

        #endregion

        #region GetPropertyInfo Tests

        [Fact]
        public void GetPropertyInfo_ExistingProperty_ReturnsPropertyInfo()
        {
            // Arrange
            var accessor = new PropertyAccessor<TestPerson>();
            accessor.CompileAccessors();

            // Act
            var propertyInfo = accessor.GetPropertyInfo("Name");

            // Assert
            Assert.NotNull(propertyInfo);
            Assert.Equal("Name", propertyInfo.Name);
            Assert.Equal(typeof(string), propertyInfo.PropertyType);
        }

        [Fact]
        public void GetPropertyInfo_NonExistentProperty_ThrowsKeyNotFoundException()
        {
            // Arrange
            var accessor = new PropertyAccessor<TestPerson>();
            accessor.CompileAccessors();

            // Act & Assert
            Assert.Throws<KeyNotFoundException>(() => accessor.GetPropertyInfo("NonExistent"));
        }

        #endregion

        #region GetPropertyNames Tests

        [Fact]
        public void GetPropertyNames_ReturnsAllPropertyNames()
        {
            // Arrange
            var accessor = new PropertyAccessor<TestPerson>();
            accessor.CompileAccessors();

            // Act
            var propertyNames = accessor.GetPropertyNames();

            // Assert
            Assert.Contains("Name", propertyNames);
            Assert.Contains("Age", propertyNames);
            Assert.Contains("Salary", propertyNames);
            Assert.Contains("BirthDate", propertyNames);
            Assert.Contains("IsActive", propertyNames);
            Assert.Contains("Height", propertyNames);
            Assert.Contains("ReadOnlyProperty", propertyNames);
        }

        #endregion

        #region HasProperty Tests

        [Fact]
        public void HasProperty_ExistingProperty_ReturnsTrue()
        {
            // Arrange
            var accessor = new PropertyAccessor<TestPerson>();
            accessor.CompileAccessors();

            // Act
            var hasProperty = accessor.HasProperty("Name");

            // Assert
            Assert.True(hasProperty);
        }

        [Fact]
        public void HasProperty_NonExistentProperty_ReturnsFalse()
        {
            // Arrange
            var accessor = new PropertyAccessor<TestPerson>();
            accessor.CompileAccessors();

            // Act
            var hasProperty = accessor.HasProperty("NonExistent");

            // Assert
            Assert.False(hasProperty);
        }

        #endregion

        #region Multiple Instance Tests

        [Fact]
        public void MultipleAccessors_ForSameType_WorkIndependently()
        {
            // Arrange
            var accessor1 = new PropertyAccessor<TestPerson>();
            var accessor2 = new PropertyAccessor<TestPerson>();
            accessor1.CompileAccessors();
            accessor2.CompileAccessors();

            var person1 = new TestPerson { Name = "Person1" };
            var person2 = new TestPerson { Name = "Person2" };

            // Act
            var value1 = accessor1.GetValue(person1, "Name");
            var value2 = accessor2.GetValue(person2, "Name");

            // Assert
            Assert.Equal("Person1", value1);
            Assert.Equal("Person2", value2);
        }

        #endregion

        #region Performance Verification Tests

        [Fact]
        public void CompiledAccessor_MultipleGets_PerformConsistently()
        {
            // Arrange
            var accessor = new PropertyAccessor<TestPerson>();
            accessor.CompileAccessors();
            var person = new TestPerson { Name = "Test", Age = 30 };

            // Act - Multiple accesses should work consistently
            for (int i = 0; i < 1000; i++)
            {
                var name = accessor.GetValue(person, "Name");
                var age = accessor.GetValue(person, "Age");

                // Assert
                Assert.Equal("Test", name);
                Assert.Equal(30, age);
            }
        }

        [Fact]
        public void CompiledAccessor_MultipleSets_PerformConsistently()
        {
            // Arrange
            var accessor = new PropertyAccessor<TestPerson>();
            accessor.CompileAccessors();
            var person = new TestPerson();

            // Act - Multiple sets should work consistently
            for (int i = 0; i < 1000; i++)
            {
                accessor.SetValue(person, "Age", i);
                Assert.Equal(i, person.Age);
            }
        }

        #endregion
    }
}
