namespace HMCodingWeb.ViewModels
{
    public class GenerateOutputViewModel
    {
        public long UserId { get; set; }    

        public string Error { get; set; } = string.Empty;

        public bool IsError { get; set; } = false;

        public int ProgramLanguageId { get; set; }

        public string? SourceCode { get; set; }

        public int? TimeLimit { get; set; } = 5;


        public List<GenerateOutputDetailViewModel> SampleOutputs { get; set; } = new List<GenerateOutputDetailViewModel>();
    }

    public class GenerateOutputDetailViewModel
    {
        public int SampleIndex { get; set; }
        public string? Input { get; set; }
        public string? Output { get; set; }
        public string? Error { get; set; }
        public bool IsError { get; set; } = false;
        public string? FileName { get; set; }
        public float? RunTime { get; set; } = 0;
    }


}
