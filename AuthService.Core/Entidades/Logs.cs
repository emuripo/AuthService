using System;
using System.ComponentModel.DataAnnotations;

namespace AuthService.Core.Entidades
{
    public class Logs
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string EventName { get; set; } = string.Empty; 

        public string EventDetails { get; set; } = string.Empty; 

        [Required]
        [MaxLength(255)]
        public string Username { get; set; } = string.Empty; 

        [MaxLength(255)]
        public string UserRole { get; set; } = string.Empty; 

        public int? IdEmpleado { get; set; } 

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow; 
    }
}
