using HMCodingWeb.Models;

namespace HMCodingWeb.ViewModels
{
    public class ExerciseViewModel
    {
        public ExerciseViewModel()
        {
            ExerciseCode = string.Empty;
            ExerciseName = string.Empty;
            ExerciseContent = string.Empty;
            InputFile = string.Empty;
            OutputFile = string.Empty;
            KindMarking = string.Empty;
            TypeMarking = string.Empty;
            DifficultyName = string.Empty;
            ExerciseTypeName = string.Empty;
        }

        public ExerciseViewModel(Exercise exercise, int numberSuccessful, bool isCorrect)
        {
            Id = exercise.Id;
            ExerciseCode = exercise.ExerciseCode;
            ExerciseName = exercise.ExerciseName;
            ExerciseContent = exercise.ExerciseContent;
            InputFile = exercise.InputFile;
            OutputFile = exercise.OutputFile;
            NumberTestcase = exercise.NumberTestcase;
            CreatedDate = exercise.CreatedDate;
            KindMarking = exercise.KindMarking;
            TypeMarking = exercise.TypeMarking;
            RuntimeLimit = exercise.RuntimeLimit;
            DifficultyId = exercise.DifficultyId;
            DifficultyName = exercise.Difficulty?.DifficultyName ?? string.Empty;
            ExerciseTypeName = string.Join(", ", exercise.ExerciseBelongTypes.Select(ebt => ebt.ExerciseType.TypeName).ToList());
            ChapterId = exercise.ChapterId;
            ChapterName = exercise.Chapter?.ChapterName ?? string.Empty;
            UserCreatedId = exercise.UserCreatedId;
            AccessId = exercise.AccessId;
            IsExam = exercise.IsExam;
            IsAccept = exercise.IsAccept;
            NumberSuccessful = numberSuccessful;
            IsCorrect = isCorrect;
        }

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

        public string DifficultyName { get; set; } = string.Empty;

        public string ExerciseTypeName { get; set; } = string.Empty;

        public int ChapterId { get; set; }

        public string ChapterName { get; set; } = string.Empty;

        public long UserCreatedId { get; set; }

        public int AccessId { get; set; }

        public bool IsExam { get; set; }

        public bool IsAccept { get; set; }

        public int NumberSuccessful { get; set; } = 0;

        public bool IsCorrect { get; set; }

    }
}
