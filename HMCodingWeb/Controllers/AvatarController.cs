using HMCodingWeb.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace HMCodingWeb.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AvatarController : ControllerBase
    {
        private readonly OnlineCodingWebContext _context;
        public AvatarController(OnlineCodingWebContext context)
        {
            _context = context;
        }

        [HttpGet("{userId}")]
        [ResponseCache(Duration = 10800, Location = ResponseCacheLocation.Any, NoStore = false)]
        public IActionResult GetAvatar(long userId)
        {
            var user = _context.Users.Find(userId);
            if (user == null || user.AvartarImage == null)
            {
                var defaultAvatarPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "assets", "images", "avartardefault.jpg");
                var imageBytes = System.IO.File.ReadAllBytes(defaultAvatarPath);
                return File(imageBytes, "image/jpeg"); // đổi "image/png" nếu ảnh là png
            }

            return File(user.AvartarImage, "image/png");
        }

        [HttpGet("group/{boxId}")]
        [ResponseCache(Duration = 10800, Location = ResponseCacheLocation.Any, NoStore = false)]
        public IActionResult GetAvatarBox(long boxId)
        {
            var avatar = _context.BoxChats
                .Where(b => b.Id == boxId)
                .Select(b => b.AvatarGroup)
                .FirstOrDefault();
            if (avatar == null)
            {
                return NotFound(); // hoặc trả ảnh mặc định
            }
            // Giả sử avatar là byte[]
            return File(avatar, "image/png"); // hoặc "image/png" tùy định dạng
        }
    }
}
