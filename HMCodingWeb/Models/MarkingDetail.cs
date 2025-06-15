using System;
using System.Collections.Generic;

namespace HMCodingWeb.Models;

public partial class MarkingDetail
{
    public long Id { get; set; }

    public long MarkingId { get; set; }

    public long? TestCaseId { get; set; }

    public int? TestCaseIndex { get; set; }

    public string? Input { get; set; }

    public string? Output { get; set; }

    public string? CorrectOutput { get; set; }

    public bool IsCorrect { get; set; }

    public double RunTime { get; set; }

    public bool IsTimeLimitExceed { get; set; }

    public bool IsError { get; set; }

    public string? ErrorContent { get; set; }

    public virtual Marking Marking { get; set; } = null!;
}
