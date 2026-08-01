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

// Ctrl+Tab's own DOM-focus move - targets the Nth currently-focusable tab stop by position, in
// the same document order DiagramCanvas.FocusableTabStopIds computes its own index against
// (every rendered tab stop carries tabindex="0"; a grouped member's container carries none, so
// it's naturally excluded here the same way it's excluded there).
export function focusTabStopAt(container, index) {
    const stops = container.querySelectorAll('[tabindex="0"]');
    if (index >= 0 && index < stops.length) {
        stops[index].focus();
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
            case "ArrowRight":
            case "ArrowUp":
            case "ArrowDown":
                // Doubles as the text cursor's own movement key during inline WYSIWYG editing
                // (a contenteditable host element), so this must not hijack it - same guard as
                // Delete/Ctrl+Z/Ctrl+G below.
                if (!isEditableTarget(event.target)) {
                    // preventDefault unconditionally, even for Alt+Arrow combos the C# side ends up
                    // treating as a no-op (nothing/multiple selected) - Alt+Left/Right is the
                    // browser's own back/forward navigation shortcut, which must never fire here.
                    event.preventDefault();
                    if (event.altKey) {
                        dotnetRef.invokeMethodAsync("OnAltArrowKeyPressed", event.code, event.shiftKey);
                    } else {
                        dotnetRef.invokeMethodAsync("OnArrowKeyPressed", event.code, event.shiftKey);
                    }
                }
                break;
            case "Tab":
                // Plain Tab is never intercepted (native browser traversal, per OrderedTabStops'
                // own reading-order/tabindex setup) - only the exact Ctrl+Tab chord reaches here,
                // moving focus without selecting (see OnCtrlTabPressed). Ctrl+Shift+Tab is left
                // alone rather than treated the same as plain Ctrl+Tab - there's no reverse
                // traversal implemented for it, unlike every other modifier-branching chord below.
                if (
                    (event.ctrlKey || event.metaKey) &&
                    !event.shiftKey &&
                    !isEditableTarget(event.target)
                ) {
                    event.preventDefault();
                    dotnetRef.invokeMethodAsync("OnCtrlTabPressed");
                }
                break;
            case "Space":
                // Doubles as the browser's own default "scroll the page" action on a focused
                // non-form-control element, and as a literal space character while typing during
                // inline WYSIWYG editing - guarded the same way Delete/Backspace are above.
                if (!isEditableTarget(event.target)) {
                    event.preventDefault();
                    dotnetRef.invokeMethodAsync("OnSpacePressed");
                }
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

    // Ends an arrow-key nudge burst (see OnArrowKeyReleased) - a held key fires many rapid
    // repeat keydowns before this fires once on release, so the whole press-to-release span
    // reads as one undoable gesture rather than one entry per repeat.
    const handleKeyUp = (event) => {
        switch (event.code) {
            case "ArrowLeft":
            case "ArrowRight":
            case "ArrowUp":
            case "ArrowDown":
                if (!isEditableTarget(event.target)) {
                    dotnetRef.invokeMethodAsync("OnArrowKeyReleased");
                }
                break;
        }
    };

    window.addEventListener('keydown', handleKeyDown);
    window.addEventListener('keyup', handleKeyUp);

    return () => {
        window.removeEventListener('keydown', handleKeyDown);
        window.removeEventListener('keyup', handleKeyUp);
    };
}
