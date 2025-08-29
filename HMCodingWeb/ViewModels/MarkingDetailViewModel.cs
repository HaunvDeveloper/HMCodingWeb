using HMCodingWeb.Models;

namespace HMCodingWeb.ViewModels
{
    public class MarkingDetailViewModel
    {
        public MarkingDetailViewModel(MarkingDetail markingDetail, int totalTestcase, string sessionCode)
        {
            Id = markingDetail.Id;
            MarkingId = markingDetail.MarkingId;
            TestCaseId = markingDetail.TestCaseId;
            TestCaseIndex = markingDetail.TestCaseIndex;
            IsCorrect = markingDetail.IsCorrect;
            RunTime = markingDetail.RunTime;
            IsTimeLimitExceed = markingDetail.IsTimeLimitExceed;
            IsError = markingDetail.IsError;
            ErrorContent = markingDetail.ErrorContent;
            TotalTestcase = totalTestcase;
            SessionCode = sessionCode;
            MemoryUsed = (float)markingDetail.MemoryUsed;
            IsMemoryLimitExceed = markingDetail.IsMemoryLimitExceed;
        }


        public long Id { get; set; }

        public long MarkingId { get; set; }

        public long? TestCaseId { get; set; }

        public int? TestCaseIndex { get; set; }

        public int TotalTestcase { get; set; }

        public bool IsCorrect { get; set; }

        public double RunTime { get; set; }

        public bool IsTimeLimitExceed { get; set; }

        public float MemoryUsed { get; set; }

        public bool IsMemoryLimitExceed { get; set; }

        public bool IsError { get; set; }

        public string? ErrorContent { get; set; }

        public string SessionCode { get; set; }

    }

}
