using AutoMapper;
using AuthService.Application.DTO;
using AuthService.Core.Entidades;

namespace AuthService.Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Mapeo entre User y UserDTO
            CreateMap<User, UserDTO>()
                .ForMember(dest => dest.Roles, opt => opt.MapFrom(src => src.Roles));

            CreateMap<UserDTO, User>();

            // Mapeo entre Role y RoleDTO
            CreateMap<Role, RoleDTO>()
                .ForMember(dest => dest.Permissions, opt => opt.MapFrom(src => src.RolePermissions.Select(rp => rp.Permission)));

            CreateMap<RoleDTO, Role>();

            // Mapeo entre Permission y PermissionDTO
            CreateMap<Permission, PermissionDTO>();
            CreateMap<PermissionDTO, Permission>();

            // Mapeo entre RolePermission y RolePermissionDTO
            CreateMap<RolePermission, RolePermissionDTO>();
            CreateMap<RolePermissionDTO, RolePermission>();
        }
    }
}
