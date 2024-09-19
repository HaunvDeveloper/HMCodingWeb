using System;
using System.Collections.Generic;

namespace HMCodingWeb.Models;

public partial class ExerciseBelongType
{
    public long Id { get; set; }

    public long ExerciseId { get; set; }

    public int ExerciseTypeId { get; set; }

    public string? Description { get; set; }

    public virtual Exercise Exercise { get; set; } = null!;

    public virtual ExerciseType ExerciseType { get; set; } = null!;
}
