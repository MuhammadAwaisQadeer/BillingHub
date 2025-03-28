using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Client_Invoice_System.Models
{
    public class ClientProfileCrossTable : ISoftDeletable
    {
        [Key]
        public int Id { get; set; }

        // Foreign key for Client
        [ForeignKey("Client")]
        public int ClientId { get; set; }

        // Foreign key for Employee (Instead of ClientProfile)
        [ForeignKey("Employee")]
        public int EmployeeId { get; set; }

        // Navigation Properties
        public virtual Client Client { get; set; }
        public virtual Employee Employee { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}
