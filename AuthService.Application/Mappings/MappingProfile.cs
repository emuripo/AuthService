using AutoMapper;
using AuthService.Application.DTO;
using AuthService.Core.Entidades;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AuthService.Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Mapeos entre User y UserDTO
            CreateMap<User, UserDTO>()
                .ForMember(dest => dest.Roles, opt => opt.MapFrom(src => src.Roles));

            CreateMap<UserDTO, User>();

            // Mapeos entre Role y RoleDTO
            CreateMap<Role, RoleDTO>()
                .ForMember(dest => dest.Permissions, opt => opt.MapFrom(src => src.Permissions));

            CreateMap<RoleDTO, Role>();

            // Mapeos entre Permission y PermissionDTO
            CreateMap<Permission, PermissionDTO>();

            CreateMap<PermissionDTO, Permission>();
        }
    }
}
