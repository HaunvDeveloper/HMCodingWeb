using System;
using System.Collections.Generic;

namespace HMCodingWeb.Models;

public partial class Chapter
{
    public int Id { get; set; }

    public string ChapterCode { get; set; } = null!;

    public string ChapterName { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<Exercise> Exercises { get; set; } = new List<Exercise>();
}
