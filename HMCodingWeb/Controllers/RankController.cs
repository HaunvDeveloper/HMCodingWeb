using HMCodingWeb.Models;
using HMCodingWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HMCodingWeb.Controllers
{
    public class RankController : Controller
    {
        private readonly OnlineCodingWebContext _context;
        private readonly UserListService _userListService;

        public RankController(OnlineCodingWebContext context, UserListService userListService)
        {
            _context = context;
            _userListService = userListService;
        }
        public IActionResult Index(long id = 1)
        {
            var ranks = _context.Ranks.ToList();
            ViewBag.CurrentRankId = id; 
            return View(ranks);
        }

        public IActionResult RankingList()
        {
            return View();
        }

        public async Task<IActionResult> _GetRankingList(int draw, int start, int length, string keyword = "", [FromForm] Dictionary<string, string>[] order = null)
        {
            // Base query to get users with their program language and rank
            var query = _context.Users
                .Include(u => u.ProgramLanguage)
                .Include(u => u.Rank)
                .AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(keyword))
            {
                keyword = keyword.ToLower();
                query = query.Where(u => u.Username.ToLower().Contains(keyword) ||
                                        (u.Fullname != null && u.Fullname.ToLower().Contains(keyword)));
            }

            // Get total records
            var totalRecords = query.Count();

            // Apply sorting
            if (order != null && order.Length > 0)
            {
                IOrderedQueryable<User> orderedQuery = null;
                for (int i = 0; i < order.Length; i++)
                {
                    var columnIndex = int.Parse(order[i]["column"]);
                    var direction = order[i]["dir"].ToLower();
                    var columnName = _userListService.GetColumnName(columnIndex);

                    if (i == 0)
                    {
                        orderedQuery = _userListService.ApplyOrder(query, columnName, direction);
                    }
                    else
                    {
                        orderedQuery = _userListService.ApplyThenOrder(orderedQuery, columnName, direction);
                    }
                }
                query = orderedQuery ?? query;
            }
            else
            {
                // Default sorting: RankId descending, then Point descending
                query = query.OrderByDescending(u => u.RankId ?? int.MaxValue)
                             .ThenByDescending(u => u.Point);
            }

            // Fetch paginated data
            var users = await query
                .Skip(start)
                .Take(length)
                .Select(u => new
                {
                    id = u.Id,
                    avatar = $"/api/avatar/{u.Id}",
                    username = u.Username,
                    fullname = u.Fullname ?? "N/A",
                    programLanguage = u.ProgramLanguage != null ? u.ProgramLanguage.ProgramLanguageName : "N/A",
                    point = u.Point,
                    rankId = u.RankId,
                    rankCode = u.Rank != null ? u.Rank.RankCode : "unranked",
                    rankName = u.Rank != null ? u.Rank.RankName : "Unranked"
                })
                .AsNoTracking()
                .ToListAsync();

            // Return JSON response for DataTable
            return Json(new
            {
                draw = draw,
                recordsTotal = totalRecords,
                recordsFiltered = totalRecords, // If more complex filtering is added, update this
                data = users
            });
        }
    }
}
