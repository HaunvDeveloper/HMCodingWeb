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



        public async Task<(Rank CurrentRank, Rank NextRank, Dictionary<int, int> MissingPrerequisites)> GetNextRankPrerequisites(long userId)
        {
            // Lấy user và rank hiện tại
            var user = await _context.Users
                .Include(u => u.Rank)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return (null, null, null);

            // Nếu user là admin (Supreme) hoặc không có rank, trả về null
            if (user.Auth?.AuthCode == "admin" || user.RankId == 10)
                return (user.Rank, null, null);

            // Lấy danh sách bài đã hoàn thành đúng
            var correctExerciseIds = await _context.Markings
                .Where(m => m.UserId == userId && m.IsAllCorrect)
                .Select(m => m.ExerciseId)
                .Distinct()
                .ToListAsync();

            // Đếm số bài đã hoàn thành theo độ khó
            var completedByDifficulty = await _context.Exercises
                .Where(e => correctExerciseIds.Contains(e.Id))
                .GroupBy(e => e.DifficultyId)
                .Select(g => new { DifficultyId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.DifficultyId, g => g.Count);

            // Lấy tất cả rank, trừ Supreme, sắp xếp theo MinLimitPoint tăng dần
            var ranks = await _context.Ranks
                .Include(r => r.PrerequisitesNavigation)
                .ThenInclude(p => p.Difficulty)
                .Where(r => r.Id != 10)
                .OrderBy(r => r.MinLimitPoint)
                .ToListAsync();

            // Tìm rank hiện tại và rank tiếp theo
            Rank currentRank = user.Rank;
            Rank nextRank = null;

            if (currentRank != null)
            {
                nextRank = ranks.FirstOrDefault(r => r.MinLimitPoint > currentRank.MinLimitPoint);
            }
            else
            {
                nextRank = ranks.FirstOrDefault(); // Nếu chưa có rank, lấy rank đầu tiên (Đồng)
            }

            // Nếu không có rank tiếp theo (đã ở rank cao nhất, không tính Supreme)
            if (nextRank == null)
                return (currentRank, null, null);

            // Tính điều kiện còn thiếu cho rank tiếp theo
            var missingPrerequisites = new Dictionary<int, int>();
            foreach (var pr in nextRank.PrerequisitesNavigation)
            {
                int completedCount = completedByDifficulty.TryGetValue(pr.DifficultyId, out var count) ? count : 0;
                int missingCount = pr.AtLeast - completedCount;
                if (missingCount > 0)
                {
                    missingPrerequisites[pr.DifficultyId] = missingCount;
                }
            }

            // Đặc biệt cho rank Champion (Id = 9)
            if (nextRank.Id == 8) // Nếu rank tiếp theo là Master
            {
                var maxPoint = await _context.Users
                    .Where(u => u.RankId == 8 || u.RankId == 9)
                    .MaxAsync(u => (int?)u.Point) ?? 0;

                if (user.Point > maxPoint)
                {
                    nextRank = ranks.FirstOrDefault(r => r.Id == 9); // Champion
                    missingPrerequisites.Clear(); // Champion không yêu cầu thêm prerequisites
                }
            }

            return (currentRank, nextRank, missingPrerequisites);
        }

    }
}
