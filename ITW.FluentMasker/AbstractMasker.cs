using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ITW.FluentMasker.TypeConverters;
using ITW.FluentMasker.Compilation;
using ITW.FluentMasker.Builders;

namespace ITW.FluentMasker
{
    public abstract class AbstractMasker<T>
    {
        // Changed from List<IMaskRule> to List<object> to support both IMaskRule and IMaskRule<TProperty, TProperty>
        private readonly Dictionary<string, List<object>> rules = new Dictionary<string, List<object>>();

        private readonly TypeConverterRegistry _converters = TypeConverterRegistry.Instance;

        // High-performance property accessor using compiled expression trees
        private readonly PropertyAccessor<T> _accessor = new PropertyAccessor<T>();

        private PropertyRuleBehavior propertyRuleBehavior = PropertyRuleBehavior.Exclude;

        /// <summary>
        /// Constructor that compiles property accessors for high-performance property access.
        /// </summary>
        protected AbstractMasker()
        {
            // Compile expression trees once at construction for 10x+ performance improvement
            _accessor.CompileAccessors();
        }

        public void SetPropertyRuleBehavior(PropertyRuleBehavior behavior)
        {
            propertyRuleBehavior = behavior;
        }

        /// <summary>
        /// Registers a generic mask rule for a property of any type.
        /// Supports type-safe masking with automatic type conversion.
        /// </summary>
        /// <typeparam name="TProperty">The property type</typeparam>
        /// <param name="expression">Expression pointing to the property</param>
        /// <param name="rule">The generic mask rule to apply</param>
        public void MaskFor<TProperty>(Expression<Func<T, TProperty>> expression, IMaskRule<TProperty, TProperty> rule)
        {
            string propertyName = GetPropertyName(expression);
            if (!rules.ContainsKey(propertyName))
            {
                rules[propertyName] = new List<object>();
            }

            rules[propertyName].Add(rule);
        }

        /// <summary>
        /// Registers a mask rule for a string property (backward compatibility).
        /// Existing code using this method continues to work without modification.
        /// </summary>
        /// <param name="expression">Expression pointing to the string property</param>
        /// <param name="rule">The mask rule to apply</param>
        public void MaskFor(Expression<Func<T, string>> expression, IMaskRule rule)
        {
            string propertyName = GetPropertyName(expression);
            if (!rules.ContainsKey(propertyName))
            {
                rules[propertyName] = new List<object>();
            }

            rules[propertyName].Add(rule);
        }

        /// <summary>
        /// Registers mask rules using a fluent builder API for string properties.
        /// Allows chaining multiple masking rules in a readable, declarative style.
        /// </summary>
        /// <param name="expression">Expression pointing to the string property</param>
        /// <param name="configure">Lambda function that configures the builder with chained rules</param>
        /// <example>
        /// <code>
        /// MaskFor(x => x.Email, m => m
        ///     .MaskStart(2)
        ///     .MaskEnd(4)
        ///     .KeepFirst(3));
        /// </code>
        /// </example>
        public void MaskFor(
            Expression<Func<T, string>> expression,
            Func<StringMaskingBuilder, StringMaskingBuilder> configure)
        {
            // Create a fresh builder instance
            var builder = new StringMaskingBuilder();

            // Pass builder to the lambda to allow configuration
            var configured = configure(builder);

            // Build the rules list
            var builtRules = configured.Build();

            // Apply each rule in order by delegating to the existing MaskFor method
            foreach (var rule in builtRules)
            {
                MaskFor(expression, rule);
            }
        }

        /// <summary>
        /// Extracts the property name from an expression.
        /// </summary>
        private string GetPropertyName<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            if (expression.Body is MemberExpression memberExpression)
            {
                return memberExpression.Member.Name;
            }

            throw new ArgumentException("Expression must be a member expression", nameof(expression));
        }

        public MaskingResult Mask(T obj)
        {
            var errors = new List<string>();
            bool isSuccess = true;
            var maskedProperties = new Dictionary<string, object>();

            // Use compiled accessor for property names instead of reflection
            foreach (var propertyName in _accessor.GetPropertyNames())
            {
                try
                {
                    // Get PropertyInfo from accessor (cached, no reflection overhead)
                    PropertyInfo property = _accessor.GetPropertyInfo(propertyName);

                    /*
                    string value = (string)property.GetValue(obj);

                    if (rules.ContainsKey(property.Name))
                    {
                        foreach (var rule in rules[property.Name])
                        {
                            value = rule.Apply(value);
                        }
                        maskedProperties[property.Name] = value;
                    }*/
                    if (rules.ContainsKey(propertyName))
                    {
                        // Use compiled accessor instead of reflection (10x+ faster)
                        object value = _accessor.GetValue(obj, propertyName);

                        foreach (var rule in rules[propertyName])
                        {
                            if (rule is MaskForEachRule<IEnumerable<object>> maskForEachRule)
                            {
                                // Special handling for collection masking (unchanged)
                                var listValue = (IEnumerable<object>)value;
                                var maskedList = new List<object>();

                                var maskMethod = maskForEachRule.Masker.GetType().GetMethod("Mask");
                                if (maskMethod == null)
                                {
                                    throw new Exception($"The Masker type {maskForEachRule.Masker.GetType()} doesn't have a Mask() method");
                                }

                                foreach (var item in listValue)
                                {
                                    var maskedItemResult = (MaskingResult)maskMethod.Invoke(maskForEachRule.Masker, new object[] { item });
                                    if (maskedItemResult.IsSuccess)
                                    {
                                        maskedList.Add(JsonConvert.DeserializeObject(maskedItemResult.MaskedData, item.GetType()));
                                    }
                                    else
                                    {
                                        errors.AddRange(maskedItemResult.Errors);
                                    }
                                }

                                value = maskedList;
                            }
                            else
                            {
                                // NEW: Use type-aware rule application with automatic conversion
                                value = ApplyRuleWithTypeConversion(rule, value, property.PropertyType);
                            }
                        }

                        maskedProperties[propertyName] = value;
                    }
                    else if (propertyRuleBehavior == PropertyRuleBehavior.Include)
                    {
                        // FIXED: No longer casts all properties to string
                        // This now works with mixed types (int, string, DateTime, etc.)
                        // Use compiled accessor instead of reflection (10x+ faster)
                        object value = _accessor.GetValue(obj, propertyName);
                        maskedProperties[propertyName] = value;
                    }
                    else if (propertyRuleBehavior == PropertyRuleBehavior.Exclude)
                    {
                        maskedProperties[propertyName] = null;
                    }
                    // If PropertyRuleBehavior is Remove, we simply don't add the property to maskedProperties
                }
                catch (Exception ex)
                {
                    isSuccess = false;
                    errors.Add($"Error masking property {propertyName}: {ex.Message}");
                }
            }

            var jsonResult = JsonConvert.SerializeObject(maskedProperties);
            return new MaskingResult { IsSuccess = isSuccess, Errors = errors, MaskedData = jsonResult };
        }

        /// <summary>
        /// Applies a mask rule to a value, handling type conversion as needed.
        /// Supports both legacy IMaskRule (string-only) and new IMaskRule<TProperty, TProperty> (generic).
        /// </summary>
        /// <param name="rule">The rule to apply (can be IMaskRule or IMaskRule<T, T>)</param>
        /// <param name="value">The value to mask</param>
        /// <param name="propertyType">The type of the property being masked</param>
        /// <returns>The masked value</returns>
        private object ApplyRuleWithTypeConversion(object rule, object value, Type propertyType)
        {
            if (value == null)
                return null;

            // Check if it's a generic IMaskRule<TProperty, TProperty>
            var genericRuleInterface = rule.GetType().GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType &&
                                i.GetGenericTypeDefinition() == typeof(IMaskRule<,>));

            if (genericRuleInterface != null)
            {
                // Generic rule - call Apply directly
                var applyMethod = genericRuleInterface.GetMethod("Apply");
                if (applyMethod != null)
                {
                    return applyMethod.Invoke(rule, new[] { value });
                }
            }

            // Legacy IMaskRule (string-only) - requires type conversion
            if (rule is IMaskRule stringRule)
            {
                // If value is already a string, apply directly
                if (value is string stringValue)
                {
                    return stringRule.Apply(stringValue);
                }

                // Convert to string, apply rule, then convert back
                var toStringConverter = _converters.Get<object, string>();
                if (toStringConverter != null)
                {
                    // Try to convert to string
                    string converted = ConvertToString(value, propertyType);
                    string masked = stringRule.Apply(converted);

                    // Try to convert back to original type
                    return ConvertFromString(masked, propertyType);
                }

                // If no converter available, just apply to ToString()
                return stringRule.Apply(value.ToString());
            }

            // No rule matched - return original value
            return value;
        }

        /// <summary>
        /// Converts a value to string using registered type converters.
        /// </summary>
        private string ConvertToString(object value, Type sourceType)
        {
            if (value == null)
                return null;

            if (value is string str)
                return str;

            // Try to find a registered converter
            var converterType = typeof(ITypeConverter<,>).MakeGenericType(sourceType, typeof(string));
            var getMethod = _converters.GetType().GetMethod("Get").MakeGenericMethod(sourceType, typeof(string));
            var converter = getMethod.Invoke(_converters, null);

            if (converter != null)
            {
                var convertMethod = converterType.GetMethod("Convert");
                return (string)convertMethod.Invoke(converter, new[] { value });
            }

            // Fallback to ToString()
            return value.ToString();
        }

        /// <summary>
        /// Converts a string back to the original type using registered type converters.
        /// </summary>
        private object ConvertFromString(string value, Type targetType)
        {
            if (value == null)
                return null;

            if (targetType == typeof(string))
                return value;

            // Try to find a registered converter
            var converterType = typeof(ITypeConverter<,>).MakeGenericType(targetType, typeof(string));
            var getMethod = _converters.GetType().GetMethod("Get").MakeGenericMethod(targetType, typeof(string));
            var converter = getMethod.Invoke(_converters, null);

            if (converter != null)
            {
                var convertBackMethod = converterType.GetMethod("ConvertBack");
                return convertBackMethod.Invoke(converter, new[] { value });
            }

            // Fallback to string
            return value;
        }
    }




}
