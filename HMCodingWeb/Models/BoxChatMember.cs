using System;
using System.Collections.Generic;

namespace HMCodingWeb.Models;

public partial class BoxChatMember
{
    public long BoxChatId { get; set; }

    public long UserId { get; set; }

    public string? DisplayName { get; set; }

    public DateTime JoinedAt { get; set; }

    public string RoleInChat { get; set; } = null!;

    public string? NotificationStatus { get; set; }

    public virtual BoxChat BoxChat { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
