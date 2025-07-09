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
                return NotFound(); // hoặc trả ảnh mặc định
            }

            return File(user.AvartarImage, "image/png"); // hoặc "image/png" tùy định dạng
        }
    }
}
