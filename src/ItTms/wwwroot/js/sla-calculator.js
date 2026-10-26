/**
 * sla-calculator.js
 * Tính toán và xem trước hạn cam kết SLA (DueAt) tức thì trên form tạo yêu cầu mới.
 */
document.addEventListener("DOMContentLoaded", function () {
    const prioritySelect = document.getElementById("prioritySelect");
    const slaPreviewBox = document.getElementById("slaPreviewBox");
    const slaHoursText = document.getElementById("slaHoursText");
    const slaDueAtText = document.getElementById("slaDueAtText");

    if (!prioritySelect || !slaPreviewBox) return;

    function updateSlaPreview() {
        const val = prioritySelect.value;
        const now = new Date();
        let hours = 24;
        let colorClass = "alert-primary";

        // Mức độ ưu tiên: Low (1) = 72h, Medium (2) = 24h, High (3) = 4h, Critical (4) = 1h
        if (val === "4" || val === "Critical") {
            hours = 1;
            colorClass = "alert-danger";
        } else if (val === "3" || val === "High") {
            hours = 4;
            colorClass = "alert-warning";
        } else if (val === "1" || val === "Low") {
            hours = 72;
            colorClass = "alert-secondary";
        } else {
            hours = 24;
            colorClass = "alert-primary";
        }

        const dueAt = new Date(now.getTime() + hours * 60 * 60 * 1000);
        const formattedDueAt = dueAt.toLocaleString("vi-VN", {
            day: "2-digit",
            month: "2-digit",
            year: "numeric",
            hour: "2-digit",
            minute: "2-digit"
        });

        slaPreviewBox.className = "alert " + colorClass + " mt-3 py-2";
        if (slaHoursText) slaHoursText.textContent = hours + " giờ";
        if (slaDueAtText) slaDueAtText.textContent = formattedDueAt;
    }

    prioritySelect.addEventListener("change", updateSlaPreview);
    updateSlaPreview();
});
