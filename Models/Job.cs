using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace JobPortal.Models
{
    public class Job
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public string Location { get; set; } = string.Empty;

        [Required]
        public string Salary { get; set; } = string.Empty;

        // This property is used for display purposes only
        [Required]
        public string Company { get; set; } = string.Empty;

        // Stores the URL for the uploaded company logo
        public string CompanyLogoUrl { get; set; } = string.Empty;

        // For example: Full-Time, Part-Time, Contract, etc.
        [Required]
        public string Type { get; set; } = string.Empty;

        // Not mapped property for custom job type input (not stored in the database)
        [NotMapped]
        public string? CustomType { get; set; }

        public DateTime PostedDate { get; set; } = DateTime.UtcNow;
        public bool IsApproved { get; set; } = false;

        // Foreign key referencing EmployerProfile.
        // Using nameof(Employer) to indicate the navigation property.
        [ForeignKey(nameof(Employer))]
        public int? EmployerProfileId { get; set; }

        // Navigation properties
        public virtual EmployerProfile? Employer { get; set; }
        public virtual ICollection<Application> Applications { get; set; } = new List<Application>();

        // Not mapped property for file upload; used to handle file input which will update CompanyLogoUrl.
        [NotMapped]
        public IFormFile? CompanyLogo { get; set; }
    }
}
