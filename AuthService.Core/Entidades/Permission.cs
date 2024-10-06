using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AuthService.Core.Entidades
{
    public class Permission
    {
        [Key]
        public int Id { get; set; }

        [Required]

        public string PermissionName { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;  // Descripción del permiso

        // Relación muchos a muchos con roles
        public ICollection<Role> Roles { get; set; } = new List<Role>();
    }
}
