// The board container is overflow: hidden, and a plain focus() scrolls it to reveal the editor,
// moving the board without the canvas's zoom and pan knowing. Focus and selection happen in one
// call, so a key typed the moment the editor has focus already replaces the text.
export function focusAndSelectAll(editor) {
    editor.focus({ preventScroll: true });
    if (typeof editor.select === "function") {
        editor.select();
    }
}

// field-sizing: content is missing from Firefox, so the editor sets its own height to its text's
// on every input, which keeps a middle or bottom aligned note's text where it was. Blazor owns the
// style attribute and drops the height when it rewrites it, so a rewrite refits too.
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
