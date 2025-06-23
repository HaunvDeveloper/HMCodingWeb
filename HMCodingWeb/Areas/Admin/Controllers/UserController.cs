using HMCodingWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Linq.Dynamic.Core;
using System.Security.Claims;

namespace HMCodingWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "admin,teacher")]
    public class UserController : Controller
    {
        private readonly OnlineCodingWebContext _context;
        private const long MaxAvatarSize = 2 * 1024 * 1024; // 2MB

        public UserController(OnlineCodingWebContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> _GetList(int draw, int start, int length, string keyword = "")
        {
            var query = _context.Users
                .Include(u => u.ProgramLanguage)
                .Include(u => u.Auth)
                .Include(u => u.Rank)
                .AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(keyword))
            {
                keyword = keyword.ToLower();
                query = query.Where(u => u.Username.ToLower().Contains(keyword) ||
                                        (u.Fullname != null && u.Fullname.ToLower().Contains(keyword)) ||
                                        (u.Email != null && u.Email.ToLower().Contains(keyword)) ||
                                        (u.PhoneNumber != null && u.PhoneNumber.Contains(keyword)));
            }

            // Get total records
            var totalRecords = await query.CountAsync();

            // Sorting
            //var order = Request.Query["order[0][column]"];
            //var orderDir = Convert.ToString(Request.Query["order[0][dir]"]);
            //if (!string.IsNullOrEmpty(order))
            //{
            //    var columnIndex = int.Parse(order);
            //    var columnName = GetColumnName(columnIndex);
            //    query = query.OrderBy($"{columnName} {(orderDir.ToLower() == "asc" ? "ascending" : "descending")}");
            //}
            //else
            //{
            //    query = query.OrderByDescending(u => u.RegisterTime);
            //}
            // Pagination
            var users = await query
                .Skip(start)
                .Take(length)
                .Select(u => new
                {
                    id = u.Id,
                    avatar = u.AvartarImage != null ? Convert.ToBase64String(u.AvartarImage) : null,
                    username = u.Username,
                    fullName = u.Fullname ?? "N/A",
                    phoneNo = u.PhoneNumber ?? "N/A",
                    email = u.Email ?? "N/A",
                    dayOfBirth = u.Birthday,
                    programLanguage = u.ProgramLanguage != null ? u.ProgramLanguage.ProgramLanguageName : "N/A",
                    authentication = u.Auth != null ? u.Auth.AuthCode : "N/A",
                    rank = u.Rank != null ? u.Rank.RankName : "Unranked",
                    point = u.Point,
                    isBlock = u.IsBlock,
                    registeredTime = u.RegisterTime
                })
                .AsNoTracking()
                .ToListAsync();

            return Json(new
            {
                draw = draw,
                recordsTotal = totalRecords,
                recordsFiltered = totalRecords,
                data = users
            });
        }

        public IActionResult Create()
        {
            ViewBag.ProgramLanguages = _context.ProgramLanguages.ToList();
            ViewBag.Themes = _context.Themes.ToList();
            ViewBag.Auths = _context.Authorities.ToList();
            ViewBag.Ranks = _context.Ranks.ToList();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(User model, IFormFile avatarFile)
        {
            // Kiểm tra Username trùng
            if (await _context.Users.AnyAsync(u => u.Username == model.Username))
            {
                return Json(new { status = false, message = "Tên đăng nhập đã tồn tại!" });
            }

            // Kiểm tra file avatar
            if (avatarFile != null)
            {
                if (avatarFile.Length > MaxAvatarSize)
                {
                    return Json(new { status = false, message = "Kích thước file avatar phải nhỏ hơn 2MB!" });
                }

                using (var stream = new MemoryStream())
                {
                    await avatarFile.CopyToAsync(stream);
                    model.AvartarImage = stream.ToArray();
                }
            }

            // Gán giá trị mặc định
            model.RegisterTime = DateTime.UtcNow.AddHours(7); // UTC+7
            model.Point = model.Point < 0 ? 0 : model.Point;

            try
            {
                _context.Users.Add(model);
                await _context.SaveChangesAsync();
                return Json(new { status = true });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = $"Lỗi khi tạo: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(long id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return Json(new { status = false, error = "Người dùng không tồn tại!" });
            }

            try
            {
                _context.Users.Remove(user);
                // Xóa các liên kết với các bảng khác nếu cần
                var codepads = _context.Codepads.Where(c => c.UserId == id);
                _context.Codepads.RemoveRange(codepads);

                var contact = _context.UserContactToCommentExes.Where(c => c.UserId == id);
                _context.UserContactToCommentExes.RemoveRange(contact);

                var comments = _context.CommentToExercises.Where(c => c.UserId == id);
                _context.CommentToExercises.RemoveRange(comments);

                await _context.SaveChangesAsync();
                return Json(new { status = true });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, error = $"Lỗi khi xóa: {ex.Message}" });
            }
        }

        

        public async Task<IActionResult> Edit(long id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            ViewBag.ProgramLanguages = await _context.ProgramLanguages.ToListAsync();
            ViewBag.Themes = await _context.Themes.ToListAsync();
            ViewBag.Auths = await _context.Authorities.ToListAsync();
            ViewBag.Ranks = await _context.Ranks.ToListAsync();

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(User user, IFormFile? avatarFile)
        {
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
                existingUser.AuthId = user.AuthId;
                existingUser.RankId = user.RankId;
                existingUser.IsBlock = user.IsBlock;
                existingUser.Point = user.Point;
                // Ensure password is not updated if RetypePassword is null or empty
                if (!string.IsNullOrEmpty(user.Password))
                {
                    existingUser.Password = user.Password; // Update password only if RetypePassword matches
                }

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
        
        public IActionResult CreateWithList()
        {
            return View();
        }

        [HttpGet]
        public IActionResult DownloadSample()
        {
            // Đường dẫn tới file trong thư mục wwwroot
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "assets", "Data", "MauUser.xlsx");

            // Kiểm tra nếu file tồn tại
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("File không tồn tại."+ filePath);
            }

            // Lấy nội dung file
            var fileBytes = System.IO.File.ReadAllBytes(filePath);

            // Trả file về client
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "MauSV.xlsx");
        }

        [HttpPost]
        public async Task<IActionResult> CreateWithList(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return Json(new { success = false, error = "File không hợp lệ!!!" });
            }

            try
            {
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);

                using var package = new OfficeOpenXml.ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets[0];
                var rowCount = worksheet.Dimension.Rows;
                var listUsers = new List<User>();
                List<string> error = new List<string>();
                int succNum = 0;
                for (int row = 7; row <= rowCount; row++)
                {
                    try
                    {
                       
                        var user = new User
                        {
                            Username = worksheet.Cells[row, 2].Text.Trim(),
                            Fullname = worksheet.Cells[row, 3].Text.Trim(),
                            Password = worksheet.Cells[row, 4].Text.Trim(),
                            Birthday = DateOnly.Parse(worksheet.Cells[row, 5].Text.Trim()),
                            Email = worksheet.Cells[row, 6].Text.Trim(),
                            PhoneNumber = worksheet.Cells[row, 7].Text.Trim(),
                            ProgramLanguageId = int.Parse(worksheet.Cells[row, 8].Text.Trim()),
                            AuthId = int.Parse(worksheet.Cells[row, 9].Text.Trim()),
                            RankId = int.Parse(worksheet.Cells[row, 10].Text.Trim()),
                            Point = long.Parse(worksheet.Cells[row, 11].Text.Trim()),
                            IsBlock = false,
                            ThemeCodeId = 1,
                            RegisterTime = DateTime.UtcNow.AddHours(7), // UTC+7,
                            AvartarImage = null // Assuming no avatar image is provided in the Excel file
                        };

                        if(listUsers.Any(u => u.Username == user.Username))
                        {
                            error.Add(user.Username + " is duplicated in the list.");
                            continue;
                        }
                        else
                        {
                            listUsers.Add(user);
                            succNum++;
                        }
                    }
                    catch (Exception ex)
                    {
                        error.Add(worksheet.Cells[row, 2].Text.Trim() + " has error: " + ex.ToString());
                        continue;
                    }
                }

                _context.Users.AddRange(listUsers);
                await _context.SaveChangesAsync();
                if (succNum > 0)
                {
                    return Json(new { status = true, redirect = Url.Action("Index", "User", new { area = "Admin" }), message = "Has some error:" + string.Join("\n", error) });
                }
                return Json(new
                {
                    status = true,
                    redirect = Url.Action("Index", "User", new { area = "Admin" })
                });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, error = ex.ToString() });
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetUserByUsername(string id)
        {
            if (string.IsNullOrEmpty(id) || id.Length < 3)
            {
                return BadRequest("Vui lòng nhập ít nhất 3 ký tự.");
            }

            var users = _context.Users
                .Where(s => s.Fullname.Contains(id) || s.Id.ToString().Contains(id) || s.Username.Contains(id))
                .Select(s => new
                {
                    Id = s.Id,
                    Username = s.Username
                })
                .Take(30) // Giới hạn kết quả
                .ToList();

            return Ok(users);
        }
    }
}