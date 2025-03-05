using System.ComponentModel.DataAnnotations;

namespace JobPortal.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        // Add these properties
        public string? CompanyName { get; set; }
        public string? Description { get; set; }  // Added Description
        public string? ContactInfo { get; set; }
        public IFormFile? Logo { get; set; }
    }
}