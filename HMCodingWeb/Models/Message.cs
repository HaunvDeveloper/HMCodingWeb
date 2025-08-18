using System;
using System.Collections.Generic;

namespace HMCodingWeb.Models;

public partial class Message
{
    public long Id { get; set; }

    public long BoxChatId { get; set; }

    public long SenderId { get; set; }

    public string? Content { get; set; }

    public string MessageType { get; set; } = null!;

    public string? FileUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public virtual BoxChat BoxChat { get; set; } = null!;

    public virtual ICollection<MessageReadStatus> MessageReadStatuses { get; set; } = new List<MessageReadStatus>();

    public virtual User Sender { get; set; } = null!;
}
