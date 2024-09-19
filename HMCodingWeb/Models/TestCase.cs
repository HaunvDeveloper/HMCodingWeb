using System;
using System.Collections.Generic;

namespace HMCodingWeb.Models;

public partial class TestCase
{
    public long Id { get; set; }

    public int Position { get; set; }

    public string? Input { get; set; }

    public string? Output { get; set; }

    public long ExerciseId { get; set; }

    public virtual Exercise Exercise { get; set; } = null!;
}
