using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace ForumWebAPI.Services
{
    public interface IImageService
    {
        Task<string> UploadImage(IFormFile file);
    }
}
