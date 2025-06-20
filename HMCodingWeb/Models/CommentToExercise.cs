using System;
using System.Collections.Generic;

namespace HMCodingWeb.Models;

public partial class CommentToExercise
{
    public long Id { get; set; }

    public long ExerciseId { get; set; }

    public long UserId { get; set; }

    public string? Content { get; set; }

    public DateTime CreatedDate { get; set; }

    public long? AnswerToCmtId { get; set; }

    public bool IsApproved { get; set; }

    public int LikeQuantity { get; set; }

    public int DislikeQuantity { get; set; }

    public virtual CommentToExercise? AnswerToCmt { get; set; }

    public virtual Exercise Exercise { get; set; } = null!;

    public virtual ICollection<CommentToExercise> InverseAnswerToCmt { get; set; } = new List<CommentToExercise>();

    public virtual User User { get; set; } = null!;

    public virtual ICollection<UserContactToCommentEx> UserContactToCommentExes { get; set; } = new List<UserContactToCommentEx>();
}
