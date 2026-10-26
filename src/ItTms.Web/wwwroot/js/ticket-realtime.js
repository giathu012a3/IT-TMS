/**
 * ticket-realtime.js
 * Quản lý trao đổi trực tiếp không reload trang và đồng bộ trạng thái ticket thời gian thực qua SignalR WebSocket.
 */
document.addEventListener("DOMContentLoaded", function () {
    const container = document.getElementById("ticket-detail-container");
    if (!container) return;

    const ticketId = parseInt(container.getAttribute("data-ticket-id"), 10);
    if (!ticketId) return;

    const currentUserId = parseInt(container.getAttribute("data-current-user-id"), 10);
    const commentList = document.getElementById("comment-stream-list");
    const commentForm = document.getElementById("comment-submit-form");
    const commentInput = document.getElementById("comment-content-input");
    const commentBtn = document.getElementById("comment-submit-btn");
    const statusBadge = document.getElementById("ticket-status-badge");
    const commentLockNotice = document.getElementById("comment-locked-notice");

    function waitForConnection(callback) {
        if (window.ticketHubConnection && window.ticketHubConnection.state === signalR.HubConnectionState.Connected) {
            callback(window.ticketHubConnection);
        } else {
            setTimeout(() => waitForConnection(callback), 200);
        }
    }

    waitForConnection(function (connection) {
        // 1. Gia nhập nhóm thảo luận của ticket này
        connection.invoke("JoinTicketGroup", ticketId)
            .then(() => console.log(`[SIGNALR] Đã gia nhập phòng ticket_${ticketId}`))
            .catch(err => console.error("[SIGNALR] Không thể gia nhập phòng ticket:", err));

        // Tự động gia nhập lại nếu kết nối rớt và kết nối lại
        connection.onreconnected(function () {
            connection.invoke("JoinTicketGroup", ticketId);
        });

        // 2. Lắng nghe bình luận thời gian thực (ReceiveComment)
        connection.on("ReceiveComment", function (comment) {
            const commentTicketId = comment.ticketId !== undefined ? comment.ticketId : comment.TicketId;
            if (commentTicketId !== ticketId) return;

            appendCommentToView(comment);
            scrollToBottom();
        });

        // 3. Lắng nghe thay đổi trạng thái (TicketStatusChanged - TC-08 & TC-09)
        connection.on("TicketStatusChanged", function (data) {
            const dataTicketId = data.ticketId !== undefined ? data.ticketId : data.TicketId;
            if (dataTicketId !== ticketId) return;

            console.log("[SIGNALR] Trạng thái ticket cập nhật:", data);

            if (statusBadge) {
                statusBadge.className = "badge " + (data.colorClass || data.ColorClass || "");
                statusBadge.textContent = data.newStatusLabel || data.NewStatusLabel || "";
            }

            // Nếu trạng thái mới không cho phép bình luận (Resolved, Closed, Rejected) -> Khóa ô chat ngay lập tức!
            const canComment = data.canComment !== undefined ? data.canComment : data.CanComment;
            const newStatusLabel = data.newStatusLabel || data.NewStatusLabel || "";
            if (!canComment) {
                if (commentForm) commentForm.classList.add("d-none");
                if (commentLockNotice) {
                    commentLockNotice.classList.remove("d-none");
                    commentLockNotice.innerHTML = `
                        <div class="alert alert-warning py-2 mb-0">
                            <svg class="bi me-1" width="16" height="16"><use href="#lock-fill"></use></svg> Yêu cầu hiện ở trạng thái <strong>${escapeHtml(newStatusLabel)}</strong>. Chức năng gửi bình luận đã bị khóa.
                        </div>`;
                }
            } else {
                if (commentForm) commentForm.classList.remove("d-none");
                if (commentLockNotice) commentLockNotice.classList.add("d-none");
            }
        });
    });

    // 4. Shortcut phím Enter: Nhấn Enter gửi ngay, Shift + Enter xuống dòng, tương thích IME tiếng Việt
    if (commentInput && commentForm) {
        commentInput.addEventListener("keydown", function (e) {
            if (e.key === "Enter" && !e.shiftKey && !e.isComposing) {
                e.preventDefault();
                const text = commentInput.value.trim();
                if (!text) return;

                if (typeof commentForm.requestSubmit === "function") {
                    commentForm.requestSubmit();
                } else {
                    commentForm.dispatchEvent(new Event("submit", { cancelable: true }));
                }
            }
        });
    }

    // 5. Xử lý gửi bình luận qua AJAX Form
    if (commentForm) {
        commentForm.addEventListener("submit", async function (e) {
            e.preventDefault();

            if (commentBtn && commentBtn.disabled) return;

            const content = commentInput ? commentInput.value.trim() : "";
            if (!content) return;

            const isInternalCheckbox = document.getElementById("is-internal-checkbox");
            const isInternal = isInternalCheckbox ? isInternalCheckbox.checked : false;

            if (commentBtn) {
                commentBtn.disabled = true;
                commentBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span> Đang gửi...';
            }

            try {
                const formData = new FormData(commentForm);
                const response = await fetch(commentForm.action, {
                    method: "POST",
                    body: formData,
                    headers: {
                        "X-Requested-With": "XMLHttpRequest"
                    }
                });

                if (response.ok) {
                    const resData = await response.json().catch(() => ({}));
                    if (commentInput) commentInput.value = "";
                    if (isInternalCheckbox) isInternalCheckbox.checked = false;

                    // Hiển thị tức thì bên người gửi (Optimistic/Instant Display)
                    if (resData && resData.comment) {
                        appendCommentToView(resData.comment);
                        scrollToBottom();
                    }
                } else {
                    const errData = await response.json().catch(() => ({}));
                    const errorMsg = errData.message || "Không thể gửi bình luận. Vui lòng thử lại.";
                    if (typeof window.showToastCard === "function") {
                        window.showToastCard('danger', 'Lỗi', errorMsg);
                    } else {
                        console.error("[Lỗi]", errorMsg);
                    }
                }
            } catch (err) {
                console.error("Lỗi gửi bình luận:", err);
                const errorMsg = "Lỗi kết nối máy chủ khi gửi phản hồi.";
                if (typeof window.showToastCard === "function") {
                    window.showToastCard('danger', 'Lỗi', errorMsg);
                } else {
                    console.error("[Lỗi]", errorMsg);
                }
            } finally {
                if (commentBtn) {
                    commentBtn.disabled = false;
                    commentBtn.innerHTML = '<svg class="bi me-1" width="16" height="16"><use xlink:href="#send-fill"/></svg> Gửi phản hồi';
                }
            }
        });
    }

    function appendCommentToView(comment) {
        if (!commentList || !comment) return;

        const commentId = comment.id !== undefined ? comment.id : comment.Id;
        // Chống trùng lặp: nếu đã tồn tại element có data-comment-id này thì bỏ qua
        if (commentId && document.querySelector(`[data-comment-id="${commentId}"]`)) {
            return;
        }

        const authorUserId = comment.authorUserId !== undefined ? comment.authorUserId : comment.AuthorUserId;
        const authorName = comment.authorName !== undefined ? comment.authorName : (comment.AuthorName || "Người dùng");
        const authorRole = comment.authorRole !== undefined ? comment.authorRole : (comment.AuthorRole || "");
        const formattedTime = comment.formattedTime !== undefined ? comment.formattedTime : (comment.FormattedTime || "Vừa xong");
        const content = comment.content !== undefined ? comment.content : (comment.Content || "");
        const isInternal = comment.isInternal !== undefined ? Boolean(comment.isInternal) : Boolean(comment.IsInternal);

        const isMe = authorUserId === currentUserId;
        const bubbleClass = isInternal 
            ? "chat-bubble-internal" 
            : (isMe ? "chat-bubble-me" : "chat-bubble-other");
        const alignClass = isMe ? "ms-auto text-end" : "me-auto text-start";
        const metaAlign = isMe ? "justify-content-end" : "justify-content-start";

        const internalBadge = isInternal 
            ? '<span class="badge bg-warning text-dark me-1">Nội bộ IT</span>' 
            : '';

        const roleBadge = authorRole 
            ? `<span class="badge bg-secondary-subtle ms-1">${escapeHtml(authorRole)}</span>`
            : '';

        const commentHtml = `
            <div class="mb-3 d-flex flex-column ${alignClass}" style="max-width: 80%;" data-comment-id="${commentId || ''}">
                <div class="d-flex align-items-center mb-1 small text-muted ${metaAlign}">
                    ${internalBadge}
                    <strong>${escapeHtml(authorName)}</strong>
                    ${roleBadge}
                    <span class="ms-2">${escapeHtml(formattedTime)}</span>
                </div>
                <div class="p-3 rounded-3 ${bubbleClass} text-start">
                    ${escapeHtml(content).replace(/\n/g, '<br/>')}
                </div>
            </div>`;

        // Xóa dòng "Chưa có bình luận" nếu có
        const emptyMsg = commentList.querySelector(".empty-comment-placeholder");
        if (emptyMsg) emptyMsg.remove();

        commentList.insertAdjacentHTML("beforeend", commentHtml);
    }

    function scrollToBottom() {
        if (commentList) {
            commentList.scrollTop = commentList.scrollHeight;
        }
    }

    function escapeHtml(text) {
        if (!text) return "";
        const div = document.createElement("div");
        div.textContent = text;
        return div.innerHTML;
    }
});
