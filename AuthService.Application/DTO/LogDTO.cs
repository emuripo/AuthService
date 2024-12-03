using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Application.DTO
{
    public class LogDTO
    {
        public string EventName { get; set; } = string.Empty;
        public string EventDetails { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
        public int? IdEmpleado { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
