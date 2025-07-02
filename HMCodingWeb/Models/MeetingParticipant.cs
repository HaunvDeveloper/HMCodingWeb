using System;
using System.Collections.Generic;

namespace HMCodingWeb.Models;

public partial class MeetingParticipant
{
    public Guid MeetingId { get; set; }

    public long UserId { get; set; }

    public string Role { get; set; } = null!;

    public DateTime? JoinedAt { get; set; }

    public virtual Meeting Meeting { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
