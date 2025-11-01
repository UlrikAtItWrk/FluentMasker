using Serilog.Core;
using Serilog.Events;
using ITW.FluentMasker;
using ITW.FluentMasker.Extensions;
using ITW.FluentMasker.Builders;

namespace ITW.FluentMasker.Serilog.Destructure.Sample
{
    /// <summary>
    /// A custom Serilog IDestructuringPolicy that uses FluentMasker to mask sensitive data before logging.
    /// This policy intercepts objects during destructuring and produces masked values.
    /// </summary>
    /// <remarks>
    /// This implementation supports multiple types and uses different masking strategies for each.
    /// You can extend this to handle additional types or generalize it further.
    /// </remarks>
    public sealed class FluentMaskerPolicy : IDestructuringPolicy
    {
        public bool TryDestructure(
            object value,
            ILogEventPropertyValueFactory propertyValueFactory,
            out LogEventPropertyValue result)
        {
            // Handle Person type
            if (value is Person person)
            {
                var masked = MaskPerson(person);
                result = propertyValueFactory.CreatePropertyValue(masked, destructureObjects: true);
                return true;
            }

            // Handle CreditCard type
            if (value is CreditCard card)
            {
                var masked = MaskCreditCard(card);
                result = propertyValueFactory.CreatePropertyValue(masked, destructureObjects: true);
                return true;
            }

            // Handle HealthRecord type
            if (value is HealthRecord healthRecord)
            {
                var masked = MaskHealthRecord(healthRecord);
                result = propertyValueFactory.CreatePropertyValue(masked, destructureObjects: true);
                return true;
            }

            // Fall through: not handled by this policy
            result = null!;
            return false;
        }

        /// <summary>
        /// Masks a Person object using FluentMasker with StringMaskingBuilder.
        /// </summary>
        private string MaskPerson(Person person)
        {
            var masker = new PersonMasker();
            var result = masker.Mask(person);
            return result.MaskedData;
        }

        /// <summary>
        /// Masks a CreditCard object using FluentMasker.
        /// </summary>
        private string MaskCreditCard(CreditCard card)
        {
            var masker = new CreditCardMasker();
            var result = masker.Mask(card);
            return result.MaskedData;
        }

        /// <summary>
        /// Masks a HealthRecord object using FluentMasker.
        /// </summary>
        private string MaskHealthRecord(HealthRecord record)
        {
            var masker = new HealthRecordMasker();
            var result = masker.Mask(record);
            return result.MaskedData;
        }
    }
}
