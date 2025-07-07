using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using HMCodingWeb.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using Microsoft.AspNetCore.Authorization;
using System.Security.Cryptography;
using HMCodingWeb.Services;
using Newtonsoft.Json;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using HMCodingWeb.Middlewares;

namespace HMCodingWeb.Controllers
{
    [Authorize]
    public class UserInfoController : Controller
    {
        private readonly OnlineCodingWebContext _context;
        private readonly OnlineUsersService _onlineUsersService;
        private readonly RankingService _rankingService;
        private const long MaxAvatarSize = 2 * 1024 * 1024; // 2MB


        public UserInfoController(OnlineCodingWebContext context, UserListService userListService, OnlineUsersService onlineUsersService, RankingService rankingService)
        {
            _context = context;
            _onlineUsersService = onlineUsersService;
            _rankingService = rankingService;
        }
        // GET: User/UserDetail/5
        [Authorize]
        public async Task<IActionResult> Details(long? id)
        {
            long userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            id ??= userId;
            if (id <= 0)
            {
                return NotFound();
            }
            var user = await _context.Users
                .Include(u => u.ThemeCode)
                .Include(u => u.ProgramLanguage)
                .Include(u => u.Rank)
                .Include(u => u.Auth)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            var (currentRank, nextRank, missingPrerequisites) = await _rankingService.GetNextRankPrerequisites(id ?? 0);

            ViewBag.CurrentRank = currentRank;
            ViewBag.NextRank = nextRank;
            ViewBag.MissingPrerequisites = missingPrerequisites;
            ViewBag.Difficulties = await _context.DifficultyLevels.ToListAsync();
            ViewBag.OwnUserId = userId;
            return View(user);
        }


        // GET: User/Edit
        [Authorize]
        public async Task<IActionResult> EditProfile()
        {
            var id = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            ViewBag.ProgramLanguages = await _context.ProgramLanguages.ToListAsync();
            ViewBag.Themes = await _context.Themes.ToListAsync();
            return View(user);
        }

        // POST: User/Edit
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> EditProfile(User user, IFormFile? avatarFile)
        {
            var currentUserId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            if (user.Id != currentUserId)
            {
                return Json(new { status = false, message = "You can only edit your own profile." });
            }


            try
            {
                var existingUser = await _context.Users.FindAsync(user.Id);
                if (existingUser == null)
                {
                    return Json(new { status = false, message = "User not found." });
                }

                // Update fields
                existingUser.Fullname = user.Fullname;
                if(string.IsNullOrEmpty(existingUser.Email))
                {
                    existingUser.Email = user.Email;
                }
                existingUser.PhoneNumber = user.PhoneNumber;
                existingUser.Birthday = user.Birthday;
                existingUser.ProgramLanguageId = user.ProgramLanguageId;
                existingUser.ThemeCodeId = user.ThemeCodeId;

                // Handle avatar upload
                if (avatarFile != null)
                {
                    if (avatarFile.Length > MaxAvatarSize)
                    {
                        return Json(new { status = false, message = "Avatar file size must be less than 2MB." });
                    }

                    using var memoryStream = new MemoryStream();
                    await avatarFile.CopyToAsync(memoryStream);
                    existingUser.AvartarImage = memoryStream.ToArray();

                }

                await _context.SaveChangesAsync();
                return Json(new { status = true, userId = user.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Users.AnyAsync(e => e.Id == user.Id))
                {
                    return Json(new { status = false, message = "User not found." });
                }
                return Json(new { status = false, message = "Concurrency error occurred." });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "An error occurred: " + ex.Message });
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAvatar()
        {
            var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var user = await _context.Users.FindAsync(userId);

            if (user == null || user.AvartarImage == null)
            {
                return Json(new { status = false, message = "Avatar not found." });
            }

            return Json(new { status = true, avatar = Convert.ToBase64String(user.AvartarImage) });
        }

    }
}
