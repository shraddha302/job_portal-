using System;
using System.ComponentModel.DataAnnotations;

namespace JobPortal.Models
{
    public class Application
    {
        [Key]
        public int Id { get; set; }

        // Foreign key to Job
        public int JobId { get; set; }
        public virtual Job Job { get; set; } = null!;

        // Foreign key to User
        public int UserId { get; set; }
        public virtual User User { get; set; } = null!;

        public string Status { get; set; } = "Pending";
        public DateTime AppliedDate { get; set; } = DateTime.UtcNow;
        public string? CVFileName { get; set; }
    }
}
