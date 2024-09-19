using System;
using System.Collections.Generic;

namespace HMCodingWeb.Models;

public partial class Exercise
{
    public long Id { get; set; }

    public string ExerciseCode { get; set; } = null!;

    public string? ExerciseName { get; set; }

    public string? ExerciseContent { get; set; }

    public string? InputFile { get; set; }

    public string? OutputFile { get; set; }

    public int NumberTestcase { get; set; }

    public DateTime? CreatedDate { get; set; }

    public string KindMarking { get; set; } = null!;

    public string TypeMarking { get; set; } = null!;

    public double RuntimeLimit { get; set; }

    public int DifficultyId { get; set; }

    public int ChapterId { get; set; }

    public long UserCreatedId { get; set; }

    public int AccessId { get; set; }

    public bool IsExam { get; set; }

    public bool IsAccept { get; set; }

    public virtual AccessRole Access { get; set; } = null!;

    public virtual Chapter Chapter { get; set; } = null!;

    public virtual ICollection<CopyPasteHistory> CopyPasteHistories { get; set; } = new List<CopyPasteHistory>();

    public virtual DifficultyLevel Difficulty { get; set; } = null!;

    public virtual ICollection<ExerciseBelongType> ExerciseBelongTypes { get; set; } = new List<ExerciseBelongType>();

    public virtual ICollection<TestCase> TestCases { get; set; } = new List<TestCase>();

    public virtual User UserCreated { get; set; } = null!;
}
