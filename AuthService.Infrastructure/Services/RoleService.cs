// src/AuthService.Infrastructure/Services/RoleService.cs
using AuthService.Core.Entidades;
using AuthService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthService.Infrastructure.Services
{
    public class RoleService
    {
        private readonly AuthDbContext _context;

        public RoleService(AuthDbContext context)
        {
            _context = context;
        }

        // Asignar un permiso a un rol
        public async Task AssignPermissionToRoleAsync(int roleId, int permissionId)
        {
            var rolePermission = await _context.RolePermissions
                .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

            if (rolePermission == null)
            {
                _context.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permissionId });
                await _context.SaveChangesAsync();
            }
        }

        // Revocar un permiso de un rol
        public async Task RevokePermissionFromRoleAsync(int roleId, int permissionId)
        {
            var rolePermission = await _context.RolePermissions
                .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

            if (rolePermission != null)
            {
                _context.RolePermissions.Remove(rolePermission);
                await _context.SaveChangesAsync();
            }
        }

        // Obtener todos los permisos de un rol
        public async Task<List<Permission>> GetPermissionsByRoleAsync(int roleId)
        {
            var role = await _context.Roles
                .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(r => r.Id == roleId);

            return role?.RolePermissions.Select(rp => rp.Permission).ToList();
        }

        // Crear un nuevo rol
        public async Task<Role> CreateRoleAsync(string roleName)
        {
            if (await _context.Roles.AnyAsync(r => r.RoleName == roleName))
                throw new InvalidOperationException("El rol ya existe.");

            var role = new Role { RoleName = roleName };
            _context.Roles.Add(role);
            await _context.SaveChangesAsync();
            return role;
        }

        // Eliminar un rol
        public async Task DeleteRoleAsync(int roleId)
        {
            var role = await _context.Roles.FindAsync(roleId);
            if (role == null)
                throw new KeyNotFoundException("Rol no encontrado.");

            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();
        }
    }
}
