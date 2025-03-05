using Microsoft.EntityFrameworkCore;

namespace JobPortal.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Job> Jobs { get; set; }
        public DbSet<Application> Applications { get; set; }
        public DbSet<EmployerProfile> EmployerProfiles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();
                entity.Property(u => u.Role).HasDefaultValue("User");

                // Configure one-to-one relationship with EmployerProfile
                entity.HasOne(u => u.EmployerProfile)
                      .WithOne(e => e.User)
                      .HasForeignKey<EmployerProfile>(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure EmployerProfile relationships
            modelBuilder.Entity<EmployerProfile>(entity =>
            {
                // Configure one-to-many relationship with Jobs
                entity.HasMany(e => e.Jobs)
                      .WithOne(j => j.Employer)
                      .HasForeignKey(j => j.EmployerProfileId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.Logo).HasDefaultValue("/images/default-logo.png");
            });

            // Configure Job relationships
            modelBuilder.Entity<Job>(entity =>
            {
                entity.Property(j => j.PostedDate).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(j => j.IsApproved).HasDefaultValue(false);

                // Configure one-to-many relationship with Applications
                entity.HasMany(j => j.Applications)
                      .WithOne(a => a.Job)
                      .HasForeignKey(a => a.JobId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Application relationships
            modelBuilder.Entity<Application>(entity =>
            {
                entity.Property(a => a.Status).HasDefaultValue("Pending");
                entity.Property(a => a.AppliedDate).HasDefaultValueSql("GETUTCDATE()");

                // Configure relationship with User
                entity.HasOne(a => a.User)
                      .WithMany(u => u.Applications)
                      .HasForeignKey(a => a.UserId)
                      .OnDelete(DeleteBehavior.NoAction);
            });
        }
    }
}
