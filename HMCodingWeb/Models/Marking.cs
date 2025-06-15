using System;
using System.Collections.Generic;

namespace HMCodingWeb.Models;

public partial class Marking
{
    public long Id { get; set; }

    public long ExerciseId { get; set; }

    public long UserId { get; set; }

    public bool IsAllCorrect { get; set; }

    public double Score { get; set; }

    public int CorrectTestNumber { get; set; }

    public DateTime MarkingDate { get; set; }

    public bool IsExam { get; set; }

    public bool IsError { get; set; }

    public string KindMarking { get; set; } = null!;

    public string TypeMarking { get; set; } = null!;

    public int ProgramLanguageId { get; set; }

    public string? SourceCode { get; set; }

    public string? InputFile { get; set; }

    public string? OutputFile { get; set; }

    public double TimeLimit { get; set; }

    public TimeOnly? TimeSpent { get; set; }

    public string? ResultContent { get; set; }

    public virtual Exercise Exercise { get; set; } = null!;

    public virtual ICollection<MarkingDetail> MarkingDetails { get; set; } = new List<MarkingDetail>();

    public virtual ProgramLanguage ProgramLanguage { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
