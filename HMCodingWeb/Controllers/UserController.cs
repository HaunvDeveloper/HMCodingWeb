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
    public class UserController : Controller
    {
        private readonly OnlineCodingWebContext _context;
        private readonly EmailSendService _emailSendService;
        private readonly OnlineUsersService _onlineUsersService;
        private readonly RankingService _rankingService;
        private const long MaxAvatarSize = 2 * 1024 * 1024; // 2MB
        private readonly IConfiguration _configuration;

        public UserController(OnlineCodingWebContext context, EmailSendService emailSendService, UserListService userListService, OnlineUsersService onlineUsersService, IConfiguration configuration, RankingService rankingService)
        {
            _context = context;
            _emailSendService = emailSendService;
            _onlineUsersService = onlineUsersService;
            _configuration = configuration;
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
                existingUser.Email = user.Email;
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





        private string MaskEmail(string email)
        {
            var atIndex = email.IndexOf('@');
            if (atIndex <= 3)
            {
                return email;
            }
            var emailName = email.Substring(0, atIndex);
            var domain = email.Substring(atIndex);
            var maskedEmailName = emailName.Substring(0, 3) + new string('*', emailName.Length - 3);
            return maskedEmailName + domain;
        }

        [Route("login")]
        [AllowAnonymous]
        public IActionResult Login()
        {
            TempData.Clear();
            return View();
        }

        [Route("login")]
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login(User model, string ReturnUrl)
        {
            TempData.Clear();
            var user = _context.Users.Include(x => x.Auth)
                .FirstOrDefault(x => x.Username == model.Username && x.Password == model.Password);

            if (user == null || user.Auth == null)
            {
                return Json(new { status = false, message = "Tài khoản hoặc mật khẩu không chính xác!" });
            }
            else if (user.IsBlock == true)
            {
                return Json(new { status = false, message = "Tài khoản của bạn đã bị khóa!" });
            }
            else
            {

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.GivenName, user.Fullname ?? ""),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Role, user.Auth.AuthCode),
                    new Claim(ClaimTypes.Authentication, user.Auth.AuthCode),
                };

                var identity = new ClaimsIdentity(claims, "Cookies");
                var principal = new ClaimsPrincipal(identity);


                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTime.UtcNow.AddHours(3)
                };

                await HttpContext.SignInAsync(principal, authProperties);
                _onlineUsersService.AddUser(user.Id.ToString(), user.Username, user.Fullname ?? "", user.Auth.AuthCode, null);

                return Json(new { status = true, message = "Đăng nhập thành công!", redirectUrl = string.IsNullOrEmpty(ReturnUrl) ? Url.Action("Index", "Home") : ReturnUrl });
            }
        }


        [Route("logout")]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                _onlineUsersService.RemoveUserByUserId(userId);
            }
            TempData.Clear();
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "User");
        }

        [Route("signup")]
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Signup()
        {
            TempData.Clear();
            ViewBag.SiteKey = _configuration["ReCaptcha:SiteKey"];
            return View();
        }

        [Route("signup")]
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Signup(User model)
        {
            TempData.Clear();

            var captchaResponse = Request.Form["g-recaptcha-response"];
            var isCaptchaValid = await IsCaptchaValid(captchaResponse);
            ViewBag.SiteKey = _configuration["ReCaptcha:SiteKey"];
            if (!isCaptchaValid)
            {
                ViewBag.Alert = "Xác thực reCAPTCHA thất bại. Vui lòng thử lại.";
                return View(model);
            }

            if (model != null)
            {
                string pattern = "^[a-zA-Z0-9]+$";
                if (!Regex.IsMatch(model.Username, pattern))
                {
                    ViewBag.Alert = "Username không hợp lệ! Chỉ cho phép chữ cái không dấu và số.";
                    return View(model);
                }
                //Check Exist
                var existUsername = await _context.Users.FirstOrDefaultAsync(x => x.Username == model.Username);
                if (existUsername != null)
                {
                    ViewBag.Alert = "Username đã tồn tại!";
                    return View(model);
                }

                //Check password length
                if(model.Password.Length < 8)
                {
                    ViewBag.Alert = "Password có ít nhất 8 ký tự!";
                    return View(model);
                }

                Random random = new Random();
                model.IsBlock = true;
                model.Otp = random.Next(100000, 999999).ToString();
                model.OtplatestSend = DateTime.Now;

                bool emailSent = await _emailSendService.SendOtpToEmail(model.Email ?? "", model.Otp);
                if (!emailSent)
                {
                    ViewBag.Alert = "Không thể gửi email, vui lòng thử lại!";
                    return View(model);
                }
                HttpContext.Session.SetString("UserSignup", JsonConvert.SerializeObject(model));
                return RedirectToAction("ConfirmOtp");
            }
            return View(model);
        }

        private async Task<bool> IsCaptchaValid(string captchaResponse)
        {
            var secretKey = _configuration["ReCaptcha:SecretKey"];
            var client = new HttpClient();
            var result = await client.PostAsync(
                $"https://www.google.com/recaptcha/api/siteverify?secret={secretKey}&response={captchaResponse}",
                null
            );

            var json = await result.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(json);
            return data?.success == "true" || data?.success == true;
        }



        [AllowAnonymous]
        [HttpGet]
        public IActionResult ConfirmOtp()
        {
            var userJson = HttpContext.Session.GetString("UserSignup");
            if (string.IsNullOrEmpty(userJson))
            {
                return RedirectToAction("Signup");
            }
            var user = JsonConvert.DeserializeObject<User>(userJson);
            if (user == null)
            {
                return RedirectToAction("Signup");
            }
            ViewBag.Email = MaskEmail(user.Email);
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> ConfirmOtp(string otp)
        {
            var userJson = HttpContext.Session.GetString("UserSignup");
            if (string.IsNullOrEmpty(userJson))
            {
                return RedirectToAction("Signup");
            }

            var user = JsonConvert.DeserializeObject<User>(userJson);
            if (user == null)
            {
                return RedirectToAction("Signup");
            }
            ViewBag.Email = MaskEmail(user.Email);
            if (user.OtplatestSend.HasValue && (DateTime.Now - user.OtplatestSend.Value).TotalMinutes > 10)
            {
                ViewBag.Alert = "Mã OTP đã hết hạn!!!";
                HttpContext.Session.Clear();
                return View();
            }
            if (user.Otp != otp)
            {
                ViewBag.Alert = "OTP không chính xác!!!";
                return View();
            }

            user.RegisterTime = DateTime.Now;
            user.RetypePassword = null;
            user.AuthId = 4;
            user.RankId = null;
            user.IsBlock = false;
            user.Otp = null;
            user.OtplatestSend = null;
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            HttpContext.Session.Remove("UserSignup");


            var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Role, "student")
                };

            var identity = new ClaimsIdentity(claims, "Cookies");
            var principal = new ClaimsPrincipal(identity);


            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTime.UtcNow.AddHours(3)
            };

            await HttpContext.SignInAsync(principal, authProperties);
            _onlineUsersService.AddUser(user.Id.ToString(), user.Username, user.Fullname, "student", null);

            TempData["UserLogin"] = true;
            return RedirectToAction("Index", "Home");
        }


        [AllowAnonymous]
        public IActionResult ForgetPassword()
        {
            ViewBag.SiteKey = _configuration["ReCaptcha:SiteKey"];
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> ForgetPassword(string userIndent)
        {
            var captchaResponse = Request.Form["g-recaptcha-response"];
            var isCaptchaValid = await IsCaptchaValid(captchaResponse);
            ViewBag.SiteKey = _configuration["ReCaptcha:SiteKey"];
            if (!isCaptchaValid)
            {
                ViewBag.Alert = "Xác thực reCAPTCHA thất bại. Vui lòng thử lại.";
                return View();
            }
            var model = new User();
            if (userIndent.Contains('@'))
            {
                model = await _context.Users.FirstOrDefaultAsync(x => x.Email == userIndent);
            }
            else
            {
                model = await _context.Users.FirstOrDefaultAsync(x => x.Username == userIndent);
            }
            if (model == null)
            {
                ViewBag.Alert = "User không tồn tại!!!";
                return View();
            }
            if (model.IsBlock)
            {
                ViewBag.Alert = "User bị khóa!!!";
                return View();
            }
            Random random = new Random();
            model.Otp = random.Next(100000, 999999).ToString();
            model.OtplatestSend = DateTime.Now;
            model.AvartarImage = null; // Xóa đi ảnh để tránh lưu trữ không cần thiết
            HttpContext.Session.SetString("VerifyAccount", JsonConvert.SerializeObject(model));

            bool emailSent = await _emailSendService.SendOtpToEmail(model.Email ?? "", model.Otp);
            if (!emailSent)
            {
                ViewBag.Alert = "Không thể gửi email, vui lòng thử lại!";
                return View();
            }

            return RedirectToAction("VerifyAccount");
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult VerifyAccount()
        {
            var userJson = HttpContext.Session.GetString("VerifyAccount");
            if (string.IsNullOrEmpty(userJson))
            {
                return RedirectToAction("Login");
            }
            var user = JsonConvert.DeserializeObject<User>(userJson);
            if (user == null)
            {
                return RedirectToAction("Login");
            }
            ViewBag.Email = MaskEmail(user.Email);
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public IActionResult VerifyAccount(string otp)
        {
            var userJson = HttpContext.Session.GetString("VerifyAccount");
            if (string.IsNullOrEmpty(userJson))
            {
                return RedirectToAction("Login");
            }

            var user = JsonConvert.DeserializeObject<User>(userJson);
            if (user == null)
            {
                return RedirectToAction("Login");
            }
            ViewBag.Email = MaskEmail(user.Email);
            if (user.OtplatestSend.HasValue && (DateTime.Now - user.OtplatestSend.Value).TotalMinutes > 10)
            {
                ViewBag.Alert = "Mã OTP đã hết hạn!!!";
                HttpContext.Session.Clear();
                return View();
            }
            if (user.Otp != otp)
            {
                ViewBag.Alert = "OTP không chính xác!!!";
                return View();
            }

            user.Otp = null;
            user.OtplatestSend = null;
            HttpContext.Session.Remove("VerifyAccount");

            // Chuyển long sang string trước khi lưu vào TempData
            TempData["ChangePassword"] = user.Id.ToString();

            return RedirectToAction("ChangePassword", "User");
        }

        [AllowAnonymous]
        public IActionResult ChangePassword()
        {
            if (TempData["ChangePassword"] == null)
            {
                TempData.Clear();
                return RedirectToAction("Login");
            }

            TempData.Keep("ChangePassword");
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> ChangePassword(string password)
        {
            if (TempData["ChangePassword"] == null)
            {
                return RedirectToAction("Login");
            }

            if (string.IsNullOrEmpty(password))
            {
                TempData.Keep("ChangePassword");
                ViewBag.Alert = "Password không thể rỗng";
                return View();
            }
            if (password.Length < 8)
            {
                TempData.Keep("ChangePassword");
                ViewBag.Alert = "Password phải ít nhất có 8 ký tự";
                return View();
            }

            // Chuyển đổi từ string sang long
            if (!long.TryParse(TempData["ChangePassword"]?.ToString(), out long id))
            {
                return RedirectToAction("Login");
            }
            TempData.Clear();
            var model = await _context.Users.FindAsync(id);
            if (model == null)
            {
                return RedirectToAction("Login");
            }
            model.Password = password;
            await _context.SaveChangesAsync();
            return RedirectToAction("Login");
        }

        [Authorize]
        public IActionResult ChangePasswordProfile()
        {
            var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var user = _context.Users.Find(userId);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ChangePasswordProfile(string oldpassword, string password)
        {
            var currentUserId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var user = await _context.Users.FindAsync(currentUserId);
            if (user?.Password != oldpassword)
            {
                ViewBag.Alert = "Mật khẩu cũ không chính xác!"; 
                return View();
            }
            if (string.IsNullOrEmpty(password) || password.Length < 8)
            {
                ViewBag.Alert = "Mật khẩu mới phải có ít nhất 8 ký tự.";
                return View();
            }
            if (password == oldpassword)
            {
                ViewBag.Alert = "Mật khẩu mới không được trùng với mật khẩu cũ.";
                return View();
            }
            user.Password = password;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return RedirectToAction("Details", new { id = currentUserId });
        }
    }
}
