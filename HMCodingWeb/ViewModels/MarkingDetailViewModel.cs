using HMCodingWeb.Models;

namespace HMCodingWeb.ViewModels
{
    public class MarkingDetailViewModel
    {
        public MarkingDetailViewModel(MarkingDetail markingDetail)
        {
            Id = markingDetail.Id;
            MarkingId = markingDetail.MarkingId;
            TestCaseId = markingDetail.TestCaseId;
            TestCaseIndex = markingDetail.TestCaseIndex;
            CorrectOutput = markingDetail.CorrectOutput;
            IsCorrect = markingDetail.IsCorrect;
            RunTime = markingDetail.RunTime;
            IsTimeLimitExceed = markingDetail.IsTimeLimitExceed;
            IsError = markingDetail.IsError;
            ErrorContent = markingDetail.ErrorContent;
        }


        public long Id { get; set; }

        public long MarkingId { get; set; }

        public long? TestCaseId { get; set; }

        public int? TestCaseIndex { get; set; }

        public string? CorrectOutput { get; set; }

        public bool IsCorrect { get; set; }

        public double RunTime { get; set; }

        public bool IsTimeLimitExceed { get; set; }

        public bool IsError { get; set; }

        public string? ErrorContent { get; set; }

    }

}
