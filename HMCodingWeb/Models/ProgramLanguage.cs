using System;
using System.Collections.Generic;

namespace HMCodingWeb.Models;

public partial class ProgramLanguage
{
    public int Id { get; set; }

    public string ProgramLanguageCode { get; set; } = null!;

    public string? ProgramLanguageName { get; set; }

    public virtual ICollection<Codepad> Codepads { get; set; } = new List<Codepad>();

    public virtual ICollection<Marking> Markings { get; set; } = new List<Marking>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
