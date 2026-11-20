export function focusAndSelectAll(editor) {
    editor.focus({ preventScroll: true });
    if (typeof editor.select === "function") {
        editor.select();
    }
}

export function growWithContent(editor) {
    const fit = () => {
        editor.style.height = "auto";
        editor.style.height = `${editor.scrollHeight}px`;
    };
    fit();
    editor.addEventListener("input", fit);
    new MutationObserver(() => {
        if (!editor.style.height) {
            fit();
        }
    }).observe(editor, { attributes: true, attributeFilter: ["style"] });
}
