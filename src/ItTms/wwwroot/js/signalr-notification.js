/**
 * signalr-notification.js
 * Multi-tier Floating Card Box Notification System & Global SignalR Hub Management.
 * WCAG AA Accessible, Hover-Pause 5s Auto-Dismiss Progress Bar, Zero Icon Font Glitch.
 */
(function () {
    'use strict';

    function escapeHtml(text) {
        if (text === null || text === undefined) return "";
        const div = document.createElement("div");
        div.textContent = text.toString();
        return div.innerHTML;
    }

    /**
     * Dismiss a toast card box with smooth slide-up and fade-out transition.
     */
    window.dismissToastCard = function (card) {
        if (!card || card.classList.contains("is-dismissing")) return;
        card.classList.add("is-dismissing");
        setTimeout(function () {
            if (card.parentNode) {
                card.parentNode.removeChild(card);
            }
        }, 220);
    };

    /**
     * Hiển thị Floating Card Box thông báo đa tầng góc trên bên phải.
     * @param {string|object} type Loại thông báo ('success', 'error', 'danger', 'warning', 'info') hoặc options object
     * @param {string} [title] Tiêu đề phân loại in đậm
     * @param {string} [message] Nội dung chi tiết thông báo
     * @param {number} [durationMs] Thời gian đếm ngược tự đóng (mặc định 5000ms)
     * @param {string} [link] Đường dẫn hành động liên kết (nếu có)
     * @returns {HTMLElement} Thẻ card DOM vừa tạo
     */
    window.showToastCard = function (type, title, message, durationMs, link) {
        // Hỗ trợ truyền theo dạng options object: window.showToastCard({ type, title, message, duration, link })
        if (typeof type === "object" && type !== null) {
            const opts = type;
            type = opts.type;
            title = opts.title;
            message = opts.message;
            durationMs = opts.durationMs || opts.duration;
            link = opts.link;
        }

        type = (type || "info").toString().toLowerCase();
        const normalizedType = (type === "danger") ? "error" : type;
        const duration = (typeof durationMs === "number" && durationMs > 0) ? durationMs : 5000;

        if (!title) {
            if (normalizedType === "success") title = "Thành công";
            else if (normalizedType === "error") title = "Thông báo lỗi";
            else if (normalizedType === "warning") title = "Lưu ý";
            else title = "Thông báo";
        }

        let iconSymbol = "#info-circle-fill";
        if (normalizedType === "success") iconSymbol = "#check-circle-fill";
        else if (normalizedType === "error") iconSymbol = "#x-circle-fill";
        else if (normalizedType === "warning") iconSymbol = "#exclamation-triangle-fill";
        else if (title.toLowerCase().includes("bảo mật")) iconSymbol = "#lock-fill";
        else if (normalizedType === "info") iconSymbol = "#bell-fill";

        let container = document.getElementById("toast-container");
        if (!container) {
            container = document.createElement("div");
            container.id = "toast-container";
            container.className = "floating-notification-stack position-fixed top-0 end-0 p-3";
            container.style.zIndex = "1090";
            document.body.appendChild(container);
        }

        const card = document.createElement("div");
        card.className = `floating-card-box floating-card-${normalizedType}`;
        card.setAttribute("role", "alert");
        card.setAttribute("aria-live", "assertive");

        const linkHtml = link
            ? `<div class="mt-2"><a href="${escapeHtml(link)}" class="btn btn-sm btn-outline-secondary py-0 px-2 font-monospace" style="font-size: 11.5px;">Xem chi tiết &rarr;</a></div>`
            : "";

        card.innerHTML = `
            <div class="floating-card-inner">
                <div class="floating-card-icon-box">
                    <svg class="bi" width="18" height="18"><use xlink:href="${iconSymbol}"/></svg>
                </div>
                <div class="floating-card-content">
                    <div class="floating-card-title">${escapeHtml(title)}</div>
                    <div class="floating-card-message">${escapeHtml(message || "")}</div>
                    ${linkHtml}
                </div>
                <button type="button" class="floating-card-close" aria-label="Đóng">
                    <svg class="bi" width="14" height="14"><use xlink:href="#x-circle-fill"/></svg>
                </button>
            </div>
            <div class="floating-card-progress">
                <div class="floating-card-progress-bar" style="animation-duration: ${duration}ms;"></div>
            </div>
        `;

        const closeBtn = card.querySelector(".floating-card-close");
        if (closeBtn) {
            closeBtn.addEventListener("click", function () {
                window.dismissToastCard(card);
            });
        }

        const progressBar = card.querySelector(".floating-card-progress-bar");
        if (progressBar && duration > 0) {
            progressBar.addEventListener("animationend", function () {
                window.dismissToastCard(card);
            });
        }

        container.appendChild(card);
        return card;
    };

    /**
     * Tự động khởi tạo bộ đếm ngược và sự kiện đóng cho các card render sẵn từ Server (TempData / ModelState).
     */
    function initExistingToastCards() {
        document.querySelectorAll("#toast-container .floating-card-box").forEach(function (card) {
            if (card.dataset.toastBound === "true") return;
            card.dataset.toastBound = "true";

            const closeBtn = card.querySelector(".floating-card-close");
            if (closeBtn) {
                closeBtn.addEventListener("click", function () {
                    window.dismissToastCard(card);
                });
            }

            const progressBar = card.querySelector(".floating-card-progress-bar");
            if (progressBar) {
                progressBar.addEventListener("animationend", function () {
                    window.dismissToastCard(card);
                });
            }
        });
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", initExistingToastCards);
    } else {
        initExistingToastCards();
    }

    // =========================================================================
    // SIGNALR HUB CONNECTION & EVENT DISPATCH (CHỈ CHẠY KHI ĐÃ ĐĂNG NHẬP)
    // =========================================================================
    const isAuth = document.body && document.body.dataset && document.body.dataset.authenticated === "true";
    if (!isAuth) {
        // Trang chưa xác thực (như Login) không kết nối Hub để tránh lỗi 401 Unauthorized
        return;
    }

    if (typeof signalR === "undefined") {
        console.warn("[SIGNALR] Thư viện SignalR chưa được nạp.");
        return;
    }

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/ticket")
        .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
        .configureLogging(signalR.LogLevel.Warning)
        .build();

    connection.on("ReceiveNotification", function (notification) {
        console.log("[SIGNALR] Thông báo mới:", notification);
        window.showToastCard("info", "Thông báo mới", notification.message, 6000, notification.link);
        prependNotificationItem(notification);
    });

    connection.on("UpdateUnreadNotificationCount", function (count) {
        const badge = document.getElementById("notificationCountBadge");
        if (badge) {
            if (count > 0) {
                badge.textContent = count > 99 ? "99+" : count;
                badge.classList.remove("d-none");
            } else {
                badge.classList.add("d-none");
            }
        }
    });

    connection.on("ForceLogout", function (reason) {
        window.showToastCard('danger', 'Bảo mật', reason, 10000);
        setTimeout(function () {
            window.location.href = "/Auth/Login?error=" + encodeURIComponent(reason);
        }, 2500);
    });

    async function startConnection() {
        try {
            await connection.start();
            console.log("[SIGNALR] Đã kết nối TicketHub.");
            window.ticketHubConnection = connection;
        } catch (err) {
            console.error("[SIGNALR] Lỗi kết nối Hub:", err);
            setTimeout(startConnection, 5000);
        }
    }

    startConnection();

    function prependNotificationItem(notif) {
        const list = document.getElementById("notificationDropdownList");
        if (!list) return;

        const emptyItem = list.querySelector(".empty-notification-item");
        if (emptyItem) emptyItem.remove();

        const itemHtml = `
            <li>
                <a class="dropdown-item py-2 border-bottom" href="${escapeHtml(notif.link || '#')}">
                    <div class="small text-muted">${escapeHtml(notif.formattedTime || 'Vừa xong')}</div>
                    <div class="text-truncate fw-semibold">${escapeHtml(notif.message)}</div>
                </a>
            </li>`;
        list.insertAdjacentHTML('afterbegin', itemHtml);
    }
})();
