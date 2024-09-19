using System;
using System.Collections.Generic;

namespace HMCodingWeb.Models;

public partial class Theme
{
    public int Id { get; set; }

    public string ThemeCode { get; set; } = null!;

    public string? ThemeStyle { get; set; }

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
