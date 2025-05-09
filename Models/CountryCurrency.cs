﻿using System.ComponentModel.DataAnnotations;

namespace Client_Invoice_System.Models
{
    public class CountryCurrency : ISoftDeletable
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string CountryName { get; set; }

        [Required]
        public string CurrencyName { get; set; }

        [Required]
        public string Symbol { get; set; }

        [Required, StringLength(3)]
        public string CurrencyCode { get; set; } 
        public bool IsDeleted { get; set; } = false;
    }
}
