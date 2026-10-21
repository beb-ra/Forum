using ForumWebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ForumWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImagesController : ControllerBase
    {
        private readonly IImageService _imageService;

        public ImagesController(IImageService imageService)
        {
            _imageService = imageService;
        }

        [HttpPost("upload")]
        [Authorize]
        public async Task<IActionResult> Upload([FromForm] List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
                return BadRequest("No files selected");
            if (files.Count > 4)
                return BadRequest("You can upload no more than 4 images");

            var urls = new List<string>();
            foreach (var file in files)
            {
                try
                {
                    var url = await _imageService.UploadImage(file);
                    urls.Add(url);
                }
                catch (Exception ex)
                {
                    return BadRequest($"File upload error: {ex.Message}");
                }
            }
            return Ok(new { ImageUrls = urls });
        }
    }
}
