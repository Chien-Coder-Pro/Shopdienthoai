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
    const chatPanel = document.getElementById("chatPanel");
    const chatLauncher = document.getElementById("chatLauncher");
    const chatClose = document.getElementById("chatClose");
    const chatForm = document.getElementById("chatForm");
    const chatInput = document.getElementById("chatInput");
    const chatMessages = document.getElementById("chatMessages");
    if (chatPanel && chatLauncher && chatClose && chatForm && chatInput && chatMessages) {
        const setChatOpen = opened => {
            chatPanel.hidden = !opened;
            chatLauncher.setAttribute("aria-expanded", String(opened));
            if (opened) chatInput.focus();
        };
        const appendChatMessage = (text, sender, products, link, linkText) => {
            const message = document.createElement("div");
            message.className = `chat-message chat-message-${sender}`;
            message.textContent = text;
            chatMessages.append(message);
            if (Array.isArray(products) && products.length) {
                const list = document.createElement("div");
                list.className = "chat-products";
                products.forEach(product => {
                    const item = document.createElement("a");
                    item.href = `/Customer/Details/${encodeURIComponent(product.id)}`;
                    item.textContent = `${product.name} · ${product.price}`;
                    list.append(item);
                });
                chatMessages.append(list);
            }
            if (link && linkText) {
                const action = document.createElement("a");
                action.className = "chat-action-link";
                action.href = link;
                action.textContent = linkText;
                chatMessages.append(action);
            }
            chatMessages.scrollTop = chatMessages.scrollHeight;
        };
        chatLauncher.addEventListener("click", () => setChatOpen(chatPanel.hidden));
        chatClose.addEventListener("click", () => setChatOpen(false));
        document.querySelectorAll("[data-chat-suggestion]").forEach(button => {
            button.addEventListener("click", () => {
                chatInput.value = button.dataset.chatSuggestion || "";
                chatForm.requestSubmit();
            });
        });
        chatForm.addEventListener("submit", async event => {
            event.preventDefault();
            const message = chatInput.value.trim();
            if (!message) return;
            appendChatMessage(message, "user");
            chatInput.value = "";
            const sendButton = chatForm.querySelector("button[type='submit']");
            if (sendButton) sendButton.disabled = true;
            try {
                const response = await fetch(chatForm.action, {
                    method: "POST",
                    body: new FormData(chatForm),
                    headers: { "X-Requested-With": "XMLHttpRequest" }
                });
                const result = await response.json();
                if (!response.ok) throw new Error(result.error || "Không thể gửi tin nhắn.");
                appendChatMessage(result.answer, "bot", result.products, result.link, result.linkText);
            } catch (error) {
                const failure = error instanceof Error ? error.message : "Vui lòng thử lại sau.";
                appendChatMessage(`Chat chưa thể trả lời: ${failure}`, "bot");
            } finally {
                if (sendButton) sendButton.disabled = false;
                chatInput.focus();
            }
        });
    }
    const canHover = window.matchMedia("(hover: hover) and (pointer: fine)").matches;
    const prefersReducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    if (canHover && !prefersReducedMotion) {
        document.querySelectorAll(".product-card, .stat-card, .nav-chip, .btn, .btn-add, .cart-link, .account-button, .admin-sidebar nav > a, .pagination-shop a, .payment-option").forEach(element => {
            element.classList.add("hover-motion");
            element.addEventListener("pointerenter", () => element.classList.add("is-hovered"));
            element.addEventListener("pointerleave", () => element.classList.remove("is-hovered"));
        });
    }
});
