export function systemMode() {
    return matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
}

export function setMode(mode) {
    document.documentElement.setAttribute("data-bs-theme", mode);
    document.documentElement.setAttribute("data-d12-theme", mode);
}
