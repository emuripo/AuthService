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
        private readonly IHttpClientFactory _httpClientFactory;

        public AuthController(AuthDbContext context, IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        // GET: api/Auth/Roles
        [HttpGet("Roles")]
        public async Task<ActionResult<IEnumerable<RoleDTO>>> GetRoles()
        {
            var roles = await _context.Roles
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .ToListAsync();

            var roleDTOs = roles.Select(role => new RoleDTO
            {
                Id = role.Id,
                RoleName = role.RoleName,
                Permissions = role.RolePermissions
                    .Where(rp => rp.Permission != null) // Filtrar referencias nulas
                    .Select(rp => new PermissionDTO
                    {
                        Id = rp.Permission!.Id, // '!' indica que estamos seguros de que no será nulo
                        PermissionName = rp.Permission.PermissionName,
                        Description = rp.Permission.Description
                    })
                    .ToList()
            }).ToList();

            return Ok(roleDTOs);
        }

        // POST: api/Auth/Register
        [HttpPost("Register")]
        public async Task<ActionResult<UserResponseDTO>> Register([FromBody] RegisterUserDTO registerUserDTO)
        {
            // Validar si el nombre de usuario ya existe
            if (await _context.Users.AnyAsync(u => u.Username == registerUserDTO.Username))
            {
                return BadRequest($"El nombre de usuario '{registerUserDTO.Username}' ya está en uso.");
            }

            // Validar si el correo electrónico ya existe
            if (await _context.Users.AnyAsync(u => u.Email == registerUserDTO.Email))
            {
                return BadRequest($"El correo electrónico '{registerUserDTO.Email}' ya está en uso.");
            }

            // Validar si el IdEmpleado es válido si está presente
            if (registerUserDTO.IdEmpleado.HasValue)
            {
                var isValidEmpleado = await VerifyIdEmpleado(registerUserDTO.IdEmpleado.Value);
                if (!isValidEmpleado)
                {
                    return BadRequest("IdEmpleado no es válido o el empleado no está activo.");
                }
            }

            // Generar hash de la contraseña
            var hashedPassword = HashPassword(registerUserDTO.PasswordHash);

            // Obtener roles seleccionados
            var roles = await _context.Roles
                .Where(r => registerUserDTO.RoleIds.Contains(r.Id))
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .ToListAsync();

            // Crear el usuario
            var user = new User
            {
                Username = registerUserDTO.Username,
                Email = registerUserDTO.Email,
                PasswordHash = hashedPassword,
                Roles = roles,
                IsActive = registerUserDTO.IsActive,
                IdEmpleado = registerUserDTO.IdEmpleado
            };

            // Agregar usuario a la base de datos
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Crear respuesta
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
            // Validar que la clave JWT no sea nula o vacía
            var jwtKey = _configuration["Jwt:Key"];
            if (string.IsNullOrEmpty(jwtKey))
            {
                throw new InvalidOperationException("La clave JWT no está configurada en el archivo de configuración.");
            }

            var issuer = _configuration["Jwt:Issuer"];
            if (string.IsNullOrEmpty(issuer))
            {
                throw new InvalidOperationException("El emisor JWT no está configurado en el archivo de configuración.");
            }

            var audience = _configuration["Jwt:Audience"];
            if (string.IsNullOrEmpty(audience))
            {
                throw new InvalidOperationException("La audiencia JWT no está configurada en el archivo de configuración.");
            }

            // Generar la clave y las credenciales
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Crear los claims
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, user.Username),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim("IdEmpleado", user.IdEmpleado?.ToString() ?? "")
    };

            claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role.RoleName)));
            claims.AddRange(user.Roles
                .SelectMany(role => role.RolePermissions
                    .Where(rp => rp.Permission != null) // Filtrar permisos nulos
                    .Select(rp => new Claim("Permission", rp.Permission!.PermissionName))));

            // Crear el token JWT
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
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
                    Permissions = role.RolePermissions
                        .Where(rp => rp.Permission != null) // Filtrar referencias nulas
                        .Select(rp => new PermissionDTO
                        {
                            Id = rp.Permission!.Id, // Usamos '!' para indicar que no será null después del filtro
                            PermissionName = rp.Permission.PermissionName,
                            Description = rp.Permission.Description
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
                    Permissions = role.RolePermissions
                        .Where(rp => rp.Permission != null) // Filtrar referencias nulas
                        .Select(rp => new PermissionDTO
                        {
                            Id = rp.Permission!.Id, // Usamos '!' porque estamos seguros de que no será null
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

            user.Username = updateUserDTO.Username;
            user.Email = updateUserDTO.Email;

            var roles = await _context.Roles
                .Where(r => updateUserDTO.RoleIds.Contains(r.Id))
                .ToListAsync();

            user.Roles = roles;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Método para verificar si el IdEmpleado es válido en FuncionarioService
        private async Task<bool> VerifyIdEmpleado(int idEmpleado)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var serviceUrl = _configuration["FuncionarioServiceUrl"];
                var response = await client.GetAsync($"{serviceUrl}/{idEmpleado}");

                if (response.IsSuccessStatusCode)
                {
                    var empleadoData = await response.Content.ReadAsStringAsync();
                    var empleado = JsonConvert.DeserializeObject<EmpleadoDTO>(empleadoData);

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
