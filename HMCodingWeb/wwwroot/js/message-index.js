let currentBoxId = parseInt(new URLSearchParams(window.location.search).get('boxChatId')) || null;
// Kết nối SignalR
const connectionChat = new signalR.HubConnectionBuilder()
    .withUrl("/chatHub")
    .withAutomaticReconnect()
    .build();

connectionChat.start().then(async function() {
    console.log("SignalR Connected");
    await loadBoxChats();
    if (currentBoxId) {
        loadMessages(currentBoxId);
    }
}).catch(err => {
    console.error("SignalR connectionChat Error: ", err);
});

// Nhận tin nhắn mới
connectionChat.on("ReceiveMessage", (message) => {
    //console.log("Received message: ", message);

    if (message.boxChatId && message.boxChatId == currentBoxId) {
        $('#typing-indicator').hide();
        const container = document.getElementById('message-container');
        const div = document.createElement('div');
        div.setAttribute('data-id', message.messageId);
        div.setAttribute('data-is-seen', message.senderId == currentUserId ? 'true' : 'false'); // Thêm thuộc tính isSeen
        div.className = `message ${message.senderId == currentUserId ? 'current-user' : ''}`;
        div.innerHTML = `
        <img src="${message.avatarUrl || '/assets/images/avartardefault.jpg'}" alt="Avatar" class="avatar" onerror="this.src='/assets/images/avartardefault.jpg'" />
        <div>
            <div class="sender">${message.senderName}</div>
            <div class="content">${message.content}</div>
            <div class="time">${new Date(message.createdAt).toLocaleTimeString()}</div>
        </div>`;
        container.appendChild(div);
        container.scrollTop = container.scrollHeight;
    }

    // Update last message bên list
    const chatItem = document.querySelector(`#chat-list .chat-item[data-id="${message.boxChatId}"]`);
    if (chatItem) {
        chatItem.querySelector('.last-message').textContent =
            message.content.substring(0, 30) + (message.content.length > 30 ? '...' : '');

        const li = chatItem.parentElement;
        const badge = li.querySelector('.badge');
        if (badge) {
            const count = parseInt(badge.textContent) || 0;
            badge.textContent = count + 1; // Tăng số lượng tin nhắn chưa đọc
        } else {
            const newBadge = document.createElement('span');
            newBadge.className = 'badge bg-danger rounded-pill ms-2';
            newBadge.textContent = '1'; // Tin nhắn đầu tiên
            li.appendChild(newBadge);
        }
    }

    // Cập nhật danh sách hội thoại
    const chatList = $('#chat-list');
    const li = $(chatItem).closest('li');
    if (li.length) {
        li.remove();
        chatList.prepend(li); // Đưa lên đầu danh sách
    }
});


const delayTyping = 2000; // Thời gian chờ để ẩn thông báo đang nhập

// Nhận thông báo đang nhập
connectionChat.on("ReceiveTyping", (data) => {
    if (data.boxChatId == currentBoxId && data.userId != currentUserId) {
        $('#typing-indicator').text(`${data.userName} đang soạn tin...`).show();
        clearTimeout(window.typingTimeout);
        window.typingTimeout = setTimeout(() => {
            $('#typing-indicator').hide();
        }, delayTyping); // ẩn sau 2s nếu không nhận gõ mới
    }
});


let typingSentRecently = false;
const typingCooldown = 3000; // 3s không gửi lại tín hiệu

$('#message-input').on('input', function () {
    if (currentBoxId && !typingSentRecently) {
        connectionChat.invoke("Typing", {
            boxChatId: currentBoxId,
            userId: parseInt(currentUserId),
            userName: currentUserName
        }).catch(err => console.error("Typing Error:", err));

        typingSentRecently = true;
        setTimeout(() => typingSentRecently = false, typingCooldown);
    }
});




// Load danh sách hội thoại
async function loadBoxChats() {
    try {
        const data = await $.ajax({
            url: '/message/getboxchats',
            method: 'GET',
            dataType: 'json'
        });

        const list = $('#chat-list').empty();

        data.forEach(chat => {
            let haveUnread = chat.unreadCount > 0;

            const li = $(`
                <li class="list-group-item d-flex align-items-center justify-content-between ${chat.boxChatId == currentBoxId ? 'active' : ''} ${haveUnread ? 'unread' : ''}">
                    <div class="chat-item d-flex align-items-center" data-id="${chat.boxChatId}">
                        <img src="${chat.avatarUrl || '/assets/images/avartardefault.jpg'}" 
                             alt="Avatar" class="rounded-circle me-2"
                             width="40" height="40"
                             onerror="this.src='/assets/images/avartardefault.jpg'" />
                        <div class="chat-info">
                            <div class="chat-name fw-bold">${chat.boxName}</div>
                            <div class="last-message text-truncate" style="max-width:200px">
                                ${chat.lastMessage
                            ? chat.lastMessage.substring(0, 30) + (chat.lastMessage.length > 30 ? '...' : '')
                            : 'No messages yet'}
                            </div>
                        </div>
                    </div>

                    ${haveUnread
                ? `<span class="badge bg-danger rounded-pill ms-2">${chat.unreadCount}</span>`
                            : ''}
                </li>
            `);

            li.attr('data-id', chat.boxChatId);
            list.append(li);
        });


    } catch (err) {
        console.error("LoadBoxChats Error: ", err);
    }
}

$(document).on('click', '#chat-list .list-group-item', function () {
    $('#chat-list .list-group-item').removeClass('active');
    $(this).removeClass('unread').find('.badge').remove(); // clear unread style
    $(this).addClass('active');
    let boxChatId = $(this).data('id');
    loadMessages(boxChatId);
    window.history.pushState({}, '', `?boxChatId=${boxChatId}`);
})

// Load tin nhắn của box
function loadMessages(boxChatId) {
    currentBoxId = boxChatId;
    fetch(`/message/getmessages?boxChatId=${boxChatId}`)
        .then(res => {
            if (!res.ok) throw new Error(`HTTP error! Status: ${res.status}`);
            return res.json();
        })
        .then(data => {
            const container = document.getElementById('message-container');
            container.innerHTML = '';
            data.forEach(msg => {
                const div = document.createElement('div');
                div.className = `message ${msg.senderId == currentUserId ? 'current-user' : ''}`;
                div.setAttribute('data-id', msg.messageId);
                div.setAttribute('data-is-seen', msg.isSeen ? 'true' : 'false'); 

                let contentHtml = textToHtml(msg.content);
                // Chuyển đổi link thành clickable
                contentHtml = linkify(contentHtml);

                div.innerHTML = `
                    <img src="${msg.avatarUrl}" alt="Avatar" class="avatar" onerror="this.src='/assets/images/avartardefault.jpg'" />
                    <div>
                        <div class="sender">${msg.senderName}</div>
                        <div class="content">${contentHtml}</div>
                        <div class="time">${new Date(msg.createdAt).toLocaleTimeString()}</div>
                    </div>`;
                container.appendChild(div);
            });
            container.scrollTop = container.scrollHeight;
            $('#message-input').focus();
            addObserverToMessages();
        })
        .catch(err => console.error("LoadMessages Error: ", err));
}

// Gửi tin nhắn
$('#send-form').on('submit', function (e) {
    e.preventDefault();
    const messageInput = $('#message-input');
    const btnSend = $('#btn-send');
    const content = messageInput.val();
    if (content && currentBoxId) {
        //disable form to prevent multiple submissions
        messageInput.prop('disabled', true);
        btnSend.prop('disabled', true);

        fetch('/message/send', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                boxChatId: currentBoxId,
                content: content
            })
        })
            .then(res => {
                if (!res.ok) throw new Error(`HTTP error! Status: ${res.status}`);
                // re-enable input field after sending message
                messageInput.prop('disabled', false);
                btnSend.prop('disabled', false);
                return res.json();
            })
            .then(() => {
                messageInput.val('');
                messageInput.focus();

            })
            .catch(err => console.error("SendMessage Error: ", err));
    }
});

function textToHtml(str) {
    if (!str) return "";
    return str
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/\n/g, "<br/>")       // xuống dòng
        .replace(/\t/g, "&emsp;&emsp;")// tab = 2 khoảng cách tab
        .replace(/ {2}/g, "&nbsp;&nbsp;"); // giữ khoảng trắng liên tiếp
}


function linkify(text) {
    if (!text) return "";

    // Regex bắt URL (http, https, www)
    const urlPattern = /((https?:\/\/|www\.)[^\s<]+)/gi;

    return text.replace(urlPattern, (url) => {
        let href = url;
        // Nếu bắt đầu bằng www thì thêm http://
        if (!href.match(/^https?:\/\//i)) {
            href = 'http://' + href;
        }
        return `<a href="${href}" target="_blank" rel="noopener noreferrer">${url}</a>`;
    });
}


function addObserverToMessages() {
    // Gắn observer cho mọi message hiện có
    document.querySelectorAll("#message-container .message").forEach(msg => {
        observer.observe(msg);
    });
}

const messageContainer = document.getElementById("message-container");

let seenQueue = new Set();
let seenTimeout = null;

function flushSeenMessages() {
    if (seenQueue.size === 0) return;
    const ids = Array.from(seenQueue);
    seenQueue.clear();

    console.log("🔼 Sending seen messages:", ids);

    fetch("/message/markseen", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ messageIds: ids })
    }).catch(err => console.error("Seen update failed:", err));
}

const observer = new IntersectionObserver((entries) => {
    entries.forEach(entry => {
        if (entry.isIntersecting) {
            const msgEl = entry.target;
            const messageId = msgEl.dataset.id;
            const isSeen = msgEl.dataset.isSeen === "true";

            //console.log("👀 Visible:", messageId, "isSeen?", isSeen);

            if (!isSeen && !msgEl.classList.contains("current-user")) {
                msgEl.dataset.isSeen = "true";

                seenQueue.add(parseInt(messageId));
                if (!seenTimeout) {
                    seenTimeout = setTimeout(() => {
                        flushSeenMessages();
                        seenTimeout = null;
                    }, 500);
                }
            }
        }
    });
}, {
    root: messageContainer,  // rất quan trọng
    threshold: 0.2           // dễ test hơn, chỉ cần 20% lọt vào viewport
});
