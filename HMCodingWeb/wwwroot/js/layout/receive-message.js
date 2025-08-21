function loadUnreadBoxCount() {
    fetch("/Message/GetUnreadBoxCount")
        .then(res => res.json())
        .then(count => {
            const badge = document.getElementById("unread-badge");
            if (count > 0) {
                badge.textContent = count;
                badge.style.display = "inline-block";
            } else {
                badge.style.display = "none";
            }
        })
        .catch(err => console.error("❌ Load unread box count failed:", err));
}

const connectionChatReceive = new signalR.HubConnectionBuilder()
    .withUrl("/chatHub")
    .withAutomaticReconnect()
    .build();

const messageNotificationAudio = new Audio('/assets/audios/notification.mp3'); // đường dẫn đến file mp3/wav

connectionChatReceive.start().then(async function () {
    console.log("SignalR Connected");
}).catch(err => {
    console.error("SignalR connectionChat Error: ", err);
});

connectionChatReceive.on("ReceiveMessage", (message) => {
    if (message.senderId != currentUserId) {
        messageNotificationAudio.play().catch(err => console.warn("Autoplay bị chặn:", err));
        loadUnreadBoxCount();
    }
});

loadUnreadBoxCount();
