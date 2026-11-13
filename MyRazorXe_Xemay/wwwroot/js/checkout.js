document.addEventListener("DOMContentLoaded", () => {
    const checkout = document.querySelector(".checkout-page");
    const form = document.getElementById("checkoutForm");
    const cartJson = document.getElementById("checkoutCartJson");
    if (!checkout || !form || !cartJson) return;

    if (checkout.dataset.orderPlaced === "true") {
        localStorage.removeItem("motohubCart");
        return;
    }

    let cart = [];
    try {
        const parsed = JSON.parse(localStorage.getItem("motohubCart") || "[]");
        if (Array.isArray(parsed)) {
            cart = parsed.filter(item => item && Number.isInteger(item.id) && item.id > 0
                && Number.isInteger(item.quantity) && item.quantity > 0);
        }
    } catch {
        localStorage.removeItem("motohubCart");
    }

    cartJson.value = JSON.stringify(cart.map(item => ({ id: item.id, quantity: item.quantity })));
    const items = document.getElementById("checkoutItems");
    const total = document.getElementById("checkoutTotal");
    const money = value => new Intl.NumberFormat("vi-VN").format(value) + " ₫";
    if (items) {
        items.replaceChildren();
        if (cart.length === 0) {
            items.innerHTML = '<p class="cart-empty">Giỏ hàng đang trống. <a href="/Collection">Quay lại bộ sưu tập</a></p>';
        } else {
            for (const item of cart) {
                const row = document.createElement("article");
                row.className = "checkout-item";
                const image = document.createElement("img");
                image.className = "checkout-item-image";
                image.src = item.image || "/images/motorcycle-placeholder.svg";
                image.alt = item.name || "Mẫu xe";
                image.addEventListener("error", () => { image.src = "/images/motorcycle-placeholder.svg"; }, { once: true });
                const description = document.createElement("span");
                description.className = "checkout-item-description";
                const name = document.createElement("strong");
                name.textContent = `${item.name} × ${item.quantity}`;
                const detail = document.createElement("small");
                detail.textContent = [item.brand, item.engine].filter(Boolean).join(" · ");
                description.append(name, detail);
                const price = document.createElement("b");
                price.textContent = money(item.price * item.quantity);
                row.append(image, description, price);
                items.append(row);
            }
        }
    }
    if (total) total.textContent = money(cart.reduce((sum, item) => sum + item.price * item.quantity, 0));

    form.addEventListener("submit", () => {
        cartJson.value = JSON.stringify(cart.map(item => ({ id: item.id, quantity: item.quantity })));
    });
});
