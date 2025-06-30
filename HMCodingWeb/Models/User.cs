using System;
using System.Collections.Generic;

namespace HMCodingWeb.Models;

public partial class User
{
    public long Id { get; set; }

    public string Username { get; set; } = null!;

    public string? Password { get; set; }

    public string? RetypePassword { get; set; }

    public string Fullname { get; set; } = null!;

    public DateOnly? Birthday { get; set; }

    public DateTime? RegisterTime { get; set; }

    public string Email { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public int ProgramLanguageId { get; set; }

    public int ThemeCodeId { get; set; }

    public int AuthId { get; set; }

    public int? RankId { get; set; }

    public string? Otp { get; set; }

    public DateTime? OtplatestSend { get; set; }

    public bool IsBlock { get; set; }

    public long Point { get; set; }

    public byte[]? AvartarImage { get; set; }

    public virtual ICollection<Announcement> Announcements { get; set; } = new List<Announcement>();

    public virtual Authority Auth { get; set; } = null!;

    public virtual ICollection<Codepad> Codepads { get; set; } = new List<Codepad>();

    public virtual ICollection<CommentToExercise> CommentToExercises { get; set; } = new List<CommentToExercise>();

    public virtual ICollection<CopyPasteHistory> CopyPasteHistories { get; set; } = new List<CopyPasteHistory>();

    public virtual ICollection<Marking> Markings { get; set; } = new List<Marking>();

    public virtual ICollection<NotificationSeenStatus> NotificationSeenStatuses { get; set; } = new List<NotificationSeenStatus>();

    public virtual ProgramLanguage ProgramLanguage { get; set; } = null!;

    public virtual Rank? Rank { get; set; }

    public virtual Theme ThemeCode { get; set; } = null!;
}
