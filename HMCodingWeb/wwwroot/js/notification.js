$(document).ready(function () {
    var startNotifications = 0;
    var pageSize = 5;
    var totalDisplay = 0;
    function loadNotifications(start = 0) {
        if (start == 0) {
            $('#notificationList').empty(); // Clear previous notifications
            $('#unseenCount').hide(); // Hide unseen count initially
            startNotifications = 0; // Reset start index
            totalDisplay = 0; // Reset total display count
        }
        $.ajax({
            url: '/Notification/GetUserNotifications',
            data: {start},
            type: 'GET',
            success: function (response) {
                var notifications = response.notifications;
                var unseenCount = response.unseenCount;
                var notificationList = $('#notificationList');
                if (unseenCount > 0) {
                    $('#unseenCount').text(unseenCount).show();
                }

                if (notifications.length === 0 && start === 0) {
                    notificationList.html('<div class="notification-item">Không có thông báo.</div>');
                } else {
                    notifications.forEach(function (n) {
                        var notificationHtml = `
                                        <div class="notification-item ${n.isSeen ? '' : 'unseen'} ${n.isImportant ? 'important' : ''}" data-notification-id="${n.id}">
                                            <div class="notification-title">${n.title}</div>
                                            <div class="notification-meta">
                                                ${n.createdByUsername ? 'Từ: ' + n.createdByUsername + ' - ' : ''}
                                                ${new Date(n.createdAt).toLocaleString('vi-VN')}
                                            </div>
                                        </div>`;
                        notificationList.append(notificationHtml);
                        startNotifications++;
                    });
                    totalDisplay += pageSize;
                    

                    if (startNotifications >= totalDisplay) {
                        notificationList.append(`
                        
                            <button id="loadMore" type="button" class="btn btn-link text-center text-success">Tải thêm</button>
                        `);
                    } else {
                        $('#loadMore').remove();
                    }
                }
            },
            error: function () {
                $('#notificationList').html('<div class="notification-item">Lỗi khi tải thông báo.</div>');
            }
        });
    }
    loadNotifications(startNotifications);

    $(document).on('click', '#loadMore', function () {
        loadNotifications(startNotifications);
        $('#notificationDropdown').dropdown('show');
       
    });

    // Show modal with full notification content
    $(document).on('click', '.notification-item', function () {
        var notificationId = $(this).data('notification-id');
        var isUnseen = $(this).hasClass('unseen');

        // Fetch full notification content
        $.ajax({
            url: '/Notification/GetNotificationById',
            type: 'GET',
            data: { notificationId: notificationId },
            success: function (response) {
                if (response.success) {
                    var n = response.notification;
                    $('#notificationModalLabel').text(n.title);
                    $('#notificationMessage').html(n.message);
                    $('#notificationMeta').text(
                        (n.createdByUsername ? 'Từ: ' + n.createdByUsername + ' - ' : '') +
                        new Date(n.createdAt).toLocaleString('vi-VN')
                    );

                    // Show modal
                    var modal = new bootstrap.Modal(document.getElementById('notificationModal'));
                    modal.show();

                    // Mark as seen if unseen
                    if (isUnseen) {
                        $.ajax({
                            url: '/Notification/MarkAsSeen',
                            type: 'POST',
                            data: { notificationId: notificationId },
                            success: function (response) {
                                if (response.success) {
                                    $(`[data-notification-id="${notificationId}"]`).removeClass('unseen');
                                    var unseenCount = parseInt($('#unseenCount').text()) - 1;
                                    if (unseenCount <= 0) {
                                        $('#unseenCount').hide();
                                    } else {
                                        $('#unseenCount').text(unseenCount);
                                    }
                                }
                            }
                        });
                    }
                } else {
                    alert(response.message);
                }
            },
            error: function () {
                alert('Lỗi khi tải nội dung thông báo.');
            }
        });
    });

    connection.on("ReceiveNotification", function (notification) {
        loadNotifications(0); // Reload notifications
    });

});