using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Client_Invoice_System.Models
{
    public class Resource
    {
        [Key]
        public int ResourceId { get; set; }

        [ForeignKey("Client")]
        public int ClientId { get; set; }

        [Required]
        public string ResourceName { get; set; }

        [ForeignKey("Employee")]
        public int EmployeeId { get; set; }

        public int ConsumedTotalHours { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsInvoiced { get; set; }  // ✅ Tracks if this resource has been invoiced

        // ✅ New: Link to Invoice
        public int? InvoiceId { get; set; }  // Nullable because resources may exist before an invoice is created
        [ForeignKey("InvoiceId")]
        public virtual Invoice Invoice { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        public virtual Client Client { get; set; }
        public virtual Employee Employee { get; set; }
        public int OwnerProfileId { get; set; }
        [ForeignKey("OwnerProfileId")]
        public virtual OwnerProfile OwnerProfile { get; set; }
    }

}
