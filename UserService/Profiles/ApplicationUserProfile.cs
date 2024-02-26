using AutoMapper;
using UserService.Dtos;
using UserService.Models;

namespace UserService.Profiles
{
    public class ApplicationUserProfile : Profile
    {
        public ApplicationUserProfile()
        {
            // Source --> Target
            CreateMap<ApplicationUser, ApplicationUserReadDto>();
            CreateMap<RegisterRequest, ApplicationUserCreateDto>()
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.Username))
                .ForMember(dest => dest.Password, opt => opt.MapFrom(src => src.Password));
            CreateMap<ApplicationUserCreateDto, ApplicationUser>();
            CreateMap<ApplicationUser, OutboxMessage>();
            CreateMap<OutboxMessage, PublishEventDto>();
        }
    }
}