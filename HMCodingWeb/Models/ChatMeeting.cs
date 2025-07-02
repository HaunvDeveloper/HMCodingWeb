using System;
using System.Collections.Generic;

namespace HMCodingWeb.Models;

public partial class ChatMeeting
{
    public Guid Id { get; set; }

    public Guid MeetingId { get; set; }

    public long SenderUserId { get; set; }

    public string Message { get; set; } = null!;

    public DateTimeOffset SentAt { get; set; }

    public virtual Meeting Meeting { get; set; } = null!;

    public virtual User SenderUser { get; set; } = null!;
}
