using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Client_Invoice_System.Models
{
    public enum Designation
    {
        Developer,
        Designer,
        ProjectManager,
        QAEngineer,
        HR,
        Admin
    }

    public class Employee
    {
        [Key]
        public int EmployeeId { get; set; }

        [Required]
        public string EmployeeName { get; set; }

        [Required]
        public Designation Designation { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal HourlyRate { get; set; }

        // ✅ Credit Details
        [Column(TypeName = "decimal(18,2)")]
        public decimal CreditLimit { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CreditUsed { get; set; }

        public DateTime CreditExpiryDate { get; set; }

        // ✅ Track Credit Creation & Updates
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Set default on creation

        public DateTime? UpdatedAt { get; set; } // Nullable, only updates when modified

        // Navigation Properties
        public virtual ICollection<Resource> Resources { get; set; }
    }
}
