using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.IO;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Drawing.Printing;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using HMCodingWeb.Models;
using HMCodingWeb.Services;
using HMCodingWeb.ViewModels;
using System.Collections.Generic;

namespace HMCodingWeb.Controllers
{
    [Authorize]
    public class CodePadController : Controller
    {
        private readonly ILogger<CodePadController> _logger;
        private readonly OnlineCodingWebContext _context;
        private readonly RunProcessService _runProcessService;

        public CodePadController(ILogger<CodePadController> logger, OnlineCodingWebContext context, RunProcessService runProcessService)
        {
            _logger = logger;
            _context = context;
            _runProcessService = runProcessService;
        }

        [HttpGet]
        public IActionResult Index()
        {

            ViewBag.ProgramLanguageList = _context.ProgramLanguages.ToList();
            ViewBag.AccessList = _context.AccessRoles.ToList();

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> _GetList(int p = 1, int s = 10, string key = "")
        {
            long userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            IQueryable<Codepad> query = _context.Codepads
                .Where(cp => cp.UserId == userId)
                .Include(x => x.ProgramLanguage)
                .Include(x => x.Access);


            if (!string.IsNullOrEmpty(key))
            {
                ViewBag.Search = Uri.EscapeDataString(key);
                key = key.ToLower();
                query = query
                    .Where(cp =>
                        cp.FileName.ToLower().Contains(key) ||
                        cp.Id.ToString().Contains(key) ||
                        cp.ProgramLanguage.ProgramLanguageName.ToLower().Contains(key) ||
                        cp.CreateDate.ToString().Contains(key)
                    );
            }
            int totalItems = await query.CountAsync();

            var codepadList = await query
                .OrderByDescending(cp => cp.CreateDate)
                .Skip((p - 1) * s)
                .Take(s)
                .Select(x => new Codepad
                {
                    Id = x.Id,
                    FileName = x.FileName,
                    AccessId = x.AccessId,
                    CreateDate = x.CreateDate,
                    ProgramLanguageId = x.ProgramLanguageId,
                    UserId = x.UserId,
                    Access = x.Access,
                    ProgramLanguage = x.ProgramLanguage,
                    User = x.User,
                })
                .ToListAsync();

            ViewBag.ProgramLanguageList = _context.ProgramLanguages.ToList();
            ViewBag.AccessList = _context.AccessRoles.ToList();

            // Tính tổng số trang dựa trên số lượng phần tử

            ViewBag.CurrentPage = p;
            ViewBag.PageSize = s;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / s);
            ViewBag.AvailablePageSizes = new List<int> { 5, 10, 20, 50, 100 };
            ViewBag.TotalRecords = totalItems;

            return PartialView(codepadList);
        }

        [HttpGet]
        public IActionResult View(long id)
        {
            var user = _context.Users.Find(id);
            if (user == null)
            {
                return NotFound();
            }
            else if(user.Id == long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0"))
            {
                return RedirectToAction("Index");
            }
            return View(user);
        }


        [HttpPost]
        public async Task<IActionResult> _GetListByUser(long id, int draw, int start = 0, int length = 10, string keyword = "")
        {
            var currentUserId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var user = _context.Users.Find(id);
            if (user == null)
            {
                return Json(new
                {
                    draw = draw,
                    recordsTotal = 0,
                    recordsFiltered = 0,
                    data = new List<Codepad>()
                });
            }
            
            var query = _context.Codepads
                .Where(cp => cp.UserId == user.Id && cp.AccessId == 3)
                .Include(cp => cp.ProgramLanguage)
                .AsQueryable();

            if(!string.IsNullOrEmpty(keyword))
            {
                keyword = keyword.ToLower();
                query = query.Where(cp =>
                    cp.FileName.ToLower().Contains(keyword) ||
                    cp.Id.ToString().Contains(keyword) ||
                    cp.ProgramLanguage.ProgramLanguageName.ToLower().Contains(keyword) ||
                    cp.CreateDate.ToString().Contains(keyword)
                );
            }

            var totalRecords = await query.CountAsync();

            var codepads = await query
                .Skip(start)
                .Take(length)
                .OrderByDescending(cp => cp.CreateDate)
                .Select(cp => new
                {
                    Id = cp.Id,
                    FileName = cp.FileName,
                    ProgramLanguage = cp.ProgramLanguage.ProgramLanguageName,
                    CreateDate = cp.CreateDate.ToString("dd/MM/yyyy HH:mm"),
                    UserId = cp.UserId,
                    InputFile = string.IsNullOrEmpty(cp.InputFile) ? "stdin" : cp.InputFile,
                    OutputFile = string.IsNullOrEmpty(cp.OutputFile) ? "stdout" : cp.OutputFile
                })
                .ToListAsync();

            ViewBag.UserInfo = user;
            return Json(new
            {
                draw = draw,
                recordsTotal = totalRecords,
                recordsFiltered = totalRecords,
                data = codepads
            });
        }

        

        [Route("codepad/code/{id?}")]
        public async Task<IActionResult> Code(int? id)
        {
            long userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var codepad = new Codepad();
            if (id.HasValue)
            {
                codepad = await _context.Codepads
                    .Include(x => x.User)
                    .FirstOrDefaultAsync(x => x.Id == id);
                if (!User.IsInRole("admin") && !User.IsInRole("teacher") && codepad?.UserId != userId && codepad?.AccessId == 1)
                {
                    return View("NotAccess");
                }
            }
            else
            {
                codepad = null;
            }


            ViewBag.ListTheme = _context.Themes.ToList();
            ViewBag.ProgramLanguageList = _context.ProgramLanguages.ToList();
            ViewBag.UserInfo = await _context.Users.FindAsync(userId);
            return View(codepad);
        }


        [HttpPost]
        [RequestSizeLimit(1L * 1024 * 1024 * 1024)]
        public async Task<IActionResult> RunProcessCodepad([FromBody]RunProcessViewModel model)
        {
            try
            {
                if (HttpContext.Session.GetString("IsRunning") == "true")
                {
                    return Json(new { IsError = true, Error = "Đang có một tiến trình chạy, vui lòng đợi!" });
                }
                HttpContext.Session.SetString("IsRunning", "true");
                model.UserId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                model.FileName = "codepadProcess";
                if (model.RunTime <= 0)
                    model.RunTime = 1;
                if (model.RunTime > 10)
                    model.RunTime = 10;


                var result = await _runProcessService.RunProcessWithInput(model, true);
                _runProcessService.ClearTempFolder(model.UserId);

                HttpContext.Session.SetString("IsRunning", "false");
                return Json(new { IsError = result.IsError, Error = result.Error, Output = result.Output, RunTime = model.RunTime, MemoryUsed = result.MemoryUsed });
            }
            catch (Exception ex)
            {
                HttpContext.Session.SetString("IsRunning", "false");
                return Json(new { IsError = true, Error = ex.ToString() });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Save(Codepad model)
        {
            long userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var codepad = await _context.Codepads.FindAsync(model.Id);
            if (codepad == null)
            {
                return Json(new { status = false, error = "File chưa được lưu!" });
            }
            else if (codepad.UserId != userId)
            {
                return Json(new { status = false, error = "Codepad không phải của người dùng!!" });
            }
            else
            {
                try
                {

                    codepad.FileName = model.FileName ?? codepad.FileName;
                    codepad.ProgramLanguageId = model.ProgramLanguageId;
                    codepad.InputFile = model.InputFile;
                    codepad.OutputFile = model.OutputFile;
                    codepad.CodeContent = model.CodeContent;
                    codepad.UpdateDate = DateTime.Now;
                    await _context.SaveChangesAsync();
                    return Json(new { status = true, messsage = "Lưu thành công" });
                }
                catch (Exception ex)
                {
                    return Json(new { status = false, error = ex.ToString() });
                }
            }
        }
        [HttpPost]
        public async Task<IActionResult> SaveAs(Codepad model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.FileName))
                {
                    return Json(new { status = false, error = "File name is null" });
                }


                model.UserId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                model.CreateDate = DateTime.Now;
                model.UpdateDate = DateTime.Now;
                model.AccessId = 1;
                _context.Codepads.Add(model);
                await _context.SaveChangesAsync();


                return Json(new { status = true, redirect = Url.Action("Code", "CodePad", new { id = model.Id }) });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, error = ex.ToString() });
            }
        }



        [HttpPost]
        public async Task<IActionResult> Delete(long id)
        {
            long userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var codepad = await _context.Codepads.FindAsync(id);
            if (codepad == null || codepad.UserId != userId)
            {
                return Json(new { status = false, error = "File không thuộc về người dùng" });
            }
            try
            {

                _context.Codepads.Remove(codepad);
                await _context.SaveChangesAsync();
                return Json(new { status = true, message = "Xóa thành công" });

            }
            catch (Exception ex)
            {
                return Json(new { status = false, error = ex.ToString() });
            }


        }

        [HttpPost]
        public async Task<IActionResult> ChangeAccessRole(long codepadId, int accessId)
        {
            long userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var codepad = await _context.Codepads.FindAsync(codepadId);
            if (codepad == null || codepad.UserId != userId)
            {
                return Json(new { status = false, error = "Codepad không tồn tại hoặc không thuộc quyền sở hữu của người dùng!" });
            }
            try
            {
                var accessRole = _context.AccessRoles.Find(accessId);
                if (accessRole == null)
                {
                    return Json(new { status = false, error = "Phân quyền không tồn tại!" });
                }
                codepad.AccessId = accessRole.Id;
                await _context.SaveChangesAsync();
                return Json(new { status = true, accessName = accessRole.AccessName });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, error = ex.ToString() });
            }
        }

    }
}
