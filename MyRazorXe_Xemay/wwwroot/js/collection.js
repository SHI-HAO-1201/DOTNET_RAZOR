(() => {
    const list = document.getElementById("productList");
    const search = document.getElementById("searchInput");
    const sort = document.getElementById("sortPrice");
    const empty = document.getElementById("noProduct");
    if (!list || !(search instanceof HTMLInputElement) || !(sort instanceof HTMLSelectElement)) return;

    const cards = Array.from(list.querySelectorAll(".product-card"));
    const render = () => {
        const query = search.value.trim().toLocaleLowerCase("vi");
        const visible = cards.filter(card => (card.dataset.name || "").toLocaleLowerCase("vi").includes(query));
        visible.sort((left, right) => {
            const difference = Number(left.dataset.price) - Number(right.dataset.price);
            return sort.value === "asc" ? difference : sort.value === "desc" ? -difference : 0;
        });
        cards.forEach(card => { card.hidden = true; });
        visible.forEach(card => { card.hidden = false; list.append(card); });
        if (empty) empty.hidden = visible.length > 0;
    };

    search.addEventListener("input", render);
    sort.addEventListener("change", render);
})();
