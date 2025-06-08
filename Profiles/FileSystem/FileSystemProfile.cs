using AutoMapper;
using CloudShield.DTOs.FileSystem;
using CloudShield.Entities.Operations;

namespace CloudShield.Profiles.FileSystem;

public class FileSystemProfile : Profile
{
  public FileSystemProfile()
  {
    CreateMap<FileResource, FileItemDTO>()
        .ForMember(dest => dest.Category, opt => opt.MapFrom(src => GetCategoryFromPath(src.RelativePath)))
        .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreateAt))
        .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdateAt));


    CreateMap<FileResourceCloud, FileItemDTO>()
         .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreateAt))
         .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdateAt))
         .ForMember(dest => dest.Category, opt => opt.MapFrom(src => GetCategoryFromPath(src.RelativePath)))
         .ForMember(dest => dest.FileName, opt => opt.MapFrom(src => src.FileName))
         .ForMember(dest => dest.ContentType, opt => opt.MapFrom(src => src.ContentType))
         .ForMember(dest => dest.RelativePath, opt => opt.MapFrom(src => src.RelativePath))
         .ForMember(dest => dest.SizeBytes, opt => opt.MapFrom(src => src.SizeBytes));





  }

  private static string GetCategoryFromPath(string relativePath)
  {
    if (string.IsNullOrEmpty(relativePath))
      return "unknown";

    var parts = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
    return parts.Length > 0 ? parts[0] : "unknown";
  }
}
