using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Client_Invoice_System.Models
{
    public class OwnerProfile : ISoftDeletable
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Owner Name is required")]
        public string OwnerName { get; set; }

        [Required(ErrorMessage = "Billing Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string BillingEmail { get; set; }

        [Required]
        public int CountryCurrencyId { get; set; }

        [ForeignKey("CountryCurrencyId")]
        public virtual CountryCurrency CountryCurrency { get; set; }

        public string? CustomCurrency { get; set; }
        [Required(ErrorMessage = "Phone Number is required")]
        [StringLength(25, ErrorMessage = "Phone number cannot exceed 13 characters.")]
        //[RegularExpression(@"^\+92\d{10}$", ErrorMessage = "Phone number must be in the format +92XXXXXXXXXX")]
        public string PhoneNumber { get; set; }
        [Required(ErrorMessage = "Billing Address is Required")]
        public string BillingAddress { get; set; }
        [Required(ErrorMessage = "Bank Name is Required")]
        public string BankName { get; set; }

        [Required(ErrorMessage = "Swift Code is Required")]
        public string Swiftcode { get; set; }

        [Required(ErrorMessage = "Branch Address is required")]
        public string BranchAddress { get; set; }

        [Required(ErrorMessage = "Beneficiary Address is required")]
        public string BeneficeryAddress { get; set; }
        [Required(ErrorMessage = "IBAN Number is required")]
        public string IBANNumber { get; set; }

        [Required(ErrorMessage = "Account Title is required")]
        public string AccountTitle { get; set; }

        [Required(ErrorMessage = "Account Number is required")]
        public string AccountNumber { get; set; }
        public virtual ICollection<Resource> Resources { get; set; } = new List<Resource>();
        public bool IsDeleted { get; set; } = false;
    }
}