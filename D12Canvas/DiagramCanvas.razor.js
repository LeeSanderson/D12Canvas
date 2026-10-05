export async function getContainerDimensions(element) {
    const rect = element.getBoundingClientRect();
    return {
        width: rect.width,
        height: rect.height,
        left: rect.left,
        top: rect.top
    };
}

// Returns a disposable handle object rather than a bare function - a JS function isn't
// JSON-serializable, so InvokeAsync<IJSObjectReference> would marshal it back as null. A plain
// object with a named "dispose" method is a real, invokable object reference instead.
export async function addResizeListener(element, dotnetRef) {
    const handleResize = () => {
        const rect = element.getBoundingClientRect();
        dotnetRef.invokeMethodAsync("OnContainerResized", rect.width, rect.height);
    };

    handleResize();
    const resizeObserver = new ResizeObserver(handleResize);
    resizeObserver.observe(element);

    return {
        dispose: () => resizeObserver.disconnect()
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

// Every press on the board is classified once, here, by walking up from the event target to the
// nearest marked element, and exactly one owner on the C# side then holds it until its claiming
// button comes up. The decisions that cannot wait for an interop hop are taken synchronously in
// this listener: preventDefault, pointer capture on the canvas element, the single focus write,
// the drag threshold (C# is never called below it) and whether the browser's own context menu is
// suppressed. Moves are coalesced to one call per animation frame. Coordinates cross to C# as
// container-relative screen pixels, converted here so no round trip stands between a press and
// its owner.
const DRAG_THRESHOLD_PX = 4;
const PRIMARY_BUTTON = 0;
const SECONDARY_BUTTON = 2;
const MIDDLE_BUTTON = 1;
const MULTI_PRESS_WINDOW_MS = 500;
const MULTI_PRESS_RADIUS_PX = 5;
const NATIVELY_INTERACTIVE = "input, textarea, button, select, a[href], [tabindex]";

// The primary button on these roles is still served by the per-element handlers of the old
// interaction layer, so the listener takes none of its decisions for such a press and C# chooses
// no gesture. Preventing the pointerdown would suppress the compatibility mouse events those
// handlers rely on. Roles leave this set as their gestures move onto the spine.
const LEGACY_PRIMARY_ROLES = new Set([
    "instance",
    "resize-handle",
    "port",
    "port-strip",
    "edge",
    "edge-endpoint",
    "edge-label",
    "selection-bounds",
    "selection-handle",
    "author-content"
]);

function isNativelyInteractive(element) {
    return element.matches(NATIVELY_INTERACTIVE) || element.isContentEditable === true;
}

// The deepest marked element wins, so nested affordances inside a container beat the container
// with no rule needed, and the entity is the nearest marked ancestor's, so an affordance carries
// the instance it belongs to. An unmarked natively interactive element, or an author's opt-in
// marker, met before any role marker makes the press author content. Finding nothing before the
// canvas element means bare canvas.
function classify(target, canvas) {
    let insideAuthorContent = false;
    for (let element = target; element && element !== canvas; element = element.parentElement) {
        if (!(element instanceof Element)) {
            break;
        }

        const role = element.getAttribute("data-d12-role");
        if (role) {
            const entity = element.closest("[data-d12-entity]");
            return {
                role: insideAuthorContent ? "author-content" : role,
                entityId: entity === null ? null : entity.getAttribute("data-d12-entity"),
                part: insideAuthorContent ? null : element.getAttribute("data-d12-part")
            };
        }

        if (element.hasAttribute("data-d12-author-content") || isNativelyInteractive(element)) {
            insideAuthorContent = true;
        }
    }

    return { role: "canvas", entityId: null, part: null };
}

function hasLiveTextSelectionInside(element) {
    const selection = window.getSelection();
    return (
        selection !== null &&
        !selection.isCollapsed &&
        selection.anchorNode !== null &&
        element.contains(selection.anchorNode)
    );
}

// Who owns a secondary press on author content: the browser keeps its own menu over an editable
// target, a live text selection, a link or a media element; everything else is the canvas's.
function menuVerdict(target, hit) {
    if (hit.role !== "author-content") {
        return "canvas";
    }

    const instance = target.closest('[data-d12-role="instance"]') ?? target;
    if (isEditableTarget(target) || hasLiveTextSelectionInside(instance)) {
        return "browser";
    }

    if (target.closest("a[href], video, audio")) {
        return "browser";
    }

    return "canvas";
}

function modifiersOf(event) {
    return {
        shiftKey: event.shiftKey,
        ctrlKey: event.ctrlKey,
        altKey: event.altKey,
        metaKey: event.metaKey
    };
}

export async function addPointerListener(canvas, container, dotnetRef, options) {
    const classifyPresses = options?.classify !== false;
    let press = null;
    let lastPress = null;
    let storedMenuVerdict = null;

    const containerPoint = (event) => {
        const rect = container.getBoundingClientRect();
        return { x: event.clientX - rect.left, y: event.clientY - rect.top };
    };

    // Pointer events carry no click count of their own, so consecutive presses within the usual
    // multi-click window and radius are counted here.
    const pressCountFor = (event) => {
        const now = performance.now();
        const count =
            lastPress !== null &&
            now - lastPress.time < MULTI_PRESS_WINDOW_MS &&
            lastPress.button === event.button &&
            Math.hypot(event.clientX - lastPress.clientX, event.clientY - lastPress.clientY) <
                MULTI_PRESS_RADIUS_PX
                ? lastPress.count + 1
                : 1;
        lastPress = {
            time: now,
            button: event.button,
            clientX: event.clientX,
            clientY: event.clientY,
            count
        };
        return count;
    };

    const flushMove = () => {
        if (press === null) {
            return;
        }

        press.frame = 0;
        const move = press.pendingMove;
        press.pendingMove = null;
        if (move !== null) {
            dotnetRef.invokeMethodAsync("OnPointerMoved", move);
        }
    };

    const endPress = () => {
        if (press === null) {
            return null;
        }

        if (press.frame) {
            cancelAnimationFrame(press.frame);
        }

        const ended = press;
        press = null;
        return ended;
    };

    const handlePointerDown = (event) => {
        if (event.button !== SECONDARY_BUTTON) {
            storedMenuVerdict = null;
        }

        // A second button or another pointer while a press is live is dropped, and the live
        // gesture keeps running.
        if (press !== null) {
            return;
        }

        if (
            event.button !== PRIMARY_BUTTON &&
            event.button !== MIDDLE_BUTTON &&
            event.button !== SECONDARY_BUTTON
        ) {
            return;
        }

        const hit = classifyPresses
            ? classify(event.target, canvas)
            : { role: "canvas", entityId: null, part: null };

        if (event.button === PRIMARY_BUTTON && LEGACY_PRIMARY_ROLES.has(hit.role)) {
            return;
        }

        if (event.button === SECONDARY_BUTTON) {
            const verdict = menuVerdict(event.target, hit);
            if (verdict === "browser") {
                return;
            }

            storedMenuVerdict = { pointerId: event.pointerId, verdict };
        }

        event.preventDefault();
        canvas.setPointerCapture(event.pointerId);
        canvas.focus({ preventScroll: true });

        const point = containerPoint(event);
        press = {
            pointerId: event.pointerId,
            button: event.button,
            startClientX: event.clientX,
            startClientY: event.clientY,
            active: false,
            pendingMove: null,
            frame: 0
        };

        dotnetRef.invokeMethodAsync("OnPointerPressed", {
            pointerId: event.pointerId,
            button: event.button,
            buttons: event.buttons,
            pointerType: event.pointerType,
            role: hit.role,
            entityId: hit.entityId,
            part: hit.part,
            pressCount: pressCountFor(event),
            x: point.x,
            y: point.y,
            ...modifiersOf(event)
        });
    };

    const handlePointerMove = (event) => {
        if (press === null || event.pointerId !== press.pointerId) {
            return;
        }

        if (!press.active) {
            const distance = Math.hypot(
                event.clientX - press.startClientX,
                event.clientY - press.startClientY
            );
            if (distance < DRAG_THRESHOLD_PX) {
                return;
            }

            press.active = true;
        }

        const point = containerPoint(event);
        press.pendingMove = {
            pointerId: event.pointerId,
            x: point.x,
            y: point.y,
            buttons: event.buttons,
            ...modifiersOf(event)
        };

        if (!press.frame) {
            press.frame = requestAnimationFrame(flushMove);
        }
    };

    // Only the claiming button's release ends the gesture. Any move still waiting for its frame
    // reaches C# before the release does, so the release always follows the last position.
    const handlePointerUp = (event) => {
        if (
            press === null ||
            event.pointerId !== press.pointerId ||
            event.button !== press.button
        ) {
            return;
        }

        flushMove();
        const point = containerPoint(event);
        endPress();
        dotnetRef.invokeMethodAsync("OnPointerReleased", {
            pointerId: event.pointerId,
            button: event.button,
            x: point.x,
            y: point.y,
            ...modifiersOf(event)
        });
    };

    const cancelPress = (reason) => {
        const ended = endPress();
        if (ended === null) {
            return;
        }

        try {
            canvas.releasePointerCapture(ended.pointerId);
        } catch {
            // Capture was already gone, which is exactly the case lostpointercapture reports.
        }

        dotnetRef.invokeMethodAsync("OnPointerCancelled", reason);
    };

    const handlePointerCancel = (event) => {
        if (press !== null && event.pointerId === press.pointerId) {
            cancelPress("pointercancel");
        }
    };

    // On a normal release pointerup has already cleared the press, so the net finds nothing.
    // Finding a live press here is a leak, and it is made loud on purpose.
    const handleLostPointerCapture = (event) => {
        if (press !== null && event.pointerId === press.pointerId) {
            console.error("D12Canvas: pointer capture was lost while a pointer gesture was live.");
            cancelPress("lostpointercapture");
        }
    };

    // The release happens in another window and never arrives, so the press ends here.
    const handleWindowBlur = () => {
        if (press !== null) {
            cancelPress("blur");
        }
    };

    // The verdict was taken at the press, because on Windows contextmenu fires after pointerup at
    // whatever sits under the release point. The first contextmenu after the press uses it up.
    const handleContextMenu = (event) => {
        const verdict = storedMenuVerdict;
        storedMenuVerdict = null;
        if (verdict !== null && verdict.verdict === "canvas") {
            event.preventDefault();
        }
    };

    canvas.addEventListener("pointerdown", handlePointerDown);
    canvas.addEventListener("pointermove", handlePointerMove);
    canvas.addEventListener("pointerup", handlePointerUp);
    canvas.addEventListener("pointercancel", handlePointerCancel);
    canvas.addEventListener("lostpointercapture", handleLostPointerCapture);
    canvas.addEventListener("contextmenu", handleContextMenu);
    window.addEventListener("blur", handleWindowBlur);

    return {
        dispose: () => {
            endPress();
            canvas.removeEventListener("pointerdown", handlePointerDown);
            canvas.removeEventListener("pointermove", handlePointerMove);
            canvas.removeEventListener("pointerup", handlePointerUp);
            canvas.removeEventListener("pointercancel", handlePointerCancel);
            canvas.removeEventListener("lostpointercapture", handleLostPointerCapture);
            canvas.removeEventListener("contextmenu", handleContextMenu);
            window.removeEventListener("blur", handleWindowBlur);
        }
    };
}

// The per-row typing guard. Keys such as Backspace, Delete and the arrows double as text-editing
// keys, so those rows stay out of the way of an editable element inside the canvas (an inline
// edit, or an author's own input); the focus guard has already kept out the rest of the page.
function isEditableTarget(target) {
    return (
        target instanceof HTMLInputElement ||
        target instanceof HTMLTextAreaElement ||
        target.isContentEditable
    );
}

// Enter means "commit this port attachment", which is defined only on one of the canvas's own
// instance tab stops, so the Enter row is scoped to them.
function isComponentContainerTarget(target) {
    return target instanceof Element && target.classList.contains("component-container");
}

function isNothingFocused() {
    const active = document.activeElement;
    return active === null || active === document.body || active === document.documentElement;
}

function hasLiveTextSelectionOutside(container) {
    const selection = window.getSelection();
    return (
        selection !== null &&
        !selection.isCollapsed &&
        selection.anchorNode !== null &&
        !container.contains(selection.anchorNode)
    );
}

// The one focus guard every row sits behind. A key reaches the canvas when focus is inside its
// container, or when nothing is focused and the user has not selected text elsewhere on the page,
// since selecting text never moves focus off the body.
function keyReachesCanvas(container) {
    if (isNothingFocused()) {
        return !hasLiveTextSelectionOutside(container);
    }

    return container.contains(document.activeElement);
}

export async function addKeyboardListener(element, dotnetRef) {
    const handleKeyDown = (event) => {
        if (!keyReachesCanvas(element)) {
            return;
        }

        // preventDefault is called only from inside a branch that actually invokes a
        // dotnetRef method - never unconditionally after the switch. Tab (native browser focus
        // navigation) and every other unhandled key must reach the browser's own default
        // handling.
        switch (event.code) {
            case "PageUp":
                if (!isEditableTarget(event.target)) {
                    event.preventDefault();
                    dotnetRef.invokeMethodAsync("OnZoomIn");
                }
                break;
            case "PageDown":
                if (!isEditableTarget(event.target)) {
                    event.preventDefault();
                    dotnetRef.invokeMethodAsync("OnZoomOut");
                }
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
            case "Enter":
                if (isComponentContainerTarget(event.target)) {
                    event.preventDefault();
                    dotnetRef.invokeMethodAsync("OnEnterPressed");
                }
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
            case "Quote":
                if ((event.ctrlKey || event.metaKey) && !isEditableTarget(event.target)) {
                    event.preventDefault();
                    dotnetRef.invokeMethodAsync("OnSnapToGridChordPressed");
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

    // See addResizeListener above - a disposable handle object, not a bare function.
    return {
        dispose: () => {
            window.removeEventListener('keydown', handleKeyDown);
            window.removeEventListener('keyup', handleKeyUp);
        }
    };
}
