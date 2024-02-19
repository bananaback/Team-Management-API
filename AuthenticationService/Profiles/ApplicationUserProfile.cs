using AuthenticationService.Dtos;
using AuthenticationService.Models;
using AutoMapper;

namespace AuthenticationService.Profiles;

public class ApplicationUserProfile : Profile
{
    public ApplicationUserProfile()
    {
        CreateMap<ApplicationUserCreateDto, ApplicationUser>()
            .ForMember(dest => dest.ExternalUserId, options => options.MapFrom(src => src.ExternalUserId));
    }
}