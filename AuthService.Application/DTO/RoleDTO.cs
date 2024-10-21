using System.Collections.Generic;

namespace AuthService.Application.DTO
{
    public class RoleDTO
    {
        public int Id { get; set; }
        public string RoleName { get; set; } = string.Empty;

        // Agregar la propiedad Permissions
        public List<PermissionDTO> Permissions { get; set; } = new List<PermissionDTO>();
    }
}
