let currentBoxId = parseInt(new URLSearchParams(window.location.search).get('boxChatId')) || null;

// Kết nối SignalR
const connectionChat = new signalR.HubConnectionBuilder()
    .withUrl("/chatHub")
    .withAutomaticReconnect()
    .build();

connectionChat.start().then(async function () {
    console.log("SignalR Connected");
    await loadBoxChats();
    if (currentBoxId) {
        loadMessages(currentBoxId);
    } else {
        $('#chat-container').addClass("d-opacity-0"); // ẩn nếu không có box chat
        $('#chat-sidebar').collapse('show'); 
    }
}).catch(err => {
    console.error("SignalR connectionChat Error: ", err);
});

// Nhận tin nhắn mới
connectionChat.on("ReceiveMessage", (message) => {
    //console.log("Received message: ", message);

    if (message.boxChatId && message.boxChatId == currentBoxId) {
        $('#typing-indicator').hide();
        addNewMessage(message);
    }

    // Update last message bên list
    const chatItem = document.querySelector(`#chat-list .chat-item[data-id="${message.boxChatId}"]`);
    if (chatItem) {
        chatItem.querySelector('.last-message').textContent =
            message.content.substring(0, 30) + (message.content.length > 30 ? '...' : '');

        if (message.boxChatId != currentBoxId) 
        { 
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
            li.attr('data-is-group', chat.isGroup ? 'true' : 'false'); 
            li.attr('data-participants', chat.participants.join(',')); // Lưu danh sách người tham gia
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
});

function loadMessages(boxChatId) {
    currentBoxId = boxChatId;

    fetch(`/message/getmessages?boxChatId=${boxChatId}`)
        .then(res => {
            if (!res.ok) throw new Error(`HTTP error! Status: ${res.status}`);
            return res.json();
        })
        .then(data => {
            renderHeader(boxChatId);
            const container = document.getElementById('message-container');
            container.innerHTML = '';

            let lastTimestamp = null;
            let lastSenderId = null;
            let groupBuffer = []; // tạm giữ nhóm tin nhắn

            function flushGroup() {
                if (groupBuffer.length === 0) return;

                // render từng msg trong nhóm
                groupBuffer.forEach((msg, idx) => {
                    const msgTime = new Date(msg.createdAt);
                    let showName = idx === 0; // chỉ hiện tên ở msg đầu
                    let showAvatar = idx === groupBuffer.length - 1; // chỉ hiện avatar ở msg cuối

                    const div = document.createElement('div');
                    div.className = `message ${msg.senderId == currentUserId ? 'current-user' : ''}`;
                    div.setAttribute('data-id', msg.messageId);
                    div.setAttribute('data-is-seen', msg.isSeen ? 'true' : 'false');
                    div.setAttribute('data-time', msg.createdAt);
                    div.setAttribute('data-sender', msg.senderId);

                    if (groupBuffer.length === 1) {
                        div.classList.add("single-in-group");
                    } else if (idx === 0) {
                        div.classList.add("first-in-group");
                    } else if (idx === groupBuffer.length - 1) {
                        div.classList.add("last-in-group");
                    } else {
                        div.classList.add("middle-in-group");
                    }

                    let contentHtml = textToHtml(msg.content);
                    contentHtml = linkify(contentHtml);

                    div.innerHTML = `
                        ${showAvatar ? `<img src="${msg.avatarUrl}" alt="Avatar" class="avatar"
                             />` : `<div class="avatar"></div>`}
                        <div title="${msgTime.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}">
                            ${showName ? `<div class="sender">${msg.senderName}</div>` : ''}
                            <div class="content">${contentHtml}</div>
                        </div>`;
                    container.appendChild(div);
                });

                groupBuffer = []; // reset nhóm
            }

            data.forEach(msg => {
                const msgTime = new Date(msg.createdAt);

                // separator 15 phút
                if (!lastTimestamp || (msgTime - lastTimestamp) > 15 * 60 * 1000) {
                    flushGroup(); // đóng nhóm cũ
                    addTimeDivider(container, msgTime);
                    
                }

                // Nếu cùng sender và chưa qua 15p → tiếp tục nhóm
                if (lastSenderId !== null &&
                    msg.senderId === lastSenderId &&
                    lastTimestamp && (msgTime - lastTimestamp) <= 15 * 60 * 1000) {
                    groupBuffer.push(msg);
                } else {
                    // flush nhóm cũ nếu khác sender
                    flushGroup();
                    groupBuffer.push(msg);
                }

                lastTimestamp = msgTime;
                lastSenderId = msg.senderId;
            });

            // Flush nhóm cuối cùng
            flushGroup();

            container.scrollTop = container.scrollHeight;
            $('#message-input').focus();
            addObserverToMessages();
        })
        .catch(err => console.error("LoadMessages Error: ", err));
}

function isSameDay(d1, d2) {
    return d1.getFullYear() === d2.getFullYear() &&
        d1.getMonth() === d2.getMonth() &&
        d1.getDate() === d2.getDate();
}

function addNewMessage(msg) {
    const container = document.getElementById('message-container');
    const lastMsg = container.querySelector('.message:last-of-type');
    const lastDivider = container.querySelector('.time-divider:last-of-type');

    let lastTimestamp = null;
    let lastSenderId = null;

    if (lastMsg) {
        lastTimestamp = new Date(lastMsg.getAttribute('data-time'));
        lastSenderId = lastMsg.classList.contains('current-user')
            ? currentUserId
            : lastMsg.getAttribute('data-sender');
    }

    const msgTime = new Date(msg.createdAt);

    // ========== Check 15p divider ==========
    let needDivider = false;
    if (!lastTimestamp || (msgTime - lastTimestamp) > 15 * 60 * 1000) {
        needDivider = true;
    } else if (lastDivider) {
        // Nếu qua ngày → cũng cần divider
        const dividerTime = new Date(lastDivider.getAttribute('data-time'));
        if (!isSameDay(dividerTime, msgTime)) {
            needDivider = true;
        }
    }

    if (needDivider) {
        addTimeDivider(container, msgTime);
    }

    // ========== Kiểm tra grouping ==========
    let isGrouped = lastSenderId &&
        msg.senderId == lastSenderId &&
        lastTimestamp && (msgTime - lastTimestamp) <= 15 * 60 * 1000;

    // Nếu grouped → chỉnh lại avatar của tin trước đó thành rỗng
    if (isGrouped && lastMsg) {
        const avatarEl = lastMsg.querySelector('.avatar');
        if (avatarEl) {
            avatarEl.remove();
            let newAvatar = document.createElement('div');
            newAvatar.className = 'avatar';
            lastMsg.insertBefore(newAvatar, lastMsg.firstChild); // Thêm vào đầu tiên
        }
    }

    // ========== Tạo element cho message ==========
    const div = document.createElement('div');
    div.className = `message ${msg.senderId == currentUserId ? 'current-user' : ''}`;
    div.setAttribute('data-id', msg.messageId);
    div.setAttribute('data-sender', msg.senderId);
    div.setAttribute('data-is-seen', msg.isSeen ? 'true' : 'false');
    div.setAttribute('data-time', msg.createdAt);
    div.setAttribute('data-is-seen', msg.senderId == currentBoxId ? "true" : "false");

    // === Xác định bo góc ===
    if (!isGrouped) {
        div.classList.add("single-in-group");
    } else {
        // Nếu tin trước đó là first → sửa nó thành middle
        if (lastMsg && (lastMsg.classList.contains("first-in-group") || lastMsg.classList.contains("last-in-group"))) {
            lastMsg.classList.remove("first-in-group");
            lastMsg.classList.remove("last-in-group");
            lastMsg.classList.add("middle-in-group");
        }
        div.classList.add("last-in-group");
    }


    let contentHtml = textToHtml(msg.content);
    contentHtml = linkify(contentHtml);

    // Nếu là grouped → ẩn sender name
    let showName = !isGrouped;
    // Avatar chỉ hiện khi không grouped (tin đầu) hoặc là tin cuối cùng trong nhóm
    let showAvatar = true;

    div.innerHTML = `
        ${showAvatar ? `<img src="${msg.avatarUrl}" alt="Avatar" class="avatar"
            onerror="this.src='/assets/images/avartardefault.jpg'" />` : `<div class="avatar"></div>`}
        <div title="${msgTime.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}">
            ${showName ? `<div class="sender">${msg.senderName}</div>` : ''}
            <div class="content">${contentHtml}</div>
        </div>`;

    container.appendChild(div);

    // Scroll xuống cuối
    container.scrollTop = container.scrollHeight;
}


// Hàm thêm mốc thời gian
function addTimeDivider(container, date) {
    const divider = document.createElement("div");
    divider.className = "time-separator";

    let text = "";
    const now = new Date();
    if (date.toDateString() === now.toDateString()) {
        text = date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    } else {
        text = date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) +
            " " + date.toLocaleDateString();
    }

    divider.innerText = text;
    container.appendChild(divider);
}


function renderHeader(boxChatId) {
    // Add data-id to #chat-header
    $('#chat-header').attr('data-id', boxChatId);
    var itemBoxChat = $(`#chat-list .chat-item[data-id="${boxChatId}"]`);
    // Add avatar to #chat-header #chat-avatar
    const chatAvatar = $(itemBoxChat).find('img').attr("src");
    $('#chat-header #chat-avatar').attr("src", chatAvatar);
    // Cập nhật tên hội thoại
    const chatName = $(itemBoxChat).find('.chat-name').text();
    $('#chat-header #chat-username').text(chatName);

    // Cập nhật trạng thái
    let isOnline = listUserOnline.includes(chatName);
    $('#chat-header #chat-status').html(isOnline ? '<span class="text-success">Đang online</span>' : 'Đang offline');
    var groupItem = itemBoxChat.parent();
    var isGroup = groupItem.data('is-group') === true || groupItem.data('is-group') === 'true';
    if (!isGroup) {
        $("#chat-username, #chat-status, #chat-avatar").off('click').on('click', function () {
            // redirect đến trang profile của người dùng target="_blank""
            let userId = groupItem.attr('data-participants'); // lấy người đầu tiên trong danh sách
            window.open(`/userinfo/details/${userId}`, '_blank');
        });
    }
    

    //Close sidebar nếu đang mở
    if ($('#chat-sidebar').hasClass('show')) {
        $('#chat-sidebar').collapse('hide'); // dùng collapse API của Bootstrap
    }

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
                if (!res.ok) {
                    (`HTTP error! Status: ${res.status}`);
                    btnSend.prop('disabled', false);
                }
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
    let quantity = ids.length;
    // remove from badge
    const badge = document.querySelector(`#chat-list .chat-item[data-id="${currentBoxId}"] .badge`);
    if (badge) {
        let count = parseInt(badge.textContent) || 0;
        count -= quantity;
        if (count <= 0) {
            badge.remove();
        } else {
            badge.textContent = count;
        }
    }


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
