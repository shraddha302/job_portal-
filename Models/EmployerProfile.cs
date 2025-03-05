using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace JobPortal.Models
{
    public class EmployerProfile
    {
        [Key]
        public int Id { get; set; }

        public string CompanyName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ContactInfo { get; set; }
        public string? Logo { get; set; }

        // Foreign key to User
        public int UserId { get; set; }
        public User? User { get; set; }

        // Navigation property for jobs posted by the employer
        public List<Job> Jobs { get; set; } = new List<Job>();
    }
}
