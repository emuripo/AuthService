namespace AuthService.Application.DTO
{
    public class AssignPermissionDTO
    {
        public int RoleId { get; set; }
        public List<int> PermissionIds { get; set; } = new List<int>(); // Lista de IDs de permisos que se van a asignar
    }
}
