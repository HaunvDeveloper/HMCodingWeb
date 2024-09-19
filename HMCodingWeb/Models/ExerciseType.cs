using System;
using System.Collections.Generic;

namespace HMCodingWeb.Models;

public partial class ExerciseType
{
    public int Id { get; set; }

    public string TypeCode { get; set; } = null!;

    public string TypeName { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<ExerciseBelongType> ExerciseBelongTypes { get; set; } = new List<ExerciseBelongType>();
}
