namespace ITW.FluentMasker.Serilog.Destructure.Sample
{
    /// <summary>
    /// Sample model representing a person with PII data.
    /// </summary>
    public class Person
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string SSN { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DateTime BirthDate { get; set; }
        public int Age { get; set; }
    }

    /// <summary>
    /// Sample model representing credit card information.
    /// </summary>
    public class CreditCard
    {
        public string CardNumber { get; set; } = string.Empty;
        public string CVV { get; set; } = string.Empty;
        public string CardHolderName { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
    }

    /// <summary>
    /// Sample model representing sensitive health information.
    /// </summary>
    public class HealthRecord
    {
        public string PatientId { get; set; } = string.Empty;
        public string Diagnosis { get; set; } = string.Empty;
        public string Medication { get; set; } = string.Empty;
        public DateTime LastVisit { get; set; }
        public decimal BillingAmount { get; set; }
    }
}
