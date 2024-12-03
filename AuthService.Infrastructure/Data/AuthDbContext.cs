using AuthService.Core.Entidades;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

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
        public DbSet<RolePermission> RolePermissions { get; set; } // Tabla intermedia
        public DbSet<Logs> Logs { get; set; } // Tabla para Logs

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración de Logs
            modelBuilder.Entity<Logs>(entity =>
            {
                entity.Property(e => e.EventName)
                      .IsRequired()
                      .HasMaxLength(255);

                entity.Property(e => e.EventDetails)
                      .HasDefaultValue(string.Empty);

                entity.Property(e => e.Username)
                      .IsRequired()
                      .HasMaxLength(255);

                entity.Property(e => e.UserRole)
                      .HasMaxLength(255);

                entity.Property(e => e.Timestamp)
                      .IsRequired();
            });

            // Configuración de Relaciones Muchos a Muchos entre Usuario y Rol con la tabla intermedia sin entidad
            modelBuilder.Entity<User>()
                .HasMany(u => u.Roles)
                .WithMany(r => r.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "UserRole",
                    ur => ur.HasOne<Role>().WithMany().HasForeignKey("RoleId"),
                    ur => ur.HasOne<User>().WithMany().HasForeignKey("UserId")
                );

            // Configuración de la tabla intermedia RolePermission
            modelBuilder.Entity<RolePermission>()
                .HasKey(rp => new { rp.RoleId, rp.PermissionId });  // Llave compuesta

            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId);

            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId);

            // Configuración de Propiedades de Usuario, Rol y Permiso
            modelBuilder.Entity<User>()
                .Property(u => u.Username)
                .IsRequired()
                .HasMaxLength(100);

            modelBuilder.Entity<User>()
                .Property(u => u.IdEmpleado)
                .IsRequired(false); // Permitir valores nulos si IdEmpleado es opcional

            modelBuilder.Entity<Role>()
                .Property(r => r.RoleName)
                .IsRequired()
                .HasMaxLength(50);

            modelBuilder.Entity<Permission>()
                .Property(p => p.PermissionName)
                .IsRequired()
                .HasMaxLength(50);

            modelBuilder.Entity<Permission>()
                .Property(p => p.Description)
                .IsRequired()
                .HasMaxLength(200);

            // Seed de Datos Iniciales para Permisos
            modelBuilder.Entity<Permission>().HasData(
                new Permission { Id = 1, PermissionName = "CanCreateUsers", Description = "Permiso para crear nuevos usuarios" },
                new Permission { Id = 2, PermissionName = "CanEditUsers", Description = "Permiso para editar información de usuarios" },
                new Permission { Id = 3, PermissionName = "CanDeleteUsers", Description = "Permiso para eliminar cuentas de usuarios" },
                new Permission { Id = 4, PermissionName = "CanCreatePayroll", Description = "Permiso para crear nóminas" },
                new Permission { Id = 5, PermissionName = "CanEditPayroll", Description = "Permiso para editar nóminas" },
                new Permission { Id = 6, PermissionName = "CanViewReports", Description = "Permiso para ver reportes" },
                new Permission { Id = 7, PermissionName = "CanCreateReports", Description = "Permiso para crear reportes" },
                new Permission { Id = 8, PermissionName = "CanCreateRequests", Description = "Permiso para crear nuevas solicitudes" },
                new Permission { Id = 9, PermissionName = "CanApproveRequests", Description = "Permiso para aprobar solicitudes" },
                new Permission { Id = 10, PermissionName = "CanRejectRequests", Description = "Permiso para desaprobar solicitudes" },
                new Permission { Id = 11, PermissionName = "CanViewOwnRequests", Description = "Permiso para ver sus propias solicitudes" },
                new Permission { Id = 12, PermissionName = "CanEditOwnRequests", Description = "Permiso para editar sus propias solicitudes antes de aprobación" },
                new Permission { Id = 13, PermissionName = "CanManageRoles", Description = "Permiso para crear, editar y eliminar roles" },
                new Permission { Id = 14, PermissionName = "CanAssignRoles", Description = "Permiso para asignar roles a usuarios" },
                new Permission { Id = 15, PermissionName = "CanManageSystemSettings", Description = "Permiso para gestionar configuraciones del sistema" }
            );

            // Seed de Datos Iniciales para Roles
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, RoleName = "Admin" },
                new Role { Id = 2, RoleName = "RRHH" },
                new Role { Id = 3, RoleName = "Jefatura" },
                new Role { Id = 4, RoleName = "Usuario" }
            );

            // Seed de Datos Iniciales para RolePermissions (Asignación de permisos a roles)
            modelBuilder.Entity<RolePermission>().HasData(
                new RolePermission { RoleId = 1, PermissionId = 1 },
                new RolePermission { RoleId = 1, PermissionId = 2 },
                new RolePermission { RoleId = 1, PermissionId = 3 },
                new RolePermission { RoleId = 1, PermissionId = 4 },
                new RolePermission { RoleId = 1, PermissionId = 5 },
                new RolePermission { RoleId = 1, PermissionId = 6 },
                new RolePermission { RoleId = 1, PermissionId = 7 },
                new RolePermission { RoleId = 1, PermissionId = 8 },
                new RolePermission { RoleId = 1, PermissionId = 9 },
                new RolePermission { RoleId = 1, PermissionId = 10 },
                new RolePermission { RoleId = 1, PermissionId = 11 },
                new RolePermission { RoleId = 1, PermissionId = 12 },
                new RolePermission { RoleId = 1, PermissionId = 13 },
                new RolePermission { RoleId = 1, PermissionId = 14 },
                new RolePermission { RoleId = 1, PermissionId = 15 },

                // RRHH permisos
                new RolePermission { RoleId = 2, PermissionId = 1 },
                new RolePermission { RoleId = 2, PermissionId = 2 },
                new RolePermission { RoleId = 2, PermissionId = 3 },
                new RolePermission { RoleId = 2, PermissionId = 4 },
                new RolePermission { RoleId = 2, PermissionId = 5 },
                new RolePermission { RoleId = 2, PermissionId = 6 },
                new RolePermission { RoleId = 2, PermissionId = 7 },

                // Jefatura permisos
                new RolePermission { RoleId = 3, PermissionId = 8 },
                new RolePermission { RoleId = 3, PermissionId = 9 },
                new RolePermission { RoleId = 3, PermissionId = 10 },
                new RolePermission { RoleId = 3, PermissionId = 11 },
                new RolePermission { RoleId = 3, PermissionId = 12 },

                // Usuario permisos
                new RolePermission { RoleId = 4, PermissionId = 8 },
                new RolePermission { RoleId = 4, PermissionId = 11 },
                new RolePermission { RoleId = 4, PermissionId = 12 }
            );

            // Hash de la contraseña "admin" para el usuario admin
            string hashedPassword = HashPassword("admin");

            // Seed de Datos Iniciales para el Usuario 'admin'
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Username = "admin",
                    Email = "admin@sekcostarica.com",
                    PasswordHash = hashedPassword,
                    IsActive = true,
                    IdEmpleado = null // Sin empleado asignado
                }
            );

            // Asignación del Rol 'Admin' al Usuario 'admin' en la relación de unión
            modelBuilder.Entity("UserRole").HasData(new { UserId = 1, RoleId = 1 });
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
    }
}
