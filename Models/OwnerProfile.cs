using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Client_Invoice_System.Models
{
    public class OwnerProfile
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string OwnerName { get; set; }

        [Required, EmailAddress]
        public string BillingEmail { get; set; }

        // Foreign Key for Country and Currency
        [Required]
        public int CountryCurrencyId { get; set; }

        [ForeignKey("CountryCurrencyId")]
        public virtual CountryCurrency CountryCurrency { get; set; }

        // Optional: Override currency if different from country default
        public string? CustomCurrency { get; set; } 

        public string PhoneNumber { get; set; }
        public string BillingAddress { get; set; }
        public string BankName { get; set; }
        public string Swiftcode { get; set; }
        public string BranchAddress { get; set; }
        public string BeneficeryAddress { get; set; }

        [Required]
        public string IBANNumber { get; set; }

        public string AccountTitle { get; set; }
        public string AccountNumber { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Set default on creation

        public DateTime? UpdatedAt { get; set; } // Nullable, only updates when modified
    }
}
