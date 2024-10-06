namespace AuthService.Application.DTO
{
    public class CreateUpdateUserDTO
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public List<int> RoleIds { get; set; } = new List<int>(); // Lista de IDs de roles
    }
}
