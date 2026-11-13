document.addEventListener("DOMContentLoaded", () => {
    const state = { products: [], orders: [], customers: [], promotions: [], dashboard: null };
    document.getElementById("adminToday").textContent = new Intl.DateTimeFormat("vi-VN", { weekday: "long", day: "2-digit", month: "long", year: "numeric" }).format(new Date()).toLocaleUpperCase("vi-VN");
    const money = value => new Intl.NumberFormat("vi-VN").format(Number(value || 0)) + " ₫";
    const escapeHtml = value => String(value ?? "").replace(/[&<>"']/g, char => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[char]);
    const statusLabels = { Pending: "Chờ xác nhận", Processing: "Đang xử lý", Shipping: "Đang giao", Completed: "Hoàn tất", Cancelled: "Đã hủy" };
    const statusOptions = Object.keys(statusLabels);
    const request = async (url, options = {}) => {
        const response = await fetch(url, { ...options, headers: { ...(options.body ? { "Content-Type": "application/json" } : {}), ...options.headers } });
        if (!response.ok) {
            const body = await response.json().catch(() => null);
            const validation = body?.errors ? Object.values(body.errors).flat().join(" ") : "";
            throw new Error(validation || body?.message || body?.detail || `Thao tác chưa thành công (${response.status}).`);
        }
        return response.status === 204 ? null : response.json();
    };
    function notify(message, type = "success") {
        let toast = document.querySelector(".admin-toast");
        if (!toast) {
            toast = document.createElement("div");
            toast.className = "admin-toast";
            toast.setAttribute("role", "status");
            document.body.append(toast);
        }
        toast.textContent = message;
        toast.dataset.type = type;
        toast.classList.add("is-visible");
        clearTimeout(notify.timeout);
        notify.timeout = setTimeout(() => toast.classList.remove("is-visible"), 3600);
    }

    function renderProducts() {
        document.getElementById("productRows").innerHTML = state.products.map(product => `<tr data-search="${escapeHtml(product.name.toLowerCase())}"><td><strong>${escapeHtml(product.name)}</strong><small>${escapeHtml(product.brand)} · ${escapeHtml(product.code)}</small></td><td>${escapeHtml(product.category)}</td><td>${money(product.price)}</td><td>${product.stock}</td><td><span class="admin-status ${product.isActive ? "is-active" : "is-off"}">${product.isActive ? "Đang bán" : "Đã ẩn"}</span></td><td class="admin-row-actions"><button data-edit="${product.id}">Sửa</button><button data-toggle-product="${product.id}" data-active="${product.isActive}">${product.isActive ? "Ẩn" : "Hiện"}</button></td></tr>`).join("") || '<tr><td colspan="6" class="admin-empty">Chưa có sản phẩm nào.</td></tr>';
        filterProducts();
    }
    function renderOrders() {
        document.getElementById("orderRows").innerHTML = state.orders.map(order => `<tr data-search="${escapeHtml((order.orderNumber + order.customer).toLowerCase())}" data-status="${escapeHtml(order.status)}"><td><strong>${escapeHtml(order.orderNumber)}</strong></td><td>${escapeHtml(order.customer)}</td><td>${new Date(order.createdAtUtc).toLocaleDateString("vi-VN")}</td><td>${money(order.totalAmount)}</td><td><select class="admin-status-select" data-order-status="${order.id}">${statusOptions.map(status => `<option value="${status}" ${status === order.status ? "selected" : ""}>${statusLabels[status]}</option>`).join("")}</select></td><td class="admin-row-actions"><button data-delete-order="${order.id}">Xóa</button></td></tr>`).join("") || '<tr><td colspan="6" class="admin-empty">Chưa có đơn hàng nào.</td></tr>';
        filterOrders();
    }
    function renderCustomers() {
        document.getElementById("customerRows").innerHTML = state.customers.map(customer => `<tr><td><strong>${escapeHtml(customer.name)}</strong></td><td>${escapeHtml(customer.email)}</td><td>${escapeHtml(customer.phone)}</td><td>${customer.orderCount}</td><td>${money(customer.spent)}</td><td class="admin-row-actions"><button data-edit-customer="${customer.id}">Sửa</button><button data-delete-customer="${customer.id}">Xóa</button></td></tr>`).join("") || '<tr><td colspan="6" class="admin-empty">Chưa có khách hàng nào.</td></tr>';
    }
    function renderPromotions() {
        document.getElementById("offerCards").innerHTML = state.promotions.map(promotion => `<article class="admin-offer-card"><span class="admin-status ${promotion.isActive ? "is-active" : "is-off"}">${promotion.isActive ? "Đang áp dụng" : "Đã tạm dừng"}</span><h3>${escapeHtml(promotion.name)}</h3><p>${escapeHtml(promotion.description)}</p><strong>${escapeHtml(promotion.code)}</strong><p>Giảm ${money(promotion.discountAmount)} · Đã dùng ${promotion.usedCount}/${promotion.usageLimit}</p><button data-toggle-promotion="${promotion.id}" data-active="${promotion.isActive}">${promotion.isActive ? "Tạm dừng" : "Kích hoạt"}</button></article>`).join("") || '<p class="admin-empty">Chưa có chương trình ưu đãi nào.</p>';
    }
    function renderDashboard() {
        const summary = state.dashboard;
        document.getElementById("productStat").textContent = state.products.length;
        document.getElementById("orderStat").textContent = summary.orderCount;
        document.getElementById("customerStat").textContent = summary.customerCount;
        document.getElementById("revenueStat").textContent = money(summary.revenue);
        document.getElementById("reportRevenue").textContent = money(summary.revenue);
        document.getElementById("reportComplete").textContent = summary.completedCount;
        document.getElementById("reportAverage").textContent = money(summary.averageOrderValue);
        document.getElementById("recentOrders").innerHTML = summary.recentOrders.map(order => `<div class="admin-recent-row"><span><strong>${escapeHtml(order.orderNumber)}</strong><small>${escapeHtml(order.customer)}</small></span><span>${money(order.totalAmount)}<small>${escapeHtml(statusLabels[order.status] || order.status)}</small></span></div>`).join("") || '<p class="admin-empty">Chưa có đơn hàng.</p>';
    }
    function filterProducts() {
        const query = (document.getElementById("productSearch")?.value || "").toLowerCase();
        document.querySelectorAll("#productRows tr[data-search]").forEach(row => row.hidden = !row.dataset.search.includes(query));
    }
    function filterOrders() {
        const query = (document.getElementById("orderSearch")?.value || "").toLowerCase();
        const status = document.getElementById("orderFilter")?.value || "all";
        document.querySelectorAll("#orderRows tr[data-search]").forEach(row => row.hidden = !row.dataset.search.includes(query) || (status !== "all" && row.dataset.status !== status));
    }
    async function loadAll() {
        const results = await Promise.allSettled([
            request("/api/admin/dashboard"), request("/api/admin/products"), request("/api/admin/orders"),
            request("/api/admin/customers"), request("/api/admin/promotions"), request("/api/admin/settings")
        ]);
        const [dashboardResult, productsResult, ordersResult, customersResult, promotionsResult, settingsResult] = results;
        if (dashboardResult.status === "fulfilled") state.dashboard = dashboardResult.value;
        if (productsResult.status === "fulfilled") state.products = productsResult.value;
        if (ordersResult.status === "fulfilled") state.orders = ordersResult.value;
        if (customersResult.status === "fulfilled") state.customers = customersResult.value;
        if (promotionsResult.status === "fulfilled") state.promotions = promotionsResult.value;
        if (settingsResult.status === "fulfilled") {
            const settings = settingsResult.value;
            document.getElementById("brandName").value = settings.BrandName ?? "MOTOHUB";
            document.getElementById("homeMessage").value = settings.HomeMessage ?? "";
            document.getElementById("settingEmail").value = settings.SupportEmail ?? "";
            document.getElementById("settingPhone").value = settings.SupportPhone ?? "";
            document.getElementById("settingCurrency").value = settings.Currency ?? "VND (₫)";
        }
        if (state.dashboard) renderDashboard();
        if (productsResult.status === "fulfilled") renderProducts();
        if (ordersResult.status === "fulfilled") renderOrders();
        if (customersResult.status === "fulfilled") renderCustomers();
        if (promotionsResult.status === "fulfilled") renderPromotions();
        const failures = results.filter(result => result.status === "rejected");
        if (failures.length) notify("Một số mục chưa tải được. Hãy tải lại trang hoặc đăng nhập lại.", "error");
    }
    document.querySelectorAll(".admin-nav button").forEach(button => button.addEventListener("click", () => {
        document.querySelectorAll(".admin-nav button").forEach(item => item.classList.toggle("active", item === button));
        document.querySelectorAll(".admin-panel").forEach(panel => panel.classList.toggle("active", panel.id === `panel-${button.dataset.panel}`));
        document.getElementById("adminHeading").textContent = button.innerText.trim().replace(/^\S+\s*/, "");
    }));
    document.querySelectorAll("[data-goto]").forEach(button => button.addEventListener("click", () => document.querySelector(`[data-panel="${button.dataset.goto}"]`)?.click()));

    const dialog = document.getElementById("productDialog");
    const form = document.getElementById("productForm");
    function openProduct(product) {
        const categorySelect = document.getElementById("productCategory");
        categorySelect.querySelectorAll("option[data-existing-category]").forEach(option => option.remove());
        form.reset();
        document.getElementById("editingProductId").value = product?.id ?? "";
        document.getElementById("productDialogTitle").textContent = product ? "Chỉnh sửa sản phẩm" : "Thêm sản phẩm";
        if (product) {
            document.getElementById("productName").value = product.name;
            if (![...categorySelect.options].some(option => option.value === product.category)) {
                categorySelect.add(new Option(product.category, product.category));
                categorySelect.lastElementChild.dataset.existingCategory = "true";
            }
            categorySelect.value = product.category;
            document.getElementById("productPrice").value = product.price;
            document.getElementById("productStock").value = product.stock;
            document.getElementById("productBrand").value = product.brand;
            document.getElementById("productCode").value = product.code;
            document.getElementById("productImage").value = product.image;
            document.getElementById("productEngine").value = product.engine;
        }
        dialog.showModal();
    }
    document.querySelectorAll("[data-open-product]").forEach(button => button.addEventListener("click", () => openProduct()));
    form.addEventListener("submit", async event => {
        if (event.submitter?.value === "cancel") return;
        event.preventDefault();
        const id = Number(document.getElementById("editingProductId").value || 0);
        const existingProduct = state.products.find(item => item.id === id);
        const stockQuantity = Number(document.getElementById("productStock").value);
        const product = { id, name: document.getElementById("productName").value.trim(), brand: document.getElementById("productBrand").value.trim(), code: document.getElementById("productCode").value.trim(), image: document.getElementById("productImage").value.trim(), engine: document.getElementById("productEngine").value.trim(), type: document.getElementById("productCategory").value, price: Number(document.getElementById("productPrice").value), stockQuantity, isActive: existingProduct?.isActive ?? true, availabilityStatus: stockQuantity > 0 ? "ForSale" : "Incoming" };
        try {
            await request("/api/admin/products", { method: "POST", body: JSON.stringify({ Id: product.id, Name: product.name, Brand: product.brand, Code: product.code, Image: product.image, Engine: product.engine, Type: product.type, Price: product.price, StockQuantity: product.stockQuantity, IsActive: product.isActive, AvailabilityStatus: product.availabilityStatus }) });
            dialog.close(); await loadAll(); notify("Đã lưu thông tin xe.");
        } catch (error) { notify(error.message || "Không thể lưu xe. Hãy kiểm tra thông tin đã nhập.", "error"); }
    });
    document.addEventListener("click", async event => {
        if (!(event.target instanceof Element)) return;
        const edit = event.target.closest("[data-edit]");
        const toggleProduct = event.target.closest("[data-toggle-product]");
        const toggle = event.target.closest("[data-toggle-promotion]");
        try {
            if (edit) {
                const id = edit.dataset.edit;
                const product = state.products.find(item => String(item.id) === id) ?? await request(`/api/admin/products/${id}`);
                if (!product) throw new Error("Không tìm thấy sản phẩm cần chỉnh sửa.");
                openProduct(product);
            }
            if (toggleProduct && confirm(toggleProduct.dataset.active === "true" ? "Ẩn xe này khỏi cửa hàng?" : "Hiện xe này trong cửa hàng?")) {
                const product = state.products.find(item => String(item.id) === toggleProduct.dataset.toggleProduct);
                if (!product) throw new Error("Không tìm thấy xe trong danh sách.");
                const isActive = toggleProduct.dataset.active !== "true";
                await request("/api/admin/products", { method: "POST", body: JSON.stringify({ Id: product.id, Name: product.name, Brand: product.brand, Code: product.code, Image: product.image || "", Engine: product.engine, Type: product.category, Price: product.price, StockQuantity: product.stock, IsActive: isActive, AvailabilityStatus: product.availabilityStatus }) });
                product.isActive = isActive;
                renderProducts();
                notify("Đã cập nhật trạng thái xe.");
            }
            if (toggle) {
                await request(`/api/admin/promotions/${toggle.dataset.togglePromotion}`, { method: "PATCH", body: JSON.stringify({ isActive: toggle.dataset.active !== "true" }) }); await loadAll();
                notify("Đã cập nhật chương trình ưu đãi.");
            }
        } catch (error) { notify(error.message || "Không thể cập nhật dữ liệu. Vui lòng thử lại.", "error"); }
    });
    document.getElementById("orderRows").addEventListener("change", async event => {
        const select = event.target.closest("[data-order-status]"); if (!select) return;
        try { await request(`/api/admin/orders/${select.dataset.orderStatus}/status`, { method: "PUT", body: JSON.stringify({ status: select.value }) }); await loadAll(); notify("Đã cập nhật trạng thái đơn hàng."); }
        catch (error) { notify(error.message || "Không thể cập nhật đơn hàng.", "error"); await loadAll(); }
    });
    document.getElementById("orderRows").addEventListener("click", async event => {
        if (!(event.target instanceof Element)) return;
        const button = event.target.closest("[data-delete-order]");
        if (!button || !confirm("Xóa đơn hàng này? Tồn kho sẽ được hoàn lại nếu đơn chưa bị hủy.")) return;
        try {
            await request(`/api/admin/orders/${button.dataset.deleteOrder}`, { method: "DELETE" });
            await loadAll();
            notify("Đã xóa đơn hàng.");
        } catch (error) { notify(error.message || "Không thể xóa đơn hàng.", "error"); }
    });
    const customerDialog = document.getElementById("customerDialog");
    const customerForm = document.getElementById("customerForm");
    function openCustomer(customer) {
        customerForm.reset();
        document.getElementById("editingCustomerId").value = customer?.id ?? "";
        document.getElementById("customerDialogTitle").textContent = customer ? "Chỉnh sửa khách hàng" : "Thêm khách hàng";
        if (customer) {
            document.getElementById("customerName").value = customer.name;
            document.getElementById("customerEmail").value = customer.email;
            document.getElementById("customerPhone").value = customer.phone;
            document.getElementById("customerAddress").value = customer.address ?? "";
        }
        customerDialog.showModal();
    }
    document.getElementById("addCustomer").addEventListener("click", () => openCustomer());
    customerForm.addEventListener("submit", async event => {
        if (event.submitter?.value === "cancel") return;
        event.preventDefault();
        const customer = {
            Id: Number(document.getElementById("editingCustomerId").value || 0),
            FullName: document.getElementById("customerName").value.trim(),
            Email: document.getElementById("customerEmail").value.trim(),
            Phone: document.getElementById("customerPhone").value.trim(),
            Address: document.getElementById("customerAddress").value.trim()
        };
        try {
            await request("/api/admin/customers", { method: "POST", body: JSON.stringify(customer) });
            customerDialog.close();
            await loadAll();
            notify("Đã lưu thông tin khách hàng.");
        } catch (error) { notify(error.message || "Không thể lưu thông tin khách hàng.", "error"); }
    });
    document.addEventListener("click", async event => {
        if (!(event.target instanceof Element)) return;
        const editCustomer = event.target.closest("[data-edit-customer]");
        const deleteCustomer = event.target.closest("[data-delete-customer]");
        if (editCustomer) {
            const customer = state.customers.find(item => String(item.id) === editCustomer.dataset.editCustomer);
            if (customer) openCustomer(customer);
            else notify("Không tìm thấy khách hàng cần chỉnh sửa.", "error");
        }
        if (deleteCustomer && confirm("Xóa khách hàng này? Khách hàng có đơn hàng sẽ không thể xóa.")) {
            try {
                await request(`/api/admin/customers/${deleteCustomer.dataset.deleteCustomer}`, { method: "DELETE" });
                await loadAll();
                notify("Đã xóa khách hàng.");
            } catch (error) { notify(error.message || "Không thể xóa khách hàng.", "error"); }
        }
    });
    document.getElementById("productSearch").addEventListener("input", filterProducts);
    document.getElementById("exportProducts").addEventListener("click", () => window.location.assign("/api/admin/products/export"));
    document.getElementById("orderSearch").addEventListener("input", filterOrders);
    document.getElementById("orderFilter").addEventListener("change", filterOrders);
    document.getElementById("addOffer").addEventListener("click", async () => {
        const code = prompt("Nhập mã ưu đãi:")?.trim().toUpperCase(); if (!code) return;
        const name = prompt("Nhập tên chương trình:")?.trim(); if (!name) return;
        const description = prompt("Mô tả ưu đãi:") || "";
        const discountAmount = Number(prompt("Số tiền giảm (VND):", "0"));
        if (!Number.isSafeInteger(discountAmount) || discountAmount < 0) { alert("Số tiền giảm không hợp lệ."); return; }
        try { await request("/api/admin/promotions", { method: "POST", body: JSON.stringify({ name, code, description, discountAmount, usageLimit: 100, startsAtUtc: new Date().toISOString(), endsAtUtc: new Date(Date.now() + 30 * 86400000).toISOString() }) }); await loadAll(); notify("Đã tạo chương trình ưu đãi."); }
        catch (error) { notify(error.message || "Không tạo được ưu đãi. Mã có thể đã tồn tại.", "error"); }
    });
    document.getElementById("saveContent").addEventListener("click", async () => saveSettings({ BrandName: document.getElementById("brandName").value, HomeMessage: document.getElementById("homeMessage").value }));
    document.getElementById("saveSettings").addEventListener("click", async () => saveSettings({ SupportEmail: document.getElementById("settingEmail").value, SupportPhone: document.getElementById("settingPhone").value, Currency: document.getElementById("settingCurrency").value }));
    async function saveSettings(values) {
        try { await request("/api/admin/settings", { method: "PUT", body: JSON.stringify(values) }); notify("Đã lưu cài đặt."); }
        catch (error) { notify(error.message || "Không thể lưu cài đặt.", "error"); }
    }
    document.getElementById("exportReport").addEventListener("click", () => {
        const lines = [["Mã đơn", "Khách hàng", "Ngày đặt", "Tổng tiền", "Trạng thái"], ...state.orders.map(order => [order.orderNumber, order.customer, new Date(order.createdAtUtc).toLocaleDateString("vi-VN"), order.totalAmount, statusLabels[order.status] || order.status])];
        const csv = "\uFEFF" + lines.map(line => line.map(value => `"${String(value).replaceAll('"', '""')}"`).join(",")).join("\n");
        const link = document.createElement("a"); link.href = URL.createObjectURL(new Blob([csv], { type: "text/csv;charset=utf-8" })); link.download = "bao-cao-don-hang.csv"; link.click(); URL.revokeObjectURL(link.href);
    });
    loadAll().catch(error => {
        console.error(error);
        notify("Không tải được dữ liệu quản trị. Hãy đăng nhập lại và thử tải lại trang.", "error");
    });
});
