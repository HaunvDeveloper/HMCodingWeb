using System;
using System.Collections.Generic;

namespace HMCodingWeb.Models;

public partial class Authority
{
    public int Id { get; set; }

    public string AuthCode { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
