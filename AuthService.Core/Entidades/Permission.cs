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
        public string Description { get; set; } = string.Empty;  
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
