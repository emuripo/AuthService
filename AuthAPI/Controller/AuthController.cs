using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AuthService.Core.Entidades;
using AuthService.Application.DTO;
using System.Collections.Generic;
using System.Threading.Tasks;
using AuthService.Infrastructure.Data;

namespace AuthService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AuthDbContext _context;

        public AuthController(AuthDbContext context)
        {
            _context = context;
        }

        // GET: api/Auth/Users
        [HttpGet("Users")]
        public async Task<ActionResult<IEnumerable<UserDTO>>> GetUsers()
        {
            var users = await _context.Users
                .Include(u => u.Roles) // Incluir la relación con Roles
                .ThenInclude(r => r.Permissions) // Incluir la relación de Roles con Permisos
                .ToListAsync();

            // Mapeo de entidades a DTOs
            var userDTOs = users.Select(user => new UserDTO
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Roles = user.Roles.Select(role => new RoleDTO
                {
                    Id = role.Id,
                    RoleName = role.RoleName,
                    Permissions = role.Permissions.Select(permission => new PermissionDTO
                    {
                        Id = permission.Id,
                        PermissionName = permission.PermissionName
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
                .ThenInclude(r => r.Permissions)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            // Mapeo de entidad a DTO
            var userDTO = new UserDTO
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Roles = user.Roles.Select(role => new RoleDTO
                {
                    Id = role.Id,
                    RoleName = role.RoleName,
                    Permissions = role.Permissions.Select(permission => new PermissionDTO
                    {
                        Id = permission.Id,
                        PermissionName = permission.PermissionName
                    }).ToList()
                }).ToList()
            };

            return Ok(userDTO);
        }

        // POST: api/Auth/Users
        [HttpPost("Users")]
        public async Task<ActionResult<User>> PostUser([FromBody] UserDTO userDTO)
        {
            var user = new User
            {
                Username = userDTO.Username,
                Email = userDTO.Email,
                PasswordHash = userDTO.PasswordHash, // Aquí deberíamos aplicar una estrategia de hashing
                Roles = userDTO.Roles.Select(roleDTO => new Role
                {
                    Id = roleDTO.Id, // Si el rol ya existe, solo usa el ID
                    RoleName = roleDTO.RoleName
                }).ToList()
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Devolver el usuario creado
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }

        // DELETE: api/Auth/Users/5
        [HttpDelete("Users/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PUT: api/Auth/Users/5
        [HttpPut("Users/{id}")]
        public async Task<IActionResult> PutUser(int id, [FromBody] UserDTO userDTO)
        {
            if (id != userDTO.Id)
            {
                return BadRequest();
            }

            var user = await _context.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            // Actualizar datos del usuario
            user.Username = userDTO.Username;
            user.Email = userDTO.Email;

            // Actualizar roles
            user.Roles.Clear(); // Limpiar roles existentes
            user.Roles = userDTO.Roles.Select(roleDTO => new Role
            {
                Id = roleDTO.Id,
                RoleName = roleDTO.RoleName
            }).ToList();

            _context.Entry(user).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(u => u.Id == id);
        }
    }
}
