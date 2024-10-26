using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AuthService.Core.Entidades;
using AuthService.Application.DTO;
using AuthService.Infrastructure.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Newtonsoft.Json;
using System.Net.Http;
using FuncionarioService.Application.DTO;

namespace AuthService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AuthDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory; // Inyectar IHttpClientFactory

        public AuthController(AuthDbContext context, IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory; // Asignar el HttpClientFactory
        }

        // POST: api/Auth/Register
        [HttpPost("Register")]
        public async Task<ActionResult<UserResponseDTO>> Register([FromBody] RegisterUserDTO RegisterUserDTO)
        {
            // Validar IdEmpleado si se proporciona
            if (RegisterUserDTO.IdEmpleado.HasValue)
            {
                var isValidEmpleado = await VerifyIdEmpleado(RegisterUserDTO.IdEmpleado.Value);
                if (!isValidEmpleado)
                {
                    return BadRequest("IdEmpleado no es válido o el empleado no está activo.");
                }
            }

            // Hash de la contraseña
            var hashedPassword = HashPassword(RegisterUserDTO.PasswordHash);

            // Obtener roles
            var roles = await _context.Roles
                .Where(r => RegisterUserDTO.RoleIds.Contains(r.Id))
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .ToListAsync();

            var user = new User
            {
                Username = RegisterUserDTO.Username,
                Email = RegisterUserDTO.Email,
                PasswordHash = hashedPassword,
                Roles = roles,
                IsActive = RegisterUserDTO.IsActive,
                IdEmpleado = RegisterUserDTO.IdEmpleado  // Asignar IdEmpleado si está presente
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var userResponse = new UserResponseDTO
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                IsActive = user.IsActive
            };

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, userResponse);
        }

        // POST: api/Auth/Login
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO loginDTO)
        {
            var user = await _context.Users
                .Include(u => u.Roles)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.Username == loginDTO.Username);

            if (user == null || !VerifyPasswordHash(loginDTO.Password, user.PasswordHash))
            {
                return Unauthorized("Invalid username or password.");
            }

            var token = GenerateJwtToken(user);
            return Ok(new { token });
        }

        private string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("IdEmpleado", user.IdEmpleado?.ToString() ?? "") 
            };

            // Add roles and permissions as claims
            claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role.RoleName)));
            claims.AddRange(user.Roles
                .SelectMany(role => role.RolePermissions.Select(rp => new Claim("Permission", rp.Permission.PermissionName))));

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // GET: api/Auth/Users
        [HttpGet("Users")]
        public async Task<ActionResult<IEnumerable<UserDTO>>> GetUsers()
        {
            var users = await _context.Users
                .Include(u => u.Roles)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .ToListAsync();

            var userDTOs = users.Select(user => new UserDTO
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Roles = user.Roles.Select(role => new RoleDTO
                {
                    Id = role.Id,
                    RoleName = role.RoleName,
                    Permissions = role.RolePermissions.Select(rp => new PermissionDTO
                    {
                        Id = rp.Permission.Id,
                        PermissionName = rp.Permission.PermissionName,
                        Description = rp.Permission.Description // Mapea también la descripción
                    }).ToList()
                }).ToList()
            }).ToList();

            return Ok(userDTOs);
        }

        // GET: api/Auth/Users/5
        [HttpGet("Users/{id}")]
        public async Task<ActionResult<UserDTO>> GetUser(int id)
        {
            var user = await _context.Users
                .Include(u => u.Roles)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            var userDTO = new UserDTO
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Roles = user.Roles.Select(role => new RoleDTO
                {
                    Id = role.Id,
                    RoleName = role.RoleName,
                    Permissions = role.RolePermissions.Select(rp => new PermissionDTO
                    {
                        Id = rp.Permission.Id,
                        PermissionName = rp.Permission.PermissionName,
                        Description = rp.Permission.Description
                    }).ToList()
                }).ToList()
            };

            return Ok(userDTO);
        }

        // PUT: api/Auth/Users/5
        [HttpPut("Users/{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] CreateUpdateUserDTO updateUserDTO)
        {
            var user = await _context.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Actualizar campos del usuario
            user.Username = updateUserDTO.Username;
            user.Email = updateUserDTO.Email;

            // Actualizar los roles del usuario
            var roles = await _context.Roles
                .Where(r => updateUserDTO.RoleIds.Contains(r.Id))
                .ToListAsync();

            user.Roles = roles;

            // Guardar los cambios en la base de datos
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Método para verificar si el IdEmpleado es válido en FuncionarioService
        private async Task<bool> VerifyIdEmpleado(int idEmpleado)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var serviceUrl = _configuration["FuncionarioServiceUrl"]; // Obtener la URL base desde configuración
                var response = await client.GetAsync($"{serviceUrl}/{idEmpleado}");

                if (response.IsSuccessStatusCode)
                {
                    var empleadoData = await response.Content.ReadAsStringAsync();
                    var empleado = JsonConvert.DeserializeObject<EmpleadoDTO>(empleadoData);

                    // Verificación adicional del estado del empleado
                    return empleado != null && empleado.EmpleadoActivo;
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error de conexión con FuncionarioService: {ex.Message}");
            }

            return false;
        }

        private bool VerifyPasswordHash(string password, string storedHash)
        {
            using (var sha256 = SHA256.Create())
            {
                var computedHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(computedHash) == storedHash;
            }
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
    }
}
