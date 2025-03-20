using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Client_Invoice_System.Models
{
    public class Client
    {
        

        [Key]
        public int ClientId { get; set; }
        [Required]
        public string Name { get; set; }
        [Required, EmailAddress]
        public string Email { get; set; }
        [Required]
        public string Address { get; set; }
        [Required]
        public string PhoneNumber { get; set; }
        [Required]
        public int CountryCurrencyId { get; set; }

        [ForeignKey("CountryCurrencyId")]
        public virtual CountryCurrency CountryCurrency { get; set; }

        public string? CustomCurrency { get; set; }
        [Required]
        public DateTime DueDate { get; set; } = DateTime.Now;
        [Required]
        public string ClientIdentifier { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Set default on creation

        public DateTime? UpdatedAt { get; set; }
        // Navigation properties...
        public virtual ActiveClient ActiveClient { get; set; }
        public virtual ICollection<Resource> Resources { get; set; }
        public virtual ICollection<ClientProfileCrossTable> ClientProfileCrosses { get; set; }
    }
}