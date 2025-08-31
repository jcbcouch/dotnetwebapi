using AutoMapper;
using webapi.DTOs;
using webapi.Models;

namespace webapi.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Source -> Target
        CreateMap<CreatePostDto, Post>();
        CreateMap<UpdatePostDto, Post>();
        CreateMap<Post, PostDto>();
    }
}
