using HMCodingWeb.Areas.Admin.Models;
using HMCodingWeb.Models;
using HMCodingWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml.ConditionalFormatting.Contracts;

namespace HMCodingWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "admin")]
    public class RankController : Controller
    {
        private readonly OnlineCodingWebContext _context;
        private readonly RankingService _rankingService;

        public RankController(OnlineCodingWebContext context, RankingService rankingService)
        {
            _context = context;
            _rankingService = rankingService;
        }

        public IActionResult Index()
        {
            var ranks = _context.Ranks
                .Select(r => new RankViewModel
                {
                    Id = r.Id,
                    RankCode = r.RankCode,
                    RankName = r.RankName,
                    UserCount = r.Users.Count
                })
                .ToList();

            return View(ranks);
        }


        public async Task<IActionResult> Edit(int id)
        {
            var rank = await _context.Ranks
                .Include(r => r.PrerequisitesNavigation)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (rank == null) return NotFound();

            var allDifficulties = await _context.DifficultyLevels.ToListAsync();

            var model = new EditRankViewModel
            {
                Id = rank.Id,
                RankCode = rank.RankCode,
                RankName = rank.RankName,
                MinLimitPoint = rank.MinLimitPoint,
                MaxLimitPoint = rank.MaxLimitPoint,
                Description = rank.Description,
                DifficultyPrerequisites = allDifficulties
                    .Select(d => new DifficultyPrerequisiteDto
                    {
                        DifficultyId = d.Id,
                        DifficultyName = d.DifficultyName,
                        AtLeast = rank.PrerequisitesNavigation
                            .FirstOrDefault(p => p.DifficultyId == d.Id)?.AtLeast ?? 0
                    }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit([FromBody] EditRankViewModel model)
        {
            var rank = await _context.Ranks
                .Include(r => r.PrerequisitesNavigation)
                .FirstOrDefaultAsync(r => r.Id == model.Id);

            if (rank == null)
                return NotFound();

            // Cập nhật rank info
            rank.RankCode = model.RankCode;
            rank.RankName = model.RankName;
            rank.MinLimitPoint = model.MinLimitPoint;
            rank.MaxLimitPoint = model.MaxLimitPoint;
            rank.Description = model.Description;

            // Cập nhật Prerequisite
            foreach (var item in model.DifficultyPrerequisites)
            {
                var prerequisite = rank.PrerequisitesNavigation
                    .FirstOrDefault(p => p.DifficultyId == item.DifficultyId);

                if (prerequisite == null && item.AtLeast > 0)
                {
                    _context.Prerequisites.Add(new Prerequisite
                    {
                        RankId = rank.Id,
                        DifficultyId = item.DifficultyId,
                        AtLeast = item.AtLeast
                    });
                }
                else if (prerequisite != null)
                {
                    if (item.AtLeast > 0)
                        prerequisite.AtLeast = item.AtLeast;
                    else
                        _context.Prerequisites.Remove(prerequisite);
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new {status = true, message="Thành công"});
        }

        [HttpPost]
        public async Task<IActionResult> UpdateAllRanks()
        {
            var allUsers = await _context.Users
                .Include(u => u.Rank)
                .ToListAsync();
            foreach (var user in allUsers)
            {
                await _rankingService.UpdateRankUser(user.Id);
            }
            return Ok(new { status = true, message = "Cập nhật xếp hạng thành công" });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateRankUser(long id)
        {
            if(!_context.Users.Any(u => u.Id == id))
                return Ok(new {status=false, error="Không tìm thấy người dùng"});

            var (isGain, rankName) = await _rankingService.UpdateRankUser(id);
            if (isGain)
            {
                return Ok(new { status = true, message = $"Cập nhật xếp hạng thành công. User đã đạt được xếp hạng: {rankName}" });
            }
            return Ok(new { status = true, message = "Cập nhật xếp hạng thành công" });
        }

    }
}
