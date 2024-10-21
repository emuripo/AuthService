using System.ComponentModel.DataAnnotations.Schema;

namespace AuthService.Core.Entidades
{
    public class RolePermission
    {
        public int RoleId { get; set; }
        public Role? Role { get; set; }  // Hacemos opcional la propiedad Role

        public int PermissionId { get; set; }
        public Permission? Permission { get; set; }  // Hacemos opcional la propiedad Permission
    }
}
