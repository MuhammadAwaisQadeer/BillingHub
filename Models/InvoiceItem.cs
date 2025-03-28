using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Client_Invoice_System.Models
{
    public class InvoiceItem : ISoftDeletable
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Invoice")]
        public int InvoiceId { get; set; }
        public virtual Invoice Invoice { get; set; }

        [ForeignKey("Resource")]
        public int ResourceId { get; set; }
        public virtual Resource Resource { get; set; }

        public int ConsumedHours { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal RatePerHour { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount => ConsumedHours * RatePerHour;
        public bool IsDeleted { get; set; } = false;
    }
}
