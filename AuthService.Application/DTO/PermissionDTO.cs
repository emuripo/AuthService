using System.ComponentModel.DataAnnotations;

namespace AuthService.Application.DTO
{
    public class PermissionDTO
    {
        [Key]
        public int Id { get; set; }
        public string PermissionName { get; set; } = string.Empty; // Corrección del nombre de la propiedad
        public string Description { get; set; } = string.Empty;
    }
}
