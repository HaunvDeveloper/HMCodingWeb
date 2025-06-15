namespace HMCodingWeb.ViewModels
{
    public class RunProcessViewModel
    {
        public long UserId { get; set; }
        public int ProgramLanguageId { get; set; }
        public string? FileName { get; set; }
        public string? SourceCode { get; set; }
        public string? Input { get; set; }
        public string? Output { get; set; }
        public string? Error { get; set; }
        public bool? IsError { get; set; }
        public string? InputFile { get; set; }
        public string? OutputFile { get; set; }
        public int? TimeLimit { get; set; }
        public float? RunTime { get; set; }
    }
}
