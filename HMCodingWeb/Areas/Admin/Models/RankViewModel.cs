namespace HMCodingWeb.Areas.Admin.Models
{
    public class RankViewModel
    {
        public int Id { get; set; }
        public string RankCode { get; set; } = null!;
        public string? RankName { get; set; }
        public int UserCount { get; set; }
    }

    public class EditRankViewModel
    {
        public int Id { get; set; }
        public string RankCode { get; set; } = null!;
        public string? RankName { get; set; }
        public int MinLimitPoint { get; set; }
        public int MaxLimitPoint { get; set; }
        public string? Description { get; set; }

        public List<DifficultyPrerequisiteDto> DifficultyPrerequisites { get; set; } = new List<DifficultyPrerequisiteDto>();
    }

    public class DifficultyPrerequisiteDto
    {
        public int DifficultyId { get; set; }
        public string DifficultyName { get; set; } = null!;
        public int AtLeast { get; set; }
    }


}
