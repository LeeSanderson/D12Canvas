// D12Canvas JavaScript Interop functions

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

    // Initial call
    handleResize();
    const resizeObserver = new ResizeObserver(handleResize);
    resizeObserver.observe(element);

    // Return cleanup function
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
        switch (event.code) {
            case "PageUp":
                dotnetRef.invokeMethodAsync("OnZoomIn");
                break;
            case "PageDown":
                dotnetRef.invokeMethodAsync("OnZoomOut");
                break;
            case "ArrowLeft":
                dotnetRef.invokeMethodAsync("OnPanLeft");
                break;
            case "ArrowRight":
                dotnetRef.invokeMethodAsync("OnPanRight");
                break;
            case "ArrowUp":
                dotnetRef.invokeMethodAsync("OnPanUp");
                break;
            case "ArrowDown":
                dotnetRef.invokeMethodAsync("OnPanDown");
                break;
            case "Escape":
                dotnetRef.invokeMethodAsync("OnEscapePressed");
                break;
            case "Delete":
            case "Backspace":
                if (!isEditableTarget(event.target)) {
                    dotnetRef.invokeMethodAsync("OnDeletePressed");
                }
                break;
            case "KeyZ":
                // Ctrl+Z (undo) doubles as the OS/browser's own text-editing undo, so this
                // guards against hijacking it while focus is on an editable host-page element -
                // same reasoning as Delete/Backspace above (ADR 0009's Ctrl+Z/Ctrl+Shift+Z).
                if ((event.ctrlKey || event.metaKey) && !isEditableTarget(event.target)) {
                    if (event.shiftKey) {
                        dotnetRef.invokeMethodAsync("OnRedoPressed");
                    } else {
                        dotnetRef.invokeMethodAsync("OnUndoPressed");
                    }
                }
                break;
        }
        event.preventDefault();
    };

    // Add event listener
    window.addEventListener('keydown', handleKeyDown);

    // Return cleanup function
    return () => {
        window.removeEventListener('keydown', handleKeyDown);
    };
}
