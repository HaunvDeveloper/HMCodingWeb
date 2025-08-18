using HMCodingWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HMCodingWeb.Controllers
{
    [Authorize]
    public class ExerciseCommentController : Controller
    {
        private readonly OnlineCodingWebContext _context;
        public ExerciseCommentController(OnlineCodingWebContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetComments(long exerciseId)
        {
            var userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "student";
            var comments = await _context.CommentToExercises
                .Include(c => c.User)
                .Include(c => c.InverseAnswerToCmt)
                    .ThenInclude(r => r.User)
                .Include(c => c.UserContactToCommentExes)
                .Where(c => c.ExerciseId == exerciseId && c.IsApproved)
                .OrderBy(c => c.CreatedDate)
                .ToListAsync();

            var commentDtos = comments
                .Select(c => new
                {
                    c.Id,
                    c.Content,
                    Username = c.User.Username,
                    CreatedDate = c.CreatedDate.ToString("o"),
                    LikeCount = c.UserContactToCommentExes.Count(uc => uc.IsLike),
                    DislikeCount = c.UserContactToCommentExes.Count(uc => !uc.IsLike),
                    UserLiked = c.UserContactToCommentExes.Any(uc => uc.UserId == userId && uc.IsLike),
                    UserDisliked = c.UserContactToCommentExes.Any(uc => uc.UserId == userId && !uc.IsLike),
                    CanDelete = userRole == "admin" || userRole == "teacher" || c.UserId == userId,
                    UserId = c.UserId,
                    AnswerToUser = c.AnswerToCmtId.HasValue ? c.AnswerToCmt?.User.Username : null,
                    AnswerToUserId = c.AnswerToCmtId.HasValue ? c.AnswerToCmt?.UserId : null,
                }).ToList();

            return Json(new { status = true, comments = commentDtos });
        }

        [HttpPost]
        public async Task<IActionResult> AddComment(long ExerciseId, string Content, long? AnswerToCmtId)
        {
            if (string.IsNullOrEmpty(Content))
            {
                return Json(new { status = false, message = "Nội dung bình luận không được để trống!" });
            }

            var userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var comment = new CommentToExercise
            {
                ExerciseId = ExerciseId,
                UserId = userId,
                Content = Content,
                CreatedDate = DateTime.Now,
                IsApproved = true,
                AnswerToCmtId = AnswerToCmtId
            };

            _context.CommentToExercises.Add(comment);
            await _context.SaveChangesAsync();

            return Json(new { status = true, message = "Bình luận đã được gửi!" });
        }

        [HttpPost]
        public async Task<IActionResult> LikeComment(long commentId)
        {
            var userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var comment = await _context.CommentToExercises.FindAsync(commentId);
            if (comment == null)
            {
                return Json(new { status = false, message = "Bình luận không tồn tại!" });
            }

            var contact = await _context.UserContactToCommentExes
                .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.CommentExId == commentId);

            if (contact == null)
            {
                // Chưa tương tác: Thêm Like
                _context.UserContactToCommentExes.Add(new UserContactToCommentEx
                {
                    UserId = userId,
                    CommentExId = commentId,
                    IsLike = true,
                    ContactDate = DateTime.Now
                });
            }
            else if (contact.IsLike)
            {
                // Đã Like: Xóa tương tác
                _context.UserContactToCommentExes.Remove(contact);
            }
            else
            {
                // Đã Dislike: Chuyển sang Like
                contact.IsLike = true;
                contact.ContactDate = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return Json(new { status = true });
        }

        [HttpPost]
        public async Task<IActionResult> DislikeComment(long commentId)
        {
            var userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var comment = await _context.CommentToExercises.FindAsync(commentId);
            if (comment == null)
            {
                return Json(new { status = false, message = "Bình luận không tồn tại!" });
            }

            var contact = await _context.UserContactToCommentExes
                .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.CommentExId == commentId);

            if (contact == null)
            {
                // Chưa tương tác: Thêm Dislike
                _context.UserContactToCommentExes.Add(new UserContactToCommentEx
                {
                    UserId = userId,
                    CommentExId = commentId,
                    IsLike = false,
                    ContactDate = DateTime.Now
                });
            }
            else if (!contact.IsLike)
            {
                // Đã Dislike: Xóa tương tác
                _context.UserContactToCommentExes.Remove(contact);
            }
            else
            {
                // Đã Like: Chuyển sang Dislike
                contact.IsLike = false;
                contact.ContactDate = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return Json(new { status = true });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteComment(long commentId)
        {
            var userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var comment = await _context.CommentToExercises
                .Include(c => c.InverseAnswerToCmt)
                .FirstOrDefaultAsync(c => c.Id == commentId);

            if (comment == null)
            {
                return Json(new { status = false, message = "Bình luận không tồn tại!" });
            }
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "student";
            if (!(userRole == "admin" || userRole == "teacher" || comment.UserId == userId))
            {
                return Json(new { status = false, message = "Bạn không có quyền xóa bình luận này!" });
            }

            // Thu thập tất cả bình luận cần xóa (đệ quy)
            var commentIdsToDelete = new List<long>();
            CollectCommentIds(comment, commentIdsToDelete);

            // Xóa tương tác Like/Dislike
            var contacts = await _context.UserContactToCommentExes
                .Where(uc => commentIdsToDelete.Contains(uc.CommentExId))
                .ToListAsync();
            _context.UserContactToCommentExes.RemoveRange(contacts);

            // Xóa bình luận
            var commentsToDelete = await _context.CommentToExercises
                .Where(c => commentIdsToDelete.Contains(c.Id))
                .ToListAsync();
            _context.CommentToExercises.RemoveRange(commentsToDelete);

            await _context.SaveChangesAsync();
            return Json(new { status = true, message = "Bình luận đã được xóa!" });
        }

        private void CollectCommentIds(CommentToExercise comment, List<long> commentIds)
        {
            commentIds.Add(comment.Id);
            foreach (var reply in comment.InverseAnswerToCmt)
            {
                CollectCommentIds(reply, commentIds);
            }
        }

    }
}
