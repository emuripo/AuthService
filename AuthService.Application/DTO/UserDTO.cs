using System.Collections.Generic;

namespace AuthService.Application.DTO
{
    public class UserDTO
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public List<RoleDTO> Roles { get; set; } = new List<RoleDTO>(); // Cambiado a lista de RoleDTOs
    }
}
