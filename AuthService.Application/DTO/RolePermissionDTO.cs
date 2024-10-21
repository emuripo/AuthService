using AuthService.Core.Entidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Application.DTO
{
    public class RolePermissionDTO
    {
        public int RoleId { get; set; }
        public int PermissionId { get; set; }
    }
}

