using AuthService.Application;
using System.Collections.Generic;

namespace AuthService.Application.DTO
{
    public class RoleDTO
    {
        public int Id { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public List<int> PermissionIds { get; set; } = new List<int>(); // Solo los IDs de los permisos
    }

}
