document.addEventListener("DOMContentLoaded", () => {
    // MENU MOBILE
    const menuToggle = document.getElementById("menuToggle");
    const navigation = document.getElementById("mainNavigation");

    navigation?.querySelectorAll("a").forEach(link => {
        const linkPath = new URL(link.href, window.location.origin).pathname.replace(/\/$/, "").toLowerCase();
        const currentPath = window.location.pathname.replace(/\/$/, "").toLowerCase();
        const isCurrentPage = linkPath === currentPath || (!linkPath && !currentPath);
        link.classList.toggle("active", isCurrentPage);
        if (isCurrentPage) link.setAttribute("aria-current", "page");
        else link.removeAttribute("aria-current");
    });

    menuToggle?.addEventListener("click", () => {
        navigation?.classList.toggle("active");
    });

    // GIỎ HÀNG
    function loadCart() {
        const storedCart = localStorage.getItem("motohubCart");
        if (!storedCart) return [];

        try {
            const parsedCart = JSON.parse(storedCart);
            if (!Array.isArray(parsedCart)) return [];

            return parsedCart.filter(item =>
                item &&
                Number.isInteger(item.id) &&
                item.id > 0 &&
                typeof item.name === "string" &&
                Number.isFinite(item.price) &&
                Number.isInteger(item.quantity) &&
                item.quantity > 0
            );
        } catch {
            localStorage.removeItem("motohubCart");
            return [];
        }
    }

    let cart = loadCart();

    function saveCart() {
        localStorage.setItem("motohubCart", JSON.stringify(cart));
        renderCart();
    }

    function formatPrice(price) {
        return new Intl.NumberFormat("vi-VN").format(price) + " ₫";
    }

    function renderCart() {
        const cartCount = document.getElementById("cartCount");
        const cartItems = document.getElementById("cartItems");
        const cartTotal = document.getElementById("cartTotal");
        const cartQuantity = document.getElementById("cartQuantity");

        const totalQuantity = cart.reduce(
            (sum, item) => sum + item.quantity, 0
        );

        if (cartCount) cartCount.textContent = totalQuantity;
        if (cartQuantity) cartQuantity.textContent = totalQuantity;

        if (!cartItems || !cartTotal) return;

        if (cart.length === 0) {
            cartItems.innerHTML =
                '<div class="cart-empty"><span aria-hidden="true">◇</span><h3>Giỏ hàng đang trống</h3><p>Chọn một mẫu xe từ bộ sưu tập để bắt đầu.</p><a href="/Collection" class="btn-gold">Khám phá bộ sưu tập</a></div>';
            cartTotal.textContent = "0 ₫";
            return;
        }

        cartItems.innerHTML = cart.map((item, index) => `
            <article class="cart-item">
                <img class="cart-item-image" src="${escapeHtml(item.image || "/images/motorcycle-placeholder.svg")}" alt="${escapeHtml(item.name)}" loading="lazy">
                <div class="cart-item-info">
                    <span class="cart-item-brand">${escapeHtml(item.brand || "MOTOHUB")}${item.category ? ` · ${escapeHtml(item.category)}` : ""}</span>
                    <h3>${escapeHtml(item.name)}</h3>
                    <p>${escapeHtml(item.engine || "Mẫu xe chính hãng")}</p>
                    <strong>${formatPrice(item.price)}</strong>
                </div>
                <div class="cart-item-actions" aria-label="Điều chỉnh số lượng">
                    <div class="quantity-control">
                        <button type="button" data-action="decrease" data-index="${index}" aria-label="Giảm số lượng">−</button>
                        <span>${item.quantity}</span>
                        <button type="button" data-action="increase" data-index="${index}" aria-label="Tăng số lượng">+</button>
                    </div>
                    <button class="cart-remove" type="button" data-action="remove" data-index="${index}">Xóa khỏi giỏ</button>
                </div>
            </article>
        `).join("");
        cartItems.querySelectorAll(".cart-item-image").forEach(useImageFallback);

        const total = cart.reduce(
            (sum, item) => sum + item.price * item.quantity, 0
        );

        cartTotal.textContent = formatPrice(total);
    }

    function escapeHtml(value) {
        return String(value).replace(/[&<>"']/g, char => ({
            "&": "&amp;",
            "<": "&lt;",
            ">": "&gt;",
            '"': "&quot;",
            "'": "&#39;"
        })[char]);
    }

    function useImageFallback(image) {
        image.addEventListener("error", () => {
            image.src = "/images/motorcycle-placeholder.svg";
            image.classList.add("image-unavailable");
        }, { once: true });
    }

    async function hydrateCartDetails() {
        if (!cart.length || cart.every(item => item.image && item.brand && item.engine)) return;
        try {
            const response = await fetch("/api/storefront/catalog", { headers: { Accept: "application/json" } });
            if (!response.ok) return;
            const products = await response.json();
            let changed = false;
            for (const item of cart) {
                const product = products.find(candidate => candidate.id === item.id);
                if (!product) continue;
                Object.assign(item, {
                    name: product.name,
                    brand: product.brand,
                    category: product.category,
                    engine: product.engine,
                    image: product.image?.startsWith("photo-")
                        ? `https://images.unsplash.com/${product.image}?auto=format&fit=crop&w=700&q=80`
                        : product.image || "/images/motorcycle-placeholder.svg",
                    price: product.price
                });
                changed = true;
            }
            if (changed) saveCart();
        } catch {
            // The saved cart still renders with the local placeholder if the catalog is unavailable.
        }
    }

    document.querySelectorAll(".add-cart").forEach(button => {
        button.addEventListener("click", () => {
            const id = Number(button.dataset.id);
            const name = button.dataset.name;
            const price = Number(button.dataset.price);
            const details = {
                name,
                price,
                image: button.dataset.image || "/images/motorcycle-placeholder.svg",
                brand: button.dataset.brand || "MOTOHUB",
                category: button.dataset.category || "",
                engine: button.dataset.engine || ""
            };

            if (!Number.isInteger(id) || id < 1 || !name || !Number.isSafeInteger(price) || price < 0) return;

            const existing = cart.find(item => item.id === id);

            if (existing) {
                existing.quantity++;
                Object.assign(existing, details);
            } else {
                cart.push({ id, ...details, quantity: 1 });
            }

            saveCart();
            alert("Đã thêm sản phẩm vào giỏ hàng!");
        });
    });

    document.getElementById("cartItems")?.addEventListener("click", event => {
        const button = event.target.closest("button[data-action]");
        if (!button) return;

        const index = Number(button.dataset.index);
        const action = button.dataset.action;
        const item = cart[index];

        if (!item) return;

        if (action === "increase") item.quantity++;
        if (action === "decrease") item.quantity--;
        if (action === "remove" || item.quantity <= 0) {
            cart.splice(index, 1);
        }

        saveCart();
    });

    document.getElementById("checkout")?.addEventListener("click", () => {
        if (cart.length === 0) {
            alert("Giỏ hàng đang trống!");
            return;
        }

        window.location.assign("/Checkout");
    });

    // TÌM KIẾM VÀ LỌC XE
    const searchInput = document.getElementById("searchInput");
    const sortPrice = document.getElementById("sortPrice");
    const productList = document.getElementById("productList");
    const noProduct = document.getElementById("noProduct");

    let selectedCategory = "all";

    function filterProducts() {
        if (!productList) return;

        const keyword = (searchInput?.value || "").toLowerCase();
        const products = [...productList.querySelectorAll(".product-card")];

        products.forEach(product => {
            const name = product.dataset.name.toLowerCase();
            const category = product.dataset.category;

            const matchName = name.includes(keyword);
            const matchCategory =
                selectedCategory === "all" || category === selectedCategory;

            product.hidden = !(matchName && matchCategory);
        });

        if (noProduct) {
            noProduct.hidden = products.some(product => !product.hidden);
        }
    }

    searchInput?.addEventListener("input", filterProducts);

    document.querySelectorAll(".filter").forEach(button => {
        button.addEventListener("click", () => {
            document.querySelectorAll(".filter").forEach(item =>
                item.classList.remove("active")
            );

            button.classList.add("active");
            selectedCategory = button.dataset.category;
            filterProducts();
        });
    });

    sortPrice?.addEventListener("change", () => {
        if (!productList) return;

        const products = [...productList.querySelectorAll(".product-card")];

        products.sort((a, b) => {
            const priceA = Number(a.dataset.price);
            const priceB = Number(b.dataset.price);

            if (sortPrice.value === "asc") return priceA - priceB;
            if (sortPrice.value === "desc") return priceB - priceA;

            return 0;
        });

        products.forEach(product => productList.appendChild(product));
    });

    // SAO CHÉP MÃ ƯU ĐÃI
    document.querySelectorAll(".copy-voucher").forEach(button => {
        button.addEventListener("click", async () => {
            try {
                await navigator.clipboard.writeText(button.dataset.code);
                alert("Đã sao chép mã: " + button.dataset.code);
            } catch {
                alert("Không thể sao chép mã trên trình duyệt này.");
            }
        });
    });

    document.querySelectorAll("img").forEach(useImageFallback);

    // FORM LIÊN HỆ - HIỆN TẠI CHỈ KIỂM TRA GIAO DIỆN
    document.getElementById("contactForm")?.addEventListener("submit", event => {
        event.preventDefault();

        const message = document.getElementById("contactMessage");
        if (message) {
            message.textContent =
                "Thông tin đã được kiểm tra. Chức năng gửi liên hệ cần kết nối backend.";
        }
    });

    document.getElementById("forgotPasswordForm")?.addEventListener("submit", event => {
        event.preventDefault();
        const message = document.getElementById("recoveryMessage");
        if (message) {
            message.textContent = "Biểu mẫu đã được kiểm tra. Gửi email khôi phục cần kết nối dịch vụ tài khoản.";
        }
    });

    renderCart();
    void hydrateCartDetails();
});
