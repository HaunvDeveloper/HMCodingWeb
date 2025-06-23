using HMCodingWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace HMCodingWeb.Services
{
    public class RankingService
    {
        private readonly OnlineCodingWebContext _context;
        public RankingService(OnlineCodingWebContext context)
        {
            _context = context;
        }


        public async Task<(bool isGain, string rankName)> UpdateRankUser(long userId)
        {
            var user = await _context.Users
                .Include(u => u.Auth)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return (false, "");

            if (user.Auth != null && user.Auth.AuthCode == "admin")
            {
                user.RankId = 10; // Supreme
                await _context.SaveChangesAsync();
                return (false, "");
            }

            // Bước 1: Lấy danh sách các ExerciseId đã hoàn thành đúng ít nhất 1 lần
            var correctExerciseIds = await _context.Markings
                .Where(m => m.UserId == userId && m.IsAllCorrect)
                .Select(m => m.ExerciseId)
                .Distinct()
                .ToListAsync();

            // Bước 2: Lấy DifficultyId của các bài đã làm đúng, rồi đếm theo độ khó
            var completedByDifficulty = await _context.Exercises
                .Where(e => correctExerciseIds.Contains(e.Id))
                .GroupBy(e => e.DifficultyId)
                .Select(g => new { DifficultyId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.DifficultyId, g => g.Count);



            // Lấy tất cả Rank (trừ Supreme), sắp xếp theo độ khó giảm dần
            var ranks = await _context.Ranks
                .Include(r => r.PrerequisitesNavigation)
                .Where(r => r.Id != 10)
                .OrderByDescending(r => r.MinLimitPoint)
                .ToListAsync();

            int? newRankId = null;

            foreach (var rank in ranks)
            {
                if (user.Point < rank.MinLimitPoint) continue;

                bool satisfiedAll = rank.PrerequisitesNavigation.All(pr =>
                    completedByDifficulty.TryGetValue(pr.DifficultyId, out var count) && count >= pr.AtLeast
                );

                if (satisfiedAll)
                {
                    newRankId = rank.Id;
                    break;
                }
            }
            if (newRankId == 8)
            {
                var maxPoint = await _context.Users
                    .Where(u => u.RankId == 8 || u.RankId == 9)
                    .MaxAsync(u => (int?)u.Point) ?? 0;
                if (user.Point > maxPoint)
                {
                    newRankId = 9; // Chuyển sang Rank Master nếu điểm cao hơn
                }
            }

            // Cập nhật nếu có thay đổi
            if (user.RankId != newRankId)
            {
                user.RankId = newRankId;
                await _context.SaveChangesAsync();

                var rankName = await _context.Ranks
                    .AsNoTracking()
                    .Where(r => r.Id == newRankId)
                    .Select(r => r.RankName)
                    .FirstOrDefaultAsync();

                return (true, rankName ?? "");
            }

            return (false, "");
        }

    }
}
