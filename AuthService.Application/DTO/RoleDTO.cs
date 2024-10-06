using AuthService.Application;
using System.Collections.Generic;

namespace AuthService.Application.DTO
{
    public class RoleDTO
    {
        public int Id { get; set; }
        public string RoleName { get; set; } = string.Empty; 
        public List<PermissionDTO> Permissions { get; set; } = new List<PermissionDTO>(); // Cambiado a lista de PermissionDTOs
    }
}
