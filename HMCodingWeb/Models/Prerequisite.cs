using System;
using System.Collections.Generic;

namespace HMCodingWeb.Models;

public partial class Prerequisite
{
    public int RankId { get; set; }

    public int DifficultyId { get; set; }

    public int AtLeast { get; set; }

    public virtual DifficultyLevel Difficulty { get; set; } = null!;

    public virtual Rank Rank { get; set; } = null!;
}
