/* ChienNguyenShop: one custom JavaScript file, shared across shop and admin. */
"use strict";
document.addEventListener("DOMContentLoaded", () => {
    document.querySelectorAll("form[data-confirm]").forEach(form => {
        form.addEventListener("submit", event => {
            if (!window.confirm(form.dataset.confirm)) event.preventDefault();
        });
    });
    document.querySelectorAll(".password-toggle").forEach(button => {
        button.addEventListener("click", () => {
            const input = button.parentElement.querySelector("input");
            if (!input) return;
            const visible = input.type === "password";
            input.type = visible ? "text" : "password";
            button.textContent = visible ? "Ẩn" : "Hiện";
            button.setAttribute("aria-label", visible ? "Ẩn mật khẩu" : "Hiện mật khẩu");
        });
    });
    document.querySelectorAll("input[data-image-preview]").forEach(input => {
        let previousUrl;
        input.addEventListener("change", () => {
            const file = input.files && input.files[0];
            const image = document.getElementById(input.dataset.imagePreview);
            if (!file || !image) return;
            if (!["image/jpeg", "image/png", "image/webp"].includes(file.type) || file.size > 5 * 1024 * 1024) {
                alert("Chọn ảnh JPG, PNG hoặc WebP, tối đa 5MB.");
                input.value = "";
                return;
            }
            if (previousUrl) URL.revokeObjectURL(previousUrl);
            previousUrl = URL.createObjectURL(file);
            image.src = previousUrl;
        });
    });
    document.querySelectorAll("[data-print]").forEach(button => button.addEventListener("click", () => window.print()));
    const toggle = document.querySelector(".admin-menu-toggle");
    const sidebar = document.getElementById("adminSidebar");
    if (toggle && sidebar) {
        toggle.addEventListener("click", () => {
            const opened = sidebar.classList.toggle("open");
            toggle.setAttribute("aria-expanded", String(opened));
        });
        document.addEventListener("click", event => {
            if (!sidebar.contains(event.target) && !toggle.contains(event.target)) {
                sidebar.classList.remove("open");
                toggle.setAttribute("aria-expanded", "false");
            }
        });
    }
});
