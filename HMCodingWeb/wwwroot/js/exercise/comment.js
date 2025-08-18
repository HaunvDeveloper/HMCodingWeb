
class ExerciseCommentHandler {
    constructor(exerciseId, currentUserId) {
        this.exerciseId = exerciseId;
        this.currentUserId = currentUserId;
        this.init();
    }
    init() {
        // Khởi tạo CKEditor
        CKEDITOR.replace('comment-content', {
            height: 150,
            toolbar: [
                ['Bold', 'Italic', 'Underline'],
                ['Link', 'Unlink'],
                ['NumberedList', 'BulletedList'],
                ['Undo', 'Redo']
            ]
        });
        this.removeCKENotification();
        // Lấy dữ liệu bình luận
        this.fetchCommentData();
        // Thêm sự kiện submit cho form bình luận
        this.addEventHandlers();
        // Xử lý sự kiện khi người dùng nhấn Enter trong CKEditor

    }

    fetchCommentData() {
        $.get('/exercisecomment/getcomments', { exerciseId: this.exerciseId }, (response) => {
            if (response.status) {
                $('#comment-count').text(response.comments.length);
                this.renderListComments(response.comments); 
            } else {
                $('#comments-list').html('<p>Chưa có bình luận nào.</p>');
            }
        });
    }

    renderListComments(comments) {
        var container = $('#comments-list');
        container.empty();
        if (comments.length === 0) {
            container.html('<p>Chưa có bình luận nào.</p>');
            return;
        }

        comments.forEach(comment => {
            var likeClass = comment.userLiked ? 'fa-solid fa-thumbs-up' : 'fa-regular fa-thumbs-up';
            var dislikeClass = comment.userDisliked ? 'fa-solid fa-thumbs-down' : 'fa-regular fa-thumbs-down';
            var btnRemove = `<button class="btn remove-cmt me-2" data-comment-id="${comment.id}"><i class="fa-solid fa-trash" style="color: #ff0000;"></i> Xóa</button>`;
            var commentHtml = `
                    <div class="comment-item mb-3">
                        <div class="comment-header">
                            <b>
                                <a href="/userinfo/details/${comment.userId}">${comment.username}</a>
                                ${comment.answerToUserId != null ? ` đã trả lời <a class="text-danger" href="/userinfo/details/${comment.answerToUserId}">${comment.answerToUser}</a>` : ''}
                            </b>
                            <span class="text-muted"> - ${new Date(comment.createdDate).toLocaleString('vi-VN')}</span>

                        </div>
                        <hr/>
                        <div class="comment-content">${comment.content}</div>

                        <div class="comment-actions mt-3">
                            <button class="btn  btn reply-btn me-3" data-comment-id="${comment.id}">Trả lời</button>
                            <button class="btn  like-btn me-2" data-comment-id="${comment.id}">
                                <i class="${likeClass}"></i> (<span class="like-count">${comment.likeCount}</span>)
                            </button>
                            <button class="btn  dislike-btn me-2" data-comment-id="${comment.id}">
                                <i class="${dislikeClass}"></i> (<span class="dislike-count">${comment.dislikeCount}</span>)
                            </button>
                            ${comment.canDelete ? btnRemove : ''}
                        </div>
                    </div>
                `;

            container.append(commentHtml);

        });
    }

    addEventHandlers() {
        this.addFormSubmitHandler();
        this.addSendCommentHandler();
        this.addCancelReplyHandler();
        this.addInteractionHandlers();
        this.addDeleteCommentHandler();

    }


    addFormSubmitHandler() {
        // Xử lý form bình luận qua AJAX
        $('#comment-form').submit((e) => {
            e.preventDefault();
            var formData = new FormData(e.target);
            formData.set('Content', CKEDITOR.instances['comment-content'].getData());
            formData.set('ExerciseId', this.exerciseId);

            $('#btn-send-comment').prop('disabled', true).text('Đang gửi...');
            $.ajax({
                url: '/exercisecomment/addcomment',
                type: 'POST',
                data: formData,
                processData: false,
                contentType: false,
                success: (response) => { // ✅ arrow function
                    $('#btn-send-comment').prop('disabled', false).text('Gửi bình luận');
                    if (response.status) {
                        this.fetchCommentData(); // giờ chạy OK
                        CKEDITOR.instances['comment-content'].setData('');
                        $('#answer-to-cmt-id').val('');
                        $('#cancel-reply').addClass('d-none');
                    } else {
                        alert(response.message || 'Có lỗi xảy ra.');
                    }
                },
                error: () => { // cũng nên dùng arrow function cho đồng bộ
                    $('#btn-send-comment').prop('disabled', false).text('Gửi bình luận');
                    alert('Lỗi kết nối server.');
                }
            });
        });
    }


    addSendCommentHandler() {
        // Xử lý nút trả lời
        $(document).on('click', '.reply-btn', function () {
            var commentId = $(this).data('comment-id');
            $('#answer-to-cmt-id').val(commentId);
            $('#cancel-reply').removeClass('d-none');
            $('#comment-content').focus();
            CKEDITOR.instances['comment-content'].setData('');
            $('html, body').animate({
                scrollTop: $('#comment-form').offset().top
            }, 500);
        });
    }

    addCancelReplyHandler() {
        // Hủy trả lời
        $('#cancel-reply').click(function () {
            $('#answer-to-cmt-id').val('');
            $(this).addClass('d-none');
            CKEDITOR.instances['comment-content'].setData('');
        });
    }

    addInteractionHandlers() {
        // Gán sự kiện
        $(document).on('click', '.like-btn', (e) => {
            this.toggleInteractReaction($(e.currentTarget), 'like');
        });

        $(document).on('click', '.dislike-btn', (e) => {
            this.toggleInteractReaction($(e.currentTarget), 'dislike');
        });
    }

    toggleInteractReaction(btn, type) {
        const commentId = btn.data('comment-id');
        const icon = btn.find('i');
        const countEl = btn.find(`.${type}-count`);
        let count = parseInt(countEl.text()) || 0;

        const isActive = icon.hasClass('fa-solid');
        const otherType = type === 'like' ? 'dislike' : 'like';
        const otherBtn = btn.siblings(`.${otherType}-btn`);
        const otherIcon = otherBtn.find('i');
        const otherCountEl = otherBtn.find(`.${otherType}-count`);
        let otherCount = parseInt(otherCountEl.text()) || 0;

        if (isActive) {
            // Nếu đang active → bỏ chọn
            icon.removeClass('fa-solid').addClass('fa-regular');
            count = Math.max(0, count - 1);
        } else {
            // Nếu đang inactive → bật
            icon.removeClass('fa-regular').addClass('fa-solid');
            count++;
            // Nếu nút đối diện đang bật → tắt đi
            if (otherIcon.hasClass('fa-solid')) {
                otherIcon.removeClass('fa-solid').addClass('fa-regular');
                otherCount = Math.max(0, otherCount - 1);
                otherCountEl.text(otherCount);
            }
        }

        countEl.text(count);

        // Gửi lên server
        $.post(`/exercisecomment/${type}comment`, { commentId }, (response) => {
            if (!response.status) {
                alert(response.message || 'Có lỗi xảy ra.');
                // rollback (đơn giản: reload dữ liệu)
                this.fetchCommentData();
            }
        });
    }

    addDeleteCommentHandler() {
        // Xử lý xóa bình luận
        $(document).on('click', '.remove-cmt', async (e) => {
            var commentId = $(e.target).data('comment-id');
            if (await confirmAction("Bạn có chắc muốn xóa bình luận này?")) {
                $.post('/exercisecomment/deletecomment', { commentId: commentId }, (response) => {
                    if (response.status) {
                        this.fetchCommentData();
                    } else {
                        alert(response.message || 'Có lỗi xảy ra.');
                    }
                });
            }
        });
    }

    // Hàm xóa thông báo CKEditor
    removeCKENotification() {
        try {
            $('.cke_notifications_area').remove();

            if ($('.cke_notifications_area')) {
                setTimeout(this.removeCKENotification, 200);
            }
        }
        catch (e) {
            setTimeout(this.removeCKENotification, 200);
        }
    }
}

