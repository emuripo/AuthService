using AuthService.Application.DTO;
using AuthService.Application.Interfaces;
using AuthService.Core.Entidades;
using AuthService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthService.Infrastructure.Services
{
    public class LogService : ILogService
    {
        private readonly AuthDbContext _context;

        public LogService(AuthDbContext context)
        {
            _context = context;
        }

        public async Task RegistrarLogAsync(LogDTO logDto)
        {
            if (logDto == null || string.IsNullOrWhiteSpace(logDto.EventName))
            {
                throw new ArgumentException("El log proporcionado no es válido.");
            }

            var log = new Logs
            {
                EventName = logDto.EventName,
                EventDetails = logDto.EventDetails,
                Username = logDto.Username,
                UserRole = logDto.UserRole,
                IdEmpleado = logDto.IdEmpleado,
                Timestamp = DateTime.UtcNow
            };

            _context.Logs.Add(log);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<LogDTO>> ObtenerLogsAsync()
        {
            var logs = await _context.Logs
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();

            return logs.Select(log => new LogDTO
            {
                EventName = log.EventName,
                EventDetails = log.EventDetails,
                Username = log.Username,
                UserRole = log.UserRole,
                IdEmpleado = log.IdEmpleado,
                Timestamp = log.Timestamp
            });
        }
    }
}
