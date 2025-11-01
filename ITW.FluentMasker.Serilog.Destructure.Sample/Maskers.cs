using ITW.FluentMasker;
using ITW.FluentMasker.Extensions;
using ITW.FluentMasker.MaskRules;

namespace ITW.FluentMasker.Serilog.Destructure.Sample
{
    /// <summary>
    /// Masker for Person objects demonstrating various FluentMasker capabilities.
    /// </summary>
    public class PersonMasker : AbstractMasker<Person>
    {
        public PersonMasker()
        {
            Initialize();
        }

        private void Initialize()
        {
            // Remove properties without explicit masking rules from output
            SetPropertyRuleBehavior(PropertyRuleBehavior.Remove);

            // Mask first name - keep first 2 characters
            MaskFor(x => x.FirstName, m => m.MaskStart(2));

            // Mask last name - keep first and last characters
            MaskFor(x => x.LastName, m => m.KeepFirst(1).KeepLast(1));

            // Email masking - mask local part but keep domain
            MaskFor(x => x.Email, (IMaskRule)new EmailMaskRule(localKeep: 2, domainStrategy: EmailDomainStrategy.KeepFull));

            // Phone masking - preserve separators and show last 4 digits
            MaskFor(x => x.Phone, (IMaskRule)new PhoneMaskRule(keepLast: 4, preserveSeparators: true));

            // SSN - completely redact
            MaskFor(x => x.SSN, (IMaskRule)new RedactRule("[REDACTED]"));

            // Address - keep first 4 and last 3 characters
            MaskFor(x => x.Address, m => m.KeepFirst(4).KeepLast(3));

            // BirthDate - shift randomly within ±30 days for anonymity
            MaskFor(x => x.BirthDate, new DateShiftRule(daysRange: 30));

            // Age - add noise ±2 years
            MaskFor(x => x.Age, new NoiseAdditiveRule<int>(maxAbs: 2.0));
        }
    }

    /// <summary>
    /// Masker for CreditCard objects with strict security rules.
    /// </summary>
    public class CreditCardMasker : AbstractMasker<CreditCard>
    {
        public CreditCardMasker()
        {
            Initialize();
        }

        private void Initialize()
        {
            // Remove properties without explicit masking rules from output
            SetPropertyRuleBehavior(PropertyRuleBehavior.Remove);

            // Card number - show first 4 and last 4 digits (standard PCI-DSS masking)
            MaskFor(x => x.CardNumber, (IMaskRule)new CardMaskRule());

            // CVV - completely redact (should never be logged)
            MaskFor(x => x.CVV, (IMaskRule)new RedactRule("***"));

            // Cardholder name - mask middle portion
            MaskFor(x => x.CardHolderName, m => m.KeepFirst(3).KeepLast(3));

            // Expiry date - round to day (to preserve month/year)
            MaskFor(x => x.ExpiryDate, new TimeBucketRule(TimeBucketRule.Granularity.Day));
        }
    }

    /// <summary>
    /// Masker for HealthRecord objects demonstrating HIPAA-compliant masking.
    /// </summary>
    public class HealthRecordMasker : AbstractMasker<HealthRecord>
    {
        public HealthRecordMasker()
        {
            Initialize();
        }

        private void Initialize()
        {
            // Remove properties without explicit masking rules from output
            SetPropertyRuleBehavior(PropertyRuleBehavior.Remove);

            // Patient ID - hash for consistent anonymization
            MaskFor(x => x.PatientId, (IMaskRule)new HashRule(HashAlgorithmType.SHA256));

            // Diagnosis - keep first 10 characters, truncate rest
            MaskFor(x => x.Diagnosis, (IMaskRule)new TruncateRule(maxLength: 10, suffix: "..."));

            // Medication - mask 50% from middle
            MaskFor(x => x.Medication, (IMaskRule)new MaskPercentageRule(0.5, MaskFrom.Middle));

            // Last visit - round to week buckets for privacy
            MaskFor(x => x.LastVisit, new TimeBucketRule(TimeBucketRule.Granularity.Week));

            // Billing amount - round to nearest $100
            MaskFor(x => x.BillingAmount, new RoundToRule<decimal>(100m));
        }
    }
}
