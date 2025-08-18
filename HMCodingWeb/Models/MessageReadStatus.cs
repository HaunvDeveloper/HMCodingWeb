using System;
using System.Collections.Generic;

namespace HMCodingWeb.Models;

public partial class MessageReadStatus
{
    public long MessageId { get; set; }

    public long UserId { get; set; }

    public DateTime ReadAt { get; set; }

    public virtual Message Message { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
