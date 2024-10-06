using AuthService.Core.Entidades;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AuthService.Core.Entidades
{
    public class Role
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string RoleName { get; set; } = string.Empty;  // Nombre del rol

        // Relación muchos a muchos con User (esto es lo que faltaba)
        public ICollection<User> Users { get; set; } = new List<User>();

        // Relación muchos a muchos con permisos
        public ICollection<Permission> Permissions { get; set; } = new List<Permission>();
    }
}
