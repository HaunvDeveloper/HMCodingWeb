using System;
using System.Collections.Generic;

namespace HMCodingWeb.Models;

public partial class UserContactToCommentEx
{
    public long UserId { get; set; }

    public long CommentExId { get; set; }

    public DateTime ContactDate { get; set; }

    public bool IsLike { get; set; }

    public virtual CommentToExercise CommentEx { get; set; } = null!;
}
