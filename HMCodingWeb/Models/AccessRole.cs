using System;
using System.Collections.Generic;

namespace HMCodingWeb.Models;

public partial class AccessRole
{
    public int Id { get; set; }

    public string? AccessCode { get; set; }

    public string? AccessName { get; set; }

    public string? Description { get; set; }

    public virtual ICollection<Codepad> Codepads { get; set; } = new List<Codepad>();

    public virtual ICollection<Exercise> Exercises { get; set; } = new List<Exercise>();
}
