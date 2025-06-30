using System;
using System.Collections.Generic;

namespace HMCodingWeb.Models;

public partial class Notification
{
    public long Id { get; set; }

    public string Title { get; set; } = null!;

    public string Message { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public long? CreatedByUserId { get; set; }

    public string? CreatedByUsername { get; set; }

    public string Type { get; set; } = null!;

    public bool IsGlobal { get; set; }

    public bool IsImportant { get; set; }

    public bool IsSendEmail { get; set; }

    public virtual ICollection<NotificationSeenStatus> NotificationSeenStatuses { get; set; } = new List<NotificationSeenStatus>();
}
