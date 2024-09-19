using System;
using System.Collections.Generic;

namespace HMCodingWeb.Models;

public partial class Codepad
{
    public long Id { get; set; }

    public string FileName { get; set; } = null!;

    public string? CodeContent { get; set; }

    public DateTime CreateDate { get; set; }

    public DateTime UpdateDate { get; set; }

    public string? InputFile { get; set; }

    public string? OutputFile { get; set; }

    public int ProgramLanguageId { get; set; }

    public long UserId { get; set; }

    public int AccessId { get; set; }

    public virtual AccessRole Access { get; set; } = null!;

    public virtual ProgramLanguage ProgramLanguage { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
