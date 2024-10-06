namespace AuthService.Application.DTO
{
    public class AssignRoleDTO
    {
        public int UserId { get; set; }
        public List<int> RoleIds { get; set; } = new List<int>(); // Lista de IDs de roles que se van a asignar
    }
}
