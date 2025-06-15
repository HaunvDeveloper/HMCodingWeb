using HMCodingWeb.Models;
using Humanizer;
using Microsoft.EntityFrameworkCore;

namespace HMCodingWeb.Services
{
    public class UserPointService
    {
        private readonly OnlineCodingWebContext _context;
        public UserPointService(OnlineCodingWebContext context)
        {
            _context = context;
        }

        public bool CheckingPassedExercise(long userId, long exerciseId)
        {
            return _context.Markings.Any(us => us.UserId == userId && us.ExerciseId == exerciseId && us.IsAllCorrect == true);
        }

        public async Task<int> AddPointPassedExercise(long userId, long exerciseId)
        {
            // Check if the user has already passed the exercise
            if (CheckingPassedExercise(userId, exerciseId))
            {
                return 0 ;
            }

            // Fetch user and difficulty point in a single query
            var query = from user in _context.Users
                        where user.Id == userId
                        join exercise in _context.Exercises on exerciseId equals exercise.Id
                        join difficulty in _context.DifficultyLevels on exercise.DifficultyId equals difficulty.Id
                        select new { User = user, Point = difficulty.Point };

            var result = await query.FirstOrDefaultAsync();

            if (result == null)
            {
                return 0; // User or exercise not found
            }

            // Update user points
            result.User.Point += result.Point;
            _context.Users.Update(result.User);
            await _context.SaveChangesAsync();

            return result.Point;
        }
    }
}
