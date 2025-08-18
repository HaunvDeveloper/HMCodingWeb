
using System;
using System.Collections.Generic;
namespace HMCodingWeb.ViewModels
{
    public class MessageIndexVM
    {
        public List<BoxChatListItemVM> BoxChats { get; set; }
        public ChatBoxVM CurrentChat { get; set; }
    }

    public class BoxChatListItemVM
    {
        public long BoxChatId { get; set; }
        public string BoxName { get; set; }
        public string LastMessage { get; set; }
        public DateTime LastMessageTime { get; set; } // Optional: Last message time for sorting
        public string AvatarUrl { get; set; } // Optional: URL to the avatar image
        public int UnreadCount { get; set; } // Optional: Count of unread messages
    }

    public class ChatBoxVM
    {
        public long BoxChatId { get; set; }
        public List<ChatMessageVM> Messages { get; set; }
    }

    public class ChatMessageVM
    {
        public long MessageId { get; set; } // Unique identifier for the message
        public long BoxChatId { get; set; }
        public long SenderId { get; set; }
        public string SenderName { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public string AvatarUrl { get; set; } // Optional: URL to the sender's avatar image
        public bool IsSeen { get; set; } 
    }
    public class SendMessageRequest
    {
        public long BoxChatId { get; set; }
        public string Content { get; set; } = string.Empty;
    }

    public class TypingDto
    {
        public int BoxChatId { get; set; }
        public long UserId { get; set; }
        public string UserName { get; set; }
    }
    public class MarkSeenRequest
    {
        public List<long> MessageIds { get; set; }
    }
}
