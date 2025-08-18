using System;
using System.Collections.Generic;

namespace HMCodingWeb.Models;

public partial class BoxChat
{
    public long Id { get; set; }

    public string? Name { get; set; }

    public bool IsGroup { get; set; }

    public long CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public byte[]? AvatarGroup { get; set; }

    public virtual ICollection<BoxChatMember> BoxChatMembers { get; set; } = new List<BoxChatMember>();

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
}
