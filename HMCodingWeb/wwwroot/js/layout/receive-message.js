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

// Xin quyền thông báo khi load trang
if ("Notification" in window && Notification.permission !== "granted") {
    Notification.requestPermission();
}

connectionChatReceive.start().then(async function () {
    console.log("SignalR Connected");
}).catch(err => {
    console.error("SignalR connectionChat Error: ", err);
});

connectionChatReceive.on("ReceiveMessage", (message) => {
    if (message.senderId != currentUserId) {
        // 🔔 Phát âm thanh
        messageNotificationAudio.play().catch(err => console.warn("Autoplay bị chặn:", err));
        loadUnreadBoxCount();

        // 🚦 Xin quyền notification nếu chưa có
        if ("Notification" in window && Notification.permission !== "granted") {
            Notification.requestPermission();
        }

        // 🖥️ Hiện thông báo trình duyệt
        if (Notification.permission === "granted") {
            const notification = new Notification("Tin nhắn mới từ " + message.senderName, {
                body: message.content.length > 50
                    ? message.content.substring(0, 50) + "..."
                    : message.content,
                icon: message.avatarUrl || "/assets/images/avartardefault.jpg"
            });

            // Khi click vào notification
            notification.onclick = function () {
                window.focus();
                window.location.href = `/message?boxChatId=${message.boxChatId}`;
            };
        }

        // 📢 Hiện toast bằng SweetAlert2
        Swal.fire({
            toast: true,
            position: 'top-end',
            icon: 'info',
            title: `<strong>${message.senderName}</strong>`,
            html: message.content.length > 100
                ? message.content.substring(0, 100) + "..."
                : message.content,
            showConfirmButton: false,
            showCloseButton: true,   // 👉 thêm nút tắt (dấu X)
            timer: 2000,
            timerProgressBar: true,
            didOpen: (toast) => {
                toast.addEventListener('mouseenter', Swal.stopTimer);
                toast.addEventListener('mouseleave', Swal.resumeTimer);

                toast.addEventListener('click', (e) => {
                    // Nếu click vào nút close thì bỏ qua
                    if (e.target.classList.contains('swal2-close')) {
                        return;
                    }
                    window.open(`/message?boxChatId=${message.boxChatId}`, '_blank');
                });
            }
        });

    }
});


loadUnreadBoxCount();
