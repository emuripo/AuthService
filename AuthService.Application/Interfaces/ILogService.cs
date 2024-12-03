using AuthService.Application.DTO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuthService.Application.Interfaces
{
    public interface ILogService
    {
        Task RegistrarLogAsync(LogDTO logDto);
        Task<IEnumerable<LogDTO>> ObtenerLogsAsync();
    }
}
