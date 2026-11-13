document.addEventListener("click", event => {
    const target = event.target;
    if (!(target instanceof Element)) return;

    const button = target.closest("button[data-password-toggle]");
    if (!(button instanceof HTMLButtonElement)) return;

    const input = button.closest(".password-field")?.querySelector("input");
    if (!(input instanceof HTMLInputElement)) return;

    const showPassword = input.type === "password";
    input.type = showPassword ? "text" : "password";
    button.textContent = showPassword ? "Ẩn" : "Hiện";
    button.setAttribute("aria-label", showPassword ? "Ẩn mật khẩu" : "Hiện mật khẩu");
    button.setAttribute("aria-pressed", String(showPassword));
    input.focus({ preventScroll: true });
});
