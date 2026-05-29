using HazeClue.Core.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace HazeClue.Infrastructure.DbContext
{
    public class ApplicationDbContext : IdentityDbContext<AppUser,IdentityRole,string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options){ }

        public DbSet<Device> Devices { get; set; }
        public DbSet<FocusSession> Sessions { get; set; }
        public DbSet<AppNotification> Notifications { get; set; }
        public DbSet<PuzzleResult> PuzzleResults { get; set; }
        public DbSet<HealthAssessment> HealthAssessments { get; set; }
        public DbSet<ConsentRecord> ConsentRecords { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }
        public DbSet<SecurityLog> SecurityLogs { get; set; }
        public DbSet<NotificationSetting> NotificationSettings { get; set; }
        public DbSet<DeviceSetting> DeviceSettings { get; set; }
        public DbSet<SmartwatchData> SmartwatchData { get; set; }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Properties<decimal>().HavePrecision(9, 2);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            #region rename identity Table (AspNetUsers)

            modelBuilder.Entity<AppUser>().ToTable("Users", "Security");
            modelBuilder.Entity<IdentityRole>().ToTable("Roles", "Security");
            modelBuilder.Entity<IdentityUserRole<string>>().ToTable("UserRoles", "Security");
            modelBuilder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims", "Security");
            modelBuilder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins", "Security");
            modelBuilder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims", "Security");
            modelBuilder.Entity<IdentityUserToken<string>>().ToTable("UserTokens", "Security");
            #endregion

            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}
