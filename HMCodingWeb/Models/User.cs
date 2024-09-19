using System;
using System.Collections.Generic;

namespace HMCodingWeb.Models;

public partial class User
{
    public long Id { get; set; }

    public string Username { get; set; } = null!;

    public string Password { get; set; } = null!;

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

    public virtual Authority Auth { get; set; } = null!;

    public virtual ICollection<Codepad> Codepads { get; set; } = new List<Codepad>();

    public virtual ICollection<CopyPasteHistory> CopyPasteHistories { get; set; } = new List<CopyPasteHistory>();

    public virtual ICollection<Exercise> Exercises { get; set; } = new List<Exercise>();

    public virtual ProgramLanguage ProgramLanguage { get; set; } = null!;

    public virtual Rank? Rank { get; set; }

    public virtual Theme ThemeCode { get; set; } = null!;
}
