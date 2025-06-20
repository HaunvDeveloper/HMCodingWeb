using System;
using System.Collections.Generic;

namespace HMCodingWeb.Models;

public partial class Announcement
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public long CreatedByUserId { get; set; }

    public bool IsVisible { get; set; }

    public virtual User CreatedByUser { get; set; } = null!;
}
