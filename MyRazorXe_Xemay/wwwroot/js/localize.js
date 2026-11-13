(() => {
    const legacyBytes = new Map();
    const legacyValues = [0x20ac, 0x81, 0x201a, 0x192, 0x201e, 0x2026, 0x2020, 0x2021, 0x2c6, 0x2030, 0x160, 0x2039, 0x152, 0x8d, 0x17d, 0x8f, 0x90, 0x2018, 0x2019, 0x201c, 0x201d, 0x2022, 0x2013, 0x2014, 0x2dc, 0x2122, 0x161, 0x203a, 0x153, 0x9d, 0x17e, 0x178];
    legacyValues.forEach((code, index) => legacyBytes.set(String.fromCharCode(code), index + 128));
    const labels = new Map([
        ["EXPERIENCE MOTOHUB", "KHÁM PHÁ MOTOHUB"], ["VISIT OUR SHOWROOMS", "HỆ THỐNG SHOWROOM"], ["EXCLUSIVE PRIVILEGES", "ĐẶC QUYỀN KHÁCH HÀNG"],
        ["CURRENT PROMOTIONS", "ƯU ĐÃI HIỆN CÓ"], ["NEW CUSTOMER", "KHÁCH HÀNG MỚI"], ["PREMIUM SERVICE", "DỊCH VỤ CAO CẤP"],
        ["MOTOHUB MEMBER", "THÀNH VIÊN MOTOHUB"], ["MOTOHUB JOURNAL", "TẠP CHÍ MOTOHUB"], ["LATEST STORIES", "BÀI VIẾT MỚI NHẤT"],
        ["LET'S CONNECT", "KẾT NỐI VỚI CHÚNG TÔI"], ["CONTACT INFORMATION", "THÔNG TIN LIÊN HỆ"], ["SEND US A MESSAGE", "GỬI LỜI NHẮN"],
        ["THE MOTOHUB COLLECTION", "BỘ SƯU TẬP MOTOHUB"], ["DISCOVER YOUR MACHINE", "TÌM CHIẾC XE CỦA BẠN"], ["SUPERSPORT", "MÔ TÔ THỂ THAO"],
        ["NAKED BIKE", "MÔ TÔ ĐƯỜNG PHỐ"], ["TOURING", "MÔ TÔ ĐƯỜNG TRƯỜNG"], ["THE ART OF SPEED — THE SPIRIT OF RIDING", "NGHỆ THUẬT TỐC ĐỘ — BẢN LĨNH NGƯỜI LÁI"],
        ["ORDER SUMMARY", "TÓM TẮT ĐƠN HÀNG"], ["YOUR RIDING SELECTION", "LỰA CHỌN CỦA BẠN"], ["YOUR COLLECTION", "BỘ SƯU TẬP CỦA BẠN"],
        ["MEMBER ACCESS", "ĐĂNG NHẬP THÀNH VIÊN"], ["BECOME A MEMBER", "TRỞ THÀNH THÀNH VIÊN"], ["THE SPIRIT OF RIDING", "TINH THẦN TỰ DO"],
        ["ALL RIGHTS RESERVED.", "ĐÃ ĐĂNG KÝ BẢN QUYỀN."], ["ENGINEERED FOR PASSION", "THIẾT KẾ CHO ĐAM MÊ"], ["BUILT FOR THE RIDE", "SẴN SÀNG CHO MỌI HÀNH TRÌNH"]
        , ["BORN TO", "SINH RA ĐỂ"], ["RIDE.", "LÁI XE."], ["MORE THAN", "KHÔNG CHỈ LÀ"], ["A MACHINE.", "MỘT CỖ MÁY."],
        ["YOUR PASSION.", "ĐAM MÊ CỦA BẠN."], ["YOUR PRIVILEGE.", "ĐẶC QUYỀN CỦA BẠN."], ["FEEL THE", "CẢM NHẬN"], ["POWER.", "SỨC MẠNH."],
        ["EXPERIENCE THE DIFFERENCE", "TRẢI NGHIỆM KHÁC BIỆT"], ["BEYOND THE MACHINE", "VƯỢT LÊN TRÊN CỖ MÁY"], ["THE BRANDS WE ADMIRE", "THƯƠNG HIỆU CHÚNG TÔI TIN CHỌN"],
        ["THE SPIRIT OF RIDING", "TINH THẦN CHINH PHỤC"], ["JOIN THE COMMUNITY", "GIA NHẬP CỘNG ĐỒNG"], ["YOUR RIDING SELECTION", "LỰA CHỌN CỦA BẠN"],
        ["CARE & SERVICE", "CHĂM SÓC & BẢO DƯỠNG"], ["OFF", "GIẢM GIÁ"], ["PREMIUM", "CAO CẤP"], ["YAMAHA · SUPERSPORT", "YAMAHA · MÔ TÔ THỂ THAO"],
        ["KAWASAKI · SUPERSPORT", "KAWASAKI · MÔ TÔ THỂ THAO"], ["BMW MOTORRAD · SUPERSPORT", "BMW MOTORRAD · MÔ TÔ THỂ THAO"],
        ["Hotline:", "Đường dây nóng:"], ["EMAIL", "THƯ ĐIỆN TỬ"], ["HOTLINE", "ĐƯỜNG DÂY NÓNG"], ["SUPPORT", "HỖ TRỢ"]
    ]);

    function repair(value) {
        const bytes = [];
        for (const character of value) {
            const code = character.codePointAt(0);
            if (code <= 255) bytes.push(code);
            else if (legacyBytes.has(character)) bytes.push(legacyBytes.get(character));
            else bytes.push(...new TextEncoder().encode(character));
        }
        try {
            const decoded = new TextDecoder("utf-8", { fatal: true }).decode(new Uint8Array(bytes));
            return decoded !== value && /[\u00e0-\u1ef9\u0110\u0111]/i.test(decoded) ? decoded : value;
        } catch { return value; }
    }
    function translate(value) {
        let result = repair(value);
        for (const [english, vietnamese] of labels) result = result.replaceAll(english, repair(vietnamese));
        return result;
    }
    function localize() {
        const pageTitle = translate(document.title);
        if (pageTitle !== document.title) document.title = pageTitle;
        const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT);
        while (walker.nextNode()) {
            const original = walker.currentNode.textContent || "";
            const localized = translate(original);
            if (localized !== original) walker.currentNode.textContent = localized;
        }
        document.querySelectorAll("[alt], [placeholder], [aria-label], title").forEach(element => {
            for (const attribute of ["alt", "placeholder", "aria-label", "title"]) {
                const value = element.getAttribute(attribute);
                if (value) {
                    const localized = translate(value);
                    if (localized !== value) element.setAttribute(attribute, localized);
                }
            }
        });
        document.querySelectorAll("img:not([data-fallback-bound])").forEach(image => {
            image.dataset.fallbackBound = "true";
            image.addEventListener("error", () => {
            image.hidden = true;
            if (image.parentElement) image.parentElement.classList.add("image-unavailable");
            }, { once: true });
        });
    }
    if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", () => {
        localize();
        new MutationObserver(localize).observe(document.body, { childList: true, subtree: true });
    });
    else {
        localize();
        new MutationObserver(localize).observe(document.body, { childList: true, subtree: true });
    }
})();
