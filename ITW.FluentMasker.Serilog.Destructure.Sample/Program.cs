using Serilog;
using ITW.FluentMasker.Serilog.Destructure.Sample;

namespace ITW.FluentMasker.Serilog.Destructure.Sample
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== FluentMasker + Serilog Integration Sample ===\n");
            Console.WriteLine("Demonstrating how to use FluentMasker with Serilog's IDestructuringPolicy\n");
            Console.WriteLine("==================================================\n\n");

            // Configure Serilog with the FluentMaskerPolicy
            Log.Logger = new LoggerConfiguration()
                .Destructure.With(new FluentMaskerPolicy())
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss}] {Level:u3} {Message:lj}{NewLine}")
                .CreateLogger();

            try
            {
                RunPersonExample();
                Console.WriteLine("\n" + new string('-', 80) + "\n");

                RunCreditCardExample();
                Console.WriteLine("\n" + new string('-', 80) + "\n");

                RunHealthRecordExample();
                Console.WriteLine("\n" + new string('-', 80) + "\n");

                RunMultipleObjectsExample();
                Console.WriteLine("\n" + new string('-', 80) + "\n");

                RunWithoutDestructuringExample();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }

            Console.WriteLine("\n\n=== Sample Complete ===");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        /// <summary>
        /// Demonstrates masking of Person objects with various PII data.
        /// </summary>
        static void RunPersonExample()
        {
            Console.WriteLine("EXAMPLE 1: Person Object Masking");
            Console.WriteLine("Demonstrating email, phone, SSN, and date masking\n");

            var person = new Person
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                Phone = "+1 (555) 123-4567",
                SSN = "123-45-6789",
                Address = "123 Main Street, Springfield",
                BirthDate = new DateTime(1990, 5, 15),
                Age = 34
            };

            Console.WriteLine("Original data (NOT logged):");
            Console.WriteLine($"  Name: {person.FirstName} {person.LastName}");
            Console.WriteLine($"  Email: {person.Email}");
            Console.WriteLine($"  Phone: {person.Phone}");
            Console.WriteLine($"  SSN: {person.SSN}");
            Console.WriteLine($"  Address: {person.Address}");
            Console.WriteLine($"  Birth Date: {person.BirthDate:yyyy-MM-dd}");
            Console.WriteLine($"  Age: {person.Age}\n");

            Console.WriteLine("Logged with masking (using {@Person}):");
            Log.Information("User registration: {@Person}", person);

            Console.WriteLine("\nMasking rules applied:");
            Console.WriteLine("  - FirstName: MaskStart(2) - keeps first 2 chars");
            Console.WriteLine("  - LastName: KeepFirst(1).KeepLast(1) - shows only first and last char");
            Console.WriteLine("  - Email: EmailMaskRule - masks local part, keeps domain");
            Console.WriteLine("  - Phone: PhoneMaskRule - preserves format, shows last 4 digits");
            Console.WriteLine("  - SSN: RedactRule - completely redacted");
            Console.WriteLine("  - Address: KeepFirst(4).KeepLast(3) - shows partial address");
            Console.WriteLine("  - BirthDate: DateShiftRule - shifted ±30 days");
            Console.WriteLine("  - Age: NoiseAdditiveRule - noise ±2 years");
        }

        /// <summary>
        /// Demonstrates masking of credit card information.
        /// </summary>
        static void RunCreditCardExample()
        {
            Console.WriteLine("EXAMPLE 2: Credit Card Masking");
            Console.WriteLine("Demonstrating PCI-DSS compliant credit card masking\n");

            var creditCard = new CreditCard
            {
                CardNumber = "4532123456789012",
                CVV = "123",
                CardHolderName = "John Michael Doe",
                ExpiryDate = new DateTime(2025, 12, 31)
            };

            Console.WriteLine("Original data (NOT logged):");
            Console.WriteLine($"  Card Number: {creditCard.CardNumber}");
            Console.WriteLine($"  CVV: {creditCard.CVV}");
            Console.WriteLine($"  Cardholder: {creditCard.CardHolderName}");
            Console.WriteLine($"  Expiry: {creditCard.ExpiryDate:MM/yyyy}\n");

            Console.WriteLine("Logged with masking (using {@CreditCard}):");
            Log.Information("Payment processed: {@CreditCard}", creditCard);

            Console.WriteLine("\nMasking rules applied:");
            Console.WriteLine("  - CardNumber: CardMaskRule - shows first 4 and last 4 (PCI-DSS)");
            Console.WriteLine("  - CVV: RedactRule - completely redacted (never log CVV!)");
            Console.WriteLine("  - CardHolderName: KeepFirst(3).KeepLast(3) - partial name");
            Console.WriteLine("  - ExpiryDate: TimeBucketRule - rounded to month");
        }

        /// <summary>
        /// Demonstrates masking of health records (HIPAA compliance).
        /// </summary>
        static void RunHealthRecordExample()
        {
            Console.WriteLine("EXAMPLE 3: Health Record Masking");
            Console.WriteLine("Demonstrating HIPAA-compliant health data masking\n");

            var healthRecord = new HealthRecord
            {
                PatientId = "P-2024-001234",
                Diagnosis = "Type 2 Diabetes Mellitus with complications",
                Medication = "Metformin 500mg",
                LastVisit = new DateTime(2024, 3, 15, 14, 30, 0),
                BillingAmount = 1245.67m
            };

            Console.WriteLine("Original data (NOT logged):");
            Console.WriteLine($"  Patient ID: {healthRecord.PatientId}");
            Console.WriteLine($"  Diagnosis: {healthRecord.Diagnosis}");
            Console.WriteLine($"  Medication: {healthRecord.Medication}");
            Console.WriteLine($"  Last Visit: {healthRecord.LastVisit:yyyy-MM-dd HH:mm}");
            Console.WriteLine($"  Billing: ${healthRecord.BillingAmount:F2}\n");

            Console.WriteLine("Logged with masking (using {@HealthRecord}):");
            Log.Information("Patient record accessed: {@HealthRecord}", healthRecord);

            Console.WriteLine("\nMasking rules applied:");
            Console.WriteLine("  - PatientId: HashRule(SHA256) - one-way hash for anonymization");
            Console.WriteLine("  - Diagnosis: TruncateRule(10) - truncated to 10 chars");
            Console.WriteLine("  - Medication: MaskPercentageRule(50%, Middle) - 50% masked from middle");
            Console.WriteLine("  - LastVisit: TimeBucketRule(7 days) - rounded to week");
            Console.WriteLine("  - BillingAmount: RoundToRule(100) - rounded to nearest $100");
        }

        /// <summary>
        /// Demonstrates logging multiple masked objects in a single log entry.
        /// </summary>
        static void RunMultipleObjectsExample()
        {
            Console.WriteLine("EXAMPLE 4: Multiple Objects in One Log Entry");
            Console.WriteLine("Demonstrating masking of multiple different object types\n");

            var person = new Person
            {
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane.smith@company.com",
                Phone = "+44 20 7123 4567",
                SSN = "987-65-4321",
                Address = "456 Oak Avenue, London",
                BirthDate = new DateTime(1985, 8, 20),
                Age = 39
            };

            var card = new CreditCard
            {
                CardNumber = "5500000000000004",
                CVV = "456",
                CardHolderName = "Jane Smith",
                ExpiryDate = new DateTime(2026, 6, 30)
            };

            Console.WriteLine("Logging multiple objects with different masking rules:");
            Log.Information("Transaction: Person {@Person}, Payment {@CreditCard}", person, card);

            Console.WriteLine("\nBoth objects are masked according to their respective rules!");
        }

        /// <summary>
        /// Demonstrates the difference between destructuring and string interpolation.
        /// </summary>
        static void RunWithoutDestructuringExample()
        {
            Console.WriteLine("EXAMPLE 5: Destructuring vs. String Interpolation");
            Console.WriteLine("Showing why {@Object} syntax is important\n");

            var person = new Person
            {
                FirstName = "Bob",
                LastName = "Johnson",
                Email = "bob@test.com",
                Phone = "555-1234",
                SSN = "111-22-3333",
                Address = "789 Pine Road",
                BirthDate = new DateTime(1995, 3, 10),
                Age = 29
            };

            Console.WriteLine("WITHOUT destructuring (using {Person} - NOT masked!):");
            Log.Warning("User login: {Person}", person);

            Console.WriteLine("\nWITH destructuring (using {@Person} - MASKED!):");
            Log.Information("User login: {@Person}", person);

            Console.WriteLine("\nKEY POINT:");
            Console.WriteLine("  - Use {@Object} to trigger IDestructuringPolicy and enable masking");
            Console.WriteLine("  - Using {Object} will just call ToString() without masking");
        }
    }
}
