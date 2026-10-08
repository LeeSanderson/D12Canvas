// The board container is overflow: hidden, and a plain focus() scrolls it to reveal the editor,
// moving the board without the canvas's zoom and pan knowing. Focus and selection happen in one
// call, so a key typed the moment the editor has focus already replaces the text.
export function focusAndSelectAll(editor) {
    editor.focus({ preventScroll: true });
    if (typeof editor.select === "function") {
        editor.select();
    }
}
