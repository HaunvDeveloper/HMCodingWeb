namespace HMCodingWeb.ViewModels
{
    public class RoomViewModel
    {
        public Guid MeetingId { get; set; }
        public long UserId { get; set; }
        public bool MuteMic { get; set; }
        public bool DisableCamera { get; set; }
        public string MeetingTitle { get; set; }
    }

}
