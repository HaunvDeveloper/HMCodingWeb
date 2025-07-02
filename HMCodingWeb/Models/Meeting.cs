using System;
using System.Collections.Generic;

namespace HMCodingWeb.Models;

public partial class Meeting
{
    public Guid Id { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public long HostUserId { get; set; }

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public bool IsPrivate { get; set; }

    public bool IsRequireToJoin { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<ChatMeeting> ChatMeetings { get; set; } = new List<ChatMeeting>();

    public virtual User HostUser { get; set; } = null!;

    public virtual ICollection<MeetingParticipant> MeetingParticipants { get; set; } = new List<MeetingParticipant>();
}
