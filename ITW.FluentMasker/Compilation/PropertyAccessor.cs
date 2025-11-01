using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace ITW.FluentMasker.Compilation
{
    /// <summary>
    /// Provides high-performance property access through compiled expression trees.
    /// Replaces reflection-based property access with compiled delegates for 10x+ performance improvement.
    /// </summary>
    /// <typeparam name="T">The type to access properties from</typeparam>
    public class PropertyAccessor<T>
    {
        private readonly Dictionary<string, Func<T, object>> _getters = new Dictionary<string, Func<T, object>>();
        private readonly Dictionary<string, Action<T, object>> _setters = new Dictionary<string, Action<T, object>>();
        private readonly Dictionary<string, PropertyInfo> _properties = new Dictionary<string, PropertyInfo>();

        /// <summary>
        /// Compiles expression trees for all properties of type T.
        /// This method should be called once during initialization (e.g., in constructor).
        /// </summary>
        public void CompileAccessors()
        {
            foreach (var property in typeof(T).GetProperties())
            {
                // Store property info for later use
                _properties[property.Name] = property;

                // Compile getter: (obj) => obj.PropertyName
                var param = Expression.Parameter(typeof(T), "obj");
                var propertyAccess = Expression.Property(param, property);
                var convert = Expression.Convert(propertyAccess, typeof(object));
                _getters[property.Name] = Expression.Lambda<Func<T, object>>(convert, param).Compile();

                // Compile setter: (obj, value) => obj.PropertyName = (TProperty)value
                // Only compile setter if property is writable
                if (property.CanWrite)
                {
                    var valueParam = Expression.Parameter(typeof(object), "value");
                    var convertedValue = Expression.Convert(valueParam, property.PropertyType);
                    var assignment = Expression.Assign(propertyAccess, convertedValue);
                    _setters[property.Name] = Expression.Lambda<Action<T, object>>(assignment, param, valueParam).Compile();
                }
            }
        }

        /// <summary>
        /// Gets the value of a property using compiled expression tree (fast).
        /// </summary>
        /// <param name="obj">The object instance</param>
        /// <param name="propertyName">The property name</param>
        /// <returns>The property value</returns>
        /// <exception cref="KeyNotFoundException">Thrown if property doesn't exist</exception>
        public object GetValue(T obj, string propertyName)
        {
            if (!_getters.TryGetValue(propertyName, out var getter))
            {
                throw new KeyNotFoundException($"Property '{propertyName}' not found on type '{typeof(T).Name}'");
            }

            return getter(obj);
        }

        /// <summary>
        /// Sets the value of a property using compiled expression tree (fast).
        /// </summary>
        /// <param name="obj">The object instance</param>
        /// <param name="propertyName">The property name</param>
        /// <param name="value">The value to set</param>
        /// <exception cref="KeyNotFoundException">Thrown if property doesn't exist</exception>
        /// <exception cref="InvalidOperationException">Thrown if property is read-only</exception>
        public void SetValue(T obj, string propertyName, object value)
        {
            if (!_setters.TryGetValue(propertyName, out var setter))
            {
                if (_properties.ContainsKey(propertyName))
                {
                    throw new InvalidOperationException($"Property '{propertyName}' on type '{typeof(T).Name}' is read-only");
                }
                throw new KeyNotFoundException($"Property '{propertyName}' not found on type '{typeof(T).Name}'");
            }

            setter(obj, value);
        }

        /// <summary>
        /// Gets the PropertyInfo for a property.
        /// </summary>
        /// <param name="propertyName">The property name</param>
        /// <returns>The PropertyInfo</returns>
        /// <exception cref="KeyNotFoundException">Thrown if property doesn't exist</exception>
        public PropertyInfo GetPropertyInfo(string propertyName)
        {
            if (!_properties.TryGetValue(propertyName, out var propertyInfo))
            {
                throw new KeyNotFoundException($"Property '{propertyName}' not found on type '{typeof(T).Name}'");
            }

            return propertyInfo;
        }

        /// <summary>
        /// Gets all property names.
        /// </summary>
        public IEnumerable<string> GetPropertyNames()
        {
            return _properties.Keys;
        }

        /// <summary>
        /// Checks if a property exists.
        /// </summary>
        public bool HasProperty(string propertyName)
        {
            return _properties.ContainsKey(propertyName);
        }
    }
}
