using AuthService.Core.Entidades;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Data
{
    public class AuthDbContext : DbContext
    {
        public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Roles)
                .WithMany(r => r.Users)
                .UsingEntity(j => j.ToTable("UserRoles"));

            modelBuilder.Entity<Role>()
                .HasMany(r => r.Permissions)
                .WithMany(p => p.Roles)
                .UsingEntity(j => j.ToTable("RolePermissions"));

            modelBuilder.Entity<User>()
                .Property(u => u.Username)
                .IsRequired()
                .HasMaxLength(100);

            modelBuilder.Entity<Role>()
                .Property(r => r.RoleName)
                .IsRequired()
                .HasMaxLength(50);

            modelBuilder.Entity<Permission>()
                .Property(p => p.Description)
                .IsRequired()
                .HasMaxLength(200);

            // Seed de datos iniciales para la tabla de Permisos
            modelBuilder.Entity<Permission>().HasData(
                new Permission { Id = 1, PermissionName = "CanViewReports", Description = "Permission to view reports" },
                new Permission { Id = 2, PermissionName = "CanEditUsers", Description = "Permission to edit users" },
                new Permission { Id = 3, PermissionName = "CanManageRoles", Description = "Permission to manage roles" }
            );
        }
    }
}
