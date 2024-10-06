namespace AuthService.Application.DTO
{
    public class CreateUpdateRoleDTO
    {
        public string Name { get; set; } = string.Empty;
        public List<int> PermissionIds { get; set; } = new List<int>(); // Lista de IDs de permisos
    }
}
