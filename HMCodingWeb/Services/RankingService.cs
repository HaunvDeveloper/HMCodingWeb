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
            // Lấy thông tin user
            var user = await _context.Users
                .Include(u => u.Auth)
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return (false, ""); // User không tồn tại
            }

            // Kiểm tra Tối Thượng (Supreme) - Chỉ dành cho admin hoặc teacher
            if (user.Auth != null && (user.Auth.AuthCode == "admin"))
            {
                user.RankId = 10; // Supreme
                await _context.SaveChangesAsync();
                return (false, "");
            } 

            // Đếm số bài hoàn thành theo mức độ
            var completedExercises = await _context.Markings
                .Include(ec => ec.Exercise)
                .Where(ec => ec.UserId == userId && ec.IsAllCorrect == true) 
                .GroupBy(ec => ec.Exercise.DifficultyId, ec => ec.ExerciseId)
                .Select(g => new { LevelId = g.Key, Count = g.Count() })
                .ToListAsync();

            var exerciseCounts = new int[7]; // Index 0 không dùng, 1-5 tương ứng Dễ đến Ác mộng
            foreach (var group in completedExercises)
            {
                if (group.LevelId >= 1 && group.LevelId <= 6)
                {
                    exerciseCounts[group.LevelId] = group.Count;
                }
            }

            // Kiểm tra tiêu chí rank từ cao đến thấp
            int newRankId = 0; // Mặc định Unranked
            bool isMasterEligible = false;

            // Champion (Quán Quân)
            if (user.Point >= 501)
            {
                // Kiểm tra điều kiện Cao Thủ
                if (exerciseCounts[2] >= 1 && exerciseCounts[3] >= 8 && exerciseCounts[4] >= 10 &&
                    exerciseCounts[5] >= 8 && exerciseCounts[6] >= 5)
                {
                    isMasterEligible = true;
                    // Kiểm tra điểm cao nhất
                    var maxPoint = await _context.Users
                        .Where(u => u.Id != userId && u.Point >= 151 && u.Point <= 300)
                        .MaxAsync(u => (int?)u.Point) ?? 0;
                    if (user.Point > maxPoint)
                    {
                        newRankId = 9; // Champion
                    }
                }
            }

            // Challenger (Thách Đấu)
            if (newRankId == 0 && user.Point >= 301 && user.Point <= 500 &&
                exerciseCounts[2] >= 1 && exerciseCounts[3] >= 8 && exerciseCounts[4] >= 10 &&
                exerciseCounts[5] >= 8 && exerciseCounts[6] >= 5)
            {
                newRankId = 8;
            }

            // Master (Cao Thủ)
            if (newRankId == 0 && user.Point >= 151 && user.Point <= 300 &&
                exerciseCounts[2] >= 1 && exerciseCounts[3] >= 8 && exerciseCounts[4] >= 10 &&
                exerciseCounts[5] >= 8 && exerciseCounts[6] >= 1)
            {
                newRankId = 7;
                isMasterEligible = true;
            }

            // Elite (Tinh Anh)
            if (newRankId == 0 && user.Point >= 121 && user.Point <= 150 &&
                exerciseCounts[2] >= 1 && exerciseCounts[3] >= 8 && exerciseCounts[4] >= 10 &&
                exerciseCounts[5] >= 4)
            {
                newRankId = 6;
            }

            // Diamond (Kim Cương)
            if (newRankId == 0 && user.Point >= 81 && user.Point <= 120 &&
                exerciseCounts[2] >= 1 && exerciseCounts[3] >= 8 && exerciseCounts[4] >= 6 &&
                exerciseCounts[5] >= 1)
            {
                newRankId = 5;
            }

            // Platinum (Bạch Kim)
            if (newRankId == 0 && user.Point >= 51 && user.Point <= 80 &&
                exerciseCounts[2] >= 1 && exerciseCounts[3] >= 8 && exerciseCounts[4] >= 2)
            {
                newRankId = 4;
            }

            // Gold (Vàng)
            if (newRankId == 0 && user.Point >= 26 && user.Point <= 50 &&
                exerciseCounts[2] >= 1 && exerciseCounts[3] >= 3 && exerciseCounts[4] >= 1)
            {
                newRankId = 3;
            }

            // Silver (Bạc)
            if (newRankId == 0 && user.Point >= 11 && user.Point <= 25 &&
                exerciseCounts[2] >= 1 && exerciseCounts[3] >= 1)
            {
                newRankId = 2;
            }

            // Copper (Đồng)
            if (newRankId == 0 && user.Point >= 1 && user.Point <= 10 &&
                exerciseCounts[2] >= 1)
            {
                newRankId = 1;
            }

            // Cập nhật RankId
            if (user.RankId != newRankId)
            {
                user.RankId = newRankId == 0 ? null : newRankId;
                await _context.SaveChangesAsync();
                return (true, _context.Ranks.AsNoTracking().SingleOrDefault(x => x.Id == newRankId).RankName ?? ""); // Rank đã được cập nhật
            }
            // Không có thay đổi gì
            return (false, ""); // Rank không thay đổi
        }
    }
}
