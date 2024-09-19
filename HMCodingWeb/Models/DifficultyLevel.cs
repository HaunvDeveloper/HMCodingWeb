using System;
using System.Collections.Generic;

namespace HMCodingWeb.Models;

public partial class DifficultyLevel
{
    public int Id { get; set; }

    public string DifficultyCode { get; set; } = null!;

    public string DifficultyName { get; set; } = null!;

    public int Point { get; set; }

    public virtual ICollection<Exercise> Exercises { get; set; } = new List<Exercise>();
}
