using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AuthenticationModule.Models;

namespace AuthenticationModule.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<UserSession> UserSessions { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure UserSession entity
            builder.Entity<UserSession>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SessionToken).IsRequired().HasMaxLength(500);
                entity.Property(e => e.RefreshToken).IsRequired().HasMaxLength(500);
                entity.Property(e => e.DeviceInfo).HasMaxLength(500);
                entity.Property(e => e.IpAddress).HasMaxLength(45);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.UserSessions)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure ApplicationUser entity
            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.UserType).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.IsActive).IsRequired();
            });
        }
    }
}
