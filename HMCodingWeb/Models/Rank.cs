using System;
using System.Collections.Generic;

namespace HMCodingWeb.Models;

public partial class Rank
{
    public int Id { get; set; }

    public string RankCode { get; set; } = null!;

    public string? RankName { get; set; }

    public int MinLimitPoint { get; set; }

    public int MaxLimitPoint { get; set; }

    public string? Prerequisites { get; set; }

    public string? Description { get; set; }

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
