using System;
using System.Collections.Generic;

namespace HMCodingWeb.Models;

public partial class CopyPasteHistory
{
    public long Id { get; set; }

    public DateTime LaunchTime { get; set; }

    public string Content { get; set; } = null!;

    public string Zone { get; set; } = null!;

    public string Type { get; set; } = null!;

    public long UserId { get; set; }

    public long ExerciseId { get; set; }

    public virtual Exercise Exercise { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
