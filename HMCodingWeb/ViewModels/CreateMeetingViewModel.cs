using System.ComponentModel.DataAnnotations;

namespace HMCodingWeb.ViewModels
{
    public class CreateMeetingViewModel
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        public DateTime? StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public bool IsPrivate { get; set; }

        public bool IsRequireToJoin { get; set; }
    }

}
