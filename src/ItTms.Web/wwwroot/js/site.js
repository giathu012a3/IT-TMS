/**
 * IT-TMS Core Client Scripts
 * Theme toggle (Light/Dark mode) & UI Helpers
 */
document.addEventListener("DOMContentLoaded", function () {
    // 1. Quản lý Dark/Light Theme Toggle
    const btnThemeToggle = document.getElementById("btnThemeToggle");
    const themeIconUse = document.querySelector("#themeIcon use");

    function applyTheme(theme) {
        document.documentElement.setAttribute("data-bs-theme", theme);
        localStorage.setItem("it-tms-theme", theme);

        if (themeIconUse) {
            themeIconUse.setAttribute("xlink:href", theme === "dark" ? "#sun-fill" : "#moon-fill");
        }
    }

    const currentTheme = localStorage.getItem("it-tms-theme") || 
        (window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light");
    applyTheme(currentTheme);

    if (btnThemeToggle) {
        btnThemeToggle.addEventListener("click", function () {
            const activeTheme = document.documentElement.getAttribute("data-bs-theme");
            const newTheme = activeTheme === "dark" ? "light" : "dark";
            applyTheme(newTheme);
        });
    }

    // 2. Khởi tạo tất cả Bootstrap Toasts nếu có
    const toastElList = document.querySelectorAll(".toast");
    toastElList.forEach(function (toastEl) {
        if (window.bootstrap && bootstrap.Toast) {
            const toast = new bootstrap.Toast(toastEl);
            toast.show();
        }
    });

    // 3. Khởi tạo tất cả Tooltips nếu có
    const tooltipTriggerList = document.querySelectorAll('[data-bs-toggle="tooltip"]');
    tooltipTriggerList.forEach(function (tooltipTriggerEl) {
        if (window.bootstrap && bootstrap.Tooltip) {
            new bootstrap.Tooltip(tooltipTriggerEl);
        }
    });

    // 4. Tự động đóng thông báo Flash Alert sau 4 giây
    const flashAlerts = document.querySelectorAll(".flash-alert-container .alert");
    flashAlerts.forEach(function (alertEl) {
        setTimeout(function () {
            if (window.bootstrap && bootstrap.Alert) {
                const bsAlert = bootstrap.Alert.getOrCreateInstance(alertEl);
                bsAlert.close();
            }
        }, 4000);
    });
});
