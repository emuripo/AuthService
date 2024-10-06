using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AuthService.Core.Entidades
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public string Email { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        // Relación muchos a muchos con roles
        public ICollection<Role> Roles { get; set; } = new List<Role>();
    }
}
