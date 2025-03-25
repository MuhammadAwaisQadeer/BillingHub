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

        [Required(ErrorMessage = "Name is required.")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string EmployeeName { get; set; }

        [Required]
        public Designation Designation { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal HourlyRate { get; set; }

        // Navigation Property
        public virtual ICollection<Resource> Resources { get; set; }
    }
}