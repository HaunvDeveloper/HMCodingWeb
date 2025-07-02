namespace HMCodingWeb.ViewModels
{
    public class JoinMeetingViewModel
    {
        public Guid MeetingId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsPrivate { get; set; }
        public long HostUserId { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }

}
