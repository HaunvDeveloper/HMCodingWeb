using System;
using System.Collections.Generic;

namespace HMCodingWeb.Models;

public partial class NotificationSeenStatus
{
    public long UserId { get; set; }

    public long NotificationId { get; set; }

    public bool IsSeen { get; set; }

    public DateTime? SeenAt { get; set; }

    public virtual Notification Notification { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
