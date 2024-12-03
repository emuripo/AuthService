using AuthService.Application.DTO;
using AuthService.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace AuthService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LogsController : ControllerBase
    {
        private readonly ILogService _logService;

        public LogsController(ILogService logService)
        {
            _logService = logService;
        }

        [HttpPost]
        public async Task<IActionResult> RegistrarLog([FromBody] LogDTO logDto)
        {
            if (logDto == null || string.IsNullOrWhiteSpace(logDto.EventName))
            {
                return BadRequest("El log proporcionado no es válido.");
            }

            try
            {
                await _logService.RegistrarLogAsync(logDto);
                return Ok("Log registrado con éxito.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerLogs()
        {
            try
            {
                var logs = await _logService.ObtenerLogsAsync();
                return Ok(logs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }
    }
}
