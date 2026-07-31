export async function getContainerDimensions(element) {
    const rect = element.getBoundingClientRect();
    return {
        width: rect.width,
        height: rect.height,
        left: rect.left,
        top: rect.top
    };
}

export async function addResizeListener(element, dotnetRef) {
    const handleResize = () => {
        const rect = element.getBoundingClientRect();
        dotnetRef.invokeMethodAsync("OnContainerResized", rect.width, rect.height);
    };

    handleResize();
    const resizeObserver = new ResizeObserver(handleResize);
    resizeObserver.observe(element);

    return () => {
        resizeObserver.disconnect();
    };
}

export async function getElementPosition(element, container) {
    const containerRect = container.getBoundingClientRect();
    const elementRect = element.getBoundingClientRect();
    return {
        x: elementRect.left - containerRect.left,
        y: elementRect.top - containerRect.top
    };
}

export async function getElementDimensions(element) {
    const rect = element.getBoundingClientRect();
    return {
        width: rect.width,
        height: rect.height
    };
}

// Moves DOM focus to a just-created Group's own tab stop, scoped to this canvas's own
// container. Grouping always clears any prior selection down to just the new group, so right
// after it commits exactly one group-tab-stop is aria-selected - no need to identify it by id.
export function focusGroupTabStop(container) {
    const stop = container.querySelector('.group-tab-stop[aria-selected="true"]');
    if (stop) {
        stop.focus();
    }
}

// Backspace/Delete double as the browser's own text-editing keys, so unlike this listener's other
// codes they must not fire while focus is on an editable host-page element (an <input>/<textarea>
// elsewhere on the embedding page, or a future in-canvas editable field) - otherwise typing
// Backspace there would silently wipe the canvas selection instead of deleting a character.
function isEditableTarget(target) {
    return (
        target instanceof HTMLInputElement ||
        target instanceof HTMLTextAreaElement ||
        target.isContentEditable
    );
}

export async function addKeyboardListener(element, dotnetRef) {
    const handleKeyDown = (event) => {
        // preventDefault is called only from inside a branch that actually invokes a
        // dotnetRef method - never unconditionally after the switch. Tab (native browser focus
        // navigation) and every other unhandled key must reach the browser's own default
        // handling, both here and in this host page beyond D12Canvas's own container (this
        // listener is window-level, so isEditableTarget's guards actually protect anything).
        switch (event.code) {
            case "PageUp":
                event.preventDefault();
                dotnetRef.invokeMethodAsync("OnZoomIn");
                break;
            case "PageDown":
                event.preventDefault();
                dotnetRef.invokeMethodAsync("OnZoomOut");
                break;
            case "ArrowLeft":
                event.preventDefault();
                dotnetRef.invokeMethodAsync("OnPanLeft");
                break;
            case "ArrowRight":
                event.preventDefault();
                dotnetRef.invokeMethodAsync("OnPanRight");
                break;
            case "ArrowUp":
                event.preventDefault();
                dotnetRef.invokeMethodAsync("OnPanUp");
                break;
            case "ArrowDown":
                event.preventDefault();
                dotnetRef.invokeMethodAsync("OnPanDown");
                break;
            case "Escape":
                event.preventDefault();
                dotnetRef.invokeMethodAsync("OnEscapePressed");
                break;
            case "Delete":
            case "Backspace":
                if (!isEditableTarget(event.target)) {
                    event.preventDefault();
                    dotnetRef.invokeMethodAsync("OnDeletePressed");
                }
                break;
            case "KeyZ":
                // Ctrl+Z (undo) doubles as the OS/browser's own text-editing undo, so this
                // guards against hijacking it while focus is on an editable host-page element -
                // same reasoning as Delete/Backspace above.
                if ((event.ctrlKey || event.metaKey) && !isEditableTarget(event.target)) {
                    event.preventDefault();
                    if (event.shiftKey) {
                        dotnetRef.invokeMethodAsync("OnRedoPressed");
                    } else {
                        dotnetRef.invokeMethodAsync("OnUndoPressed");
                    }
                }
                break;
            case "KeyG":
                // Ctrl+G (group) / Ctrl+Shift+G (ungroup). Guarded the same way as
                // Ctrl+Z above: while focus is on an editable host-page element (e.g. mid inline
                // WYSIWYG text edit), this must not hijack the keystroke.
                if ((event.ctrlKey || event.metaKey) && !isEditableTarget(event.target)) {
                    event.preventDefault();
                    if (event.shiftKey) {
                        dotnetRef.invokeMethodAsync("OnUngroupPressed");
                    } else {
                        dotnetRef.invokeMethodAsync("OnGroupPressed");
                    }
                }
                break;
            case "BracketRight":
                if ((event.ctrlKey || event.metaKey) && !isEditableTarget(event.target)) {
                    event.preventDefault();
                    if (event.shiftKey) {
                        dotnetRef.invokeMethodAsync("OnBringToFrontPressed");
                    } else {
                        dotnetRef.invokeMethodAsync("OnBringForwardPressed");
                    }
                }
                break;
            case "BracketLeft":
                if ((event.ctrlKey || event.metaKey) && !isEditableTarget(event.target)) {
                    event.preventDefault();
                    if (event.shiftKey) {
                        dotnetRef.invokeMethodAsync("OnSendToBackPressed");
                    } else {
                        dotnetRef.invokeMethodAsync("OnSendBackwardPressed");
                    }
                }
                break;
        }
    };

    window.addEventListener('keydown', handleKeyDown);

    return () => {
        window.removeEventListener('keydown', handleKeyDown);
    };
}
