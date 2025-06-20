using System;
using System.Collections.Generic;

namespace HMCodingWeb.Models;

public partial class Notification
{
    public long Id { get; set; }

    public string Title { get; set; } = null!;

    public string Message { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public long? ToUserId { get; set; }

    public long? CreatedByUserId { get; set; }

    public string Type { get; set; } = null!;

    public bool IsGlobal { get; set; }

    public virtual User? ToUser { get; set; }
}
