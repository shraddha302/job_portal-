using Microsoft.AspNetCore.Identity;
using System.Linq;

namespace JobPortal.Models
{
    public static class SeedData
    {
        public static void Initialize(AppDbContext context)
        {
            // Ensure the database is created
            context.Database.EnsureCreated();

            // Check if admin already exists
            if (!context.Users.Any(u => u.Role == "Admin"))
            {
                var hasher = new PasswordHasher<User>();

                var admin = new User
                {
                    Username = "admin",
                    Email = "admin@jobportal.com",
                    Role = "Admin"
                };

                // Hash the password
                admin.Password = hasher.HashPassword(admin, "Admin@1234"); // Use a strong password

                context.Users.Add(admin);
                context.SaveChanges();
            }
        }
    }
}
