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

// A command's focus handoff - targets the Nth currently-focusable tab stop by position, in
// the same document order DiagramCanvas.FocusableTabStopIds computes its own index against
// (every rendered tab stop carries tabindex="0" and lives in the instance layer; a grouped
// member's container carries none, so it's naturally excluded here the same way it's excluded
// there).
export function focusTabStopAt(container, index) {
    const stops = container.querySelectorAll('.instance-layer [tabindex="0"]');
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

// A primary press on these roles carries an edge end, and its release reports what lies under
// the pointer, since a captured pointerup is targeted at the canvas whatever it is over.
const EDGE_END_ROLES = new Set(["port", "port-strip", "edge-endpoint"]);

// A primary press on these roles carries the hit stack at the press point when Alt is held, and on
// the selection box always, since its click selects what lies beneath it.
const ALT_CYCLE_ROLES = new Set(["instance", "edge", "edge-label"]);

function isNativelyInteractive(element) {
    return element.matches(NATIVELY_INTERACTIVE) || element.isContentEditable === true;
}

// The deepest marked element wins, so nested affordances inside a container beat the container
// with no rule needed, and the entity is the nearest marked ancestor's, so an affordance carries
// the instance it belongs to. An unmarked natively interactive element, or an author's opt-in
// marker, met before any role marker makes the press author content, and whichever of the two
// is met first says whether the press landed on something that takes focus by itself. Content
// inside an instance that is not addressable, one inside a group that is not entered, is the
// instance, which C# marks on the render and this walk only reads. Finding
// nothing before the canvas element means bare canvas.
function classify(target, canvas) {
    let authorContent = null;
    for (let element = target; element && element !== canvas; element = element.parentElement) {
        if (!(element instanceof Element)) {
            break;
        }

        const role = element.getAttribute("data-d12-role");
        if (role) {
            const entity = element.closest("[data-d12-entity]");
            const entityId = entity === null ? null : entity.getAttribute("data-d12-entity");
            if (authorContent === null || element.hasAttribute("data-d12-unaddressable")) {
                return { role, entityId, part: element.getAttribute("data-d12-part"), native: false };
            }

            return {
                role: "author-content",
                entityId,
                part: null,
                native: authorContent === "inferred"
            };
        }

        if (authorContent === null && isNativelyInteractive(element)) {
            authorContent = "inferred";
        } else if (authorContent === null && element.hasAttribute("data-d12-author-content")) {
            authorContent = "marked";
        }
    }

    return { role: "canvas", entityId: null, part: null, native: false };
}

// Every marked element under a point, topmost first, each classified as a press on it would be.
// The browser's own hit test answers, so pointer-events and paint order are respected.
function hitStackAt(canvas, clientX, clientY) {
    const hits = [];
    for (const element of document.elementsFromPoint(clientX, clientY)) {
        if (element === canvas || !canvas.contains(element)) {
            continue;
        }

        const hit = classify(element, canvas);
        const last = hits[hits.length - 1];
        if (
            hit.role === "canvas" ||
            (last !== undefined &&
                last.role === hit.role &&
                last.entityId === hit.entityId &&
                last.part === hit.part)
        ) {
            continue;
        }

        hits.push({ role: hit.role, entityId: hit.entityId, part: hit.part });
    }

    return hits;
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

// With Alt held, a primary press on author content reaches through to the instance unless the
// browser has a reason to keep it: an editable target or a live text selection in the instance.
function altPrimaryCell(event, button, hit) {
    if (button !== PRIMARY_BUTTON || !event.altKey || hit.role !== "author-content") {
        return hit;
    }

    const instance = event.target.closest('[data-d12-role="instance"]') ?? event.target;
    if (isEditableTarget(event.target) || hasLiveTextSelectionInside(instance)) {
        return hit;
    }

    return { role: "instance", entityId: hit.entityId, part: null, native: false };
}

function modifiersOf(event) {
    return {
        shiftKey: event.shiftKey,
        ctrlKey: event.ctrlKey,
        altKey: event.altKey,
        metaKey: event.metaKey
    };
}

function sameModifiers(first, second) {
    return (
        first.shiftKey === second.shiftKey &&
        first.ctrlKey === second.ctrlKey &&
        first.altKey === second.altKey &&
        first.metaKey === second.metaKey
    );
}

function isApplePlatform() {
    const platform = navigator.userAgentData?.platform ?? navigator.platform ?? "";
    return /Mac|iPhone|iPad|iPod/.test(platform);
}

export async function addPointerListener(canvas, container, dotnetRef, options) {
    const classifyPresses = options?.classify !== false;
    const applePlatform = isApplePlatform();
    let press = null;
    let lastPress = null;
    let storedMenuVerdict = null;

    const containerPoint = (event) => {
        const rect = container.getBoundingClientRect();
        return { x: event.clientX - rect.left, y: event.clientY - rect.top };
    };

    // Ctrl+click is the system's secondary click on Apple platforms, so a Ctrl+primary press there
    // is a secondary press, and Ctrl is never a press-time modifier anywhere.
    const buttonOf = (event) =>
        applePlatform && event.button === PRIMARY_BUTTON && event.ctrlKey
            ? SECONDARY_BUTTON
            : event.button;

    // Pointer events carry no click count of their own, so consecutive presses within the usual
    // multi-click window and radius are counted here.
    const pressCountFor = (event, button) => {
        const now = performance.now();
        const count =
            lastPress !== null &&
            now - lastPress.time < MULTI_PRESS_WINDOW_MS &&
            lastPress.button === button &&
            Math.hypot(event.clientX - lastPress.clientX, event.clientY - lastPress.clientY) <
                MULTI_PRESS_RADIUS_PX
                ? lastPress.count + 1
                : 1;
        lastPress = {
            time: now,
            button,
            clientX: event.clientX,
            clientY: event.clientY,
            count
        };
        return count;
    };

    const hitStackFor = (event, button, hit) =>
        button === PRIMARY_BUTTON &&
        (hit.role === "selection-bounds" || (event.altKey && ALT_CYCLE_ROLES.has(hit.role)))
            ? hitStackAt(canvas, event.clientX, event.clientY)
            : null;

    const moveFor = (event) => {
        const point = containerPoint(event);
        return {
            pointerId: event.pointerId,
            x: point.x,
            y: point.y,
            buttons: event.buttons,
            ...modifiersOf(event)
        };
    };

    const pressFor = (event, button, hit) => {
        const point = containerPoint(event);
        return {
            pointerId: event.pointerId,
            button,
            buttons: event.buttons,
            pointerType: event.pointerType,
            role: hit.role,
            entityId: hit.entityId,
            part: hit.part,
            pressCount: pressCountFor(event, button),
            x: point.x,
            y: point.y,
            ...modifiersOf(event),
            hits: hitStackFor(event, button, hit)
        };
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
        const button = buttonOf(event);
        if (button !== SECONDARY_BUTTON) {
            storedMenuVerdict = null;
        }

        // A second button or another pointer while a press is live is dropped, and the live
        // gesture keeps running.
        if (press !== null) {
            return;
        }

        if (button !== PRIMARY_BUTTON && button !== MIDDLE_BUTTON && button !== SECONDARY_BUTTON) {
            return;
        }

        const hit = classifyPresses
            ? altPrimaryCell(event, button, classify(event.target, canvas))
            : { role: "canvas", entityId: null, part: null, native: false };

        // A primary press on author content belongs to the browser: nothing is captured or
        // tracked, so its move and release never reach C#, and C# hears only the press itself.
        // Where an author's marker rather than a control matched, the target cannot take focus,
        // so the browser would hand focus up to the instance's tab stop and select it outright;
        // that press is prevented and focuses the canvas instead.
        if (button === PRIMARY_BUTTON && hit.role === "author-content") {
            if (!hit.native) {
                event.preventDefault();
                canvas.focus({ preventScroll: true });
            }

            dotnetRef.invokeMethodAsync("OnPointerPressed", pressFor(event, button, hit));
            return;
        }

        if (button === SECONDARY_BUTTON) {
            const verdict = menuVerdict(event.target, hit);
            if (verdict === "browser") {
                return;
            }

            storedMenuVerdict = { pointerId: event.pointerId, verdict };
        }

        event.preventDefault();
        canvas.setPointerCapture(event.pointerId);
        canvas.focus({ preventScroll: true });

        const pressed = pressFor(event, button, hit);
        press = {
            pointerId: event.pointerId,
            button,
            physicalButton: event.button,
            carriesEdgeEnd: button === PRIMARY_BUTTON && EDGE_END_ROLES.has(hit.role),
            startClientX: event.clientX,
            startClientY: event.clientY,
            active: false,
            lastMove: moveFor(event),
            pendingMove: null,
            frame: 0
        };

        dotnetRef.invokeMethodAsync("OnPointerPressed", pressed);
    };

    const queueMove = (move) => {
        press.lastMove = move;
        press.pendingMove = move;
        if (!press.frame) {
            press.frame = requestAnimationFrame(flushMove);
        }
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

        queueMove(moveFor(event));
    };

    // A modifier changed with the pointer still is a move from where the pointer last was, so a
    // gesture that reads the modifier live reacts at once. Auto-repeat keydowns carry the same
    // state and send nothing, and a press still under the drag threshold has nothing to re-run.
    // Focus may be anywhere while the pointer is captured, so this listens on the window, ahead of
    // every other handler, and prevents nothing.
    const handleModifierKey = (event) => {
        if (press === null || !press.active) {
            return;
        }

        const modifiers = modifiersOf(event);
        if (sameModifiers(modifiers, press.lastMove)) {
            return;
        }

        queueMove({ ...press.lastMove, ...modifiers });
    };

    // Only the claiming button's release ends the gesture. Any move still waiting for its frame
    // reaches C# before the release does, so the release always follows the last position.
    const handlePointerUp = (event) => {
        if (
            press === null ||
            event.pointerId !== press.pointerId ||
            event.button !== press.physicalButton
        ) {
            return;
        }

        flushMove();
        const point = containerPoint(event);
        const ended = endPress();
        dotnetRef.invokeMethodAsync("OnPointerReleased", {
            pointerId: event.pointerId,
            button: ended.button,
            x: point.x,
            y: point.y,
            ...modifiersOf(event),
            hits: ended.carriesEdgeEnd ? hitStackAt(canvas, event.clientX, event.clientY) : null
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
    window.addEventListener("keydown", handleModifierKey, true);
    window.addEventListener("keyup", handleModifierKey, true);

    return {
        // C# promotes a press still under the threshold when the viewport moves beneath it, and
        // from then on every move is forwarded as for a press that crossed it.
        promote: () => {
            if (press !== null) {
                press.active = true;
            }
        },
        dispose: () => {
            endPress();
            canvas.removeEventListener("pointerdown", handlePointerDown);
            canvas.removeEventListener("pointermove", handlePointerMove);
            canvas.removeEventListener("pointerup", handlePointerUp);
            canvas.removeEventListener("pointercancel", handlePointerCancel);
            canvas.removeEventListener("lostpointercapture", handleLostPointerCapture);
            canvas.removeEventListener("contextmenu", handleContextMenu);
            window.removeEventListener("blur", handleWindowBlur);
            window.removeEventListener("keydown", handleModifierKey, true);
            window.removeEventListener("keyup", handleModifierKey, true);
        }
    };
}

// The canvas captures every wheel event over it, so the host page never scrolls or zooms under
// it. The event's meaning is C#'s; what is decided here is only what C# cannot wait for or has no
// clock to measure. A wheel gesture is a run of events with no idle gap of WHEEL_GESTURE_IDLE_MS,
// ended early where the engine marks a momentum tail, and its granularity is read once from the
// run's first event: a mouse notch arrives in whole pixels or in lines, a trackpad in fractions.
// Deltas are summed per animation frame, which loses nothing because zoom is multiplicative in
// deltaY and pan is additive.
const WHEEL_GESTURE_IDLE_MS = 300;
const WHEEL_LINE_PX = 100 / 3;
const DOM_DELTA_LINE = 1;
const DOM_DELTA_PAGE = 2;

function pixelDeltas(event, container) {
    const unit =
        event.deltaMode === DOM_DELTA_LINE
            ? WHEEL_LINE_PX
            : event.deltaMode === DOM_DELTA_PAGE
              ? container.clientHeight
              : 1;
    return { deltaX: event.deltaX * unit, deltaY: event.deltaY * unit };
}

function isCoarse(event) {
    return (
        event.deltaMode !== 0 || (Number.isInteger(event.deltaX) && Number.isInteger(event.deltaY))
    );
}

export async function addWheelListener(container, dotnetRef) {
    let run = null;
    let pending = null;
    let frame = 0;

    const flush = () => {
        frame = 0;
        const input = pending;
        pending = null;
        if (input !== null) {
            dotnetRef.invokeMethodAsync("OnWheel", input);
        }
    };

    const sameFrame = (input, event, coarse) =>
        input.coarse === coarse &&
        input.shiftKey === event.shiftKey &&
        input.ctrlKey === event.ctrlKey &&
        input.altKey === event.altKey &&
        input.metaKey === event.metaKey;

    const handleWheel = (event) => {
        event.preventDefault();

        const now = performance.now();
        const momentum = event.momentum === true;
        if (
            run === null ||
            now - run.lastTime > WHEEL_GESTURE_IDLE_MS ||
            (run.momentum && !momentum)
        ) {
            run = { coarse: isCoarse(event), lastTime: now, momentum };
        }
        run.lastTime = now;
        run.momentum = momentum;

        const rect = container.getBoundingClientRect();
        const { deltaX, deltaY } = pixelDeltas(event, container);
        if (pending !== null && !sameFrame(pending, event, run.coarse)) {
            flush();
        }

        if (pending === null) {
            pending = {
                x: 0,
                y: 0,
                deltaX: 0,
                deltaY: 0,
                coarse: run.coarse,
                ...modifiersOf(event)
            };
        }

        pending.x = event.clientX - rect.left;
        pending.y = event.clientY - rect.top;
        pending.deltaX += deltaX;
        pending.deltaY += deltaY;

        if (!frame) {
            frame = requestAnimationFrame(flush);
        }
    };

    container.addEventListener("wheel", handleWheel, { passive: false });

    return {
        dispose: () => {
            if (frame) {
                cancelAnimationFrame(frame);
            }
            container.removeEventListener("wheel", handleWheel, { passive: false });
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

// Enter commits a port attachment on one of the canvas's own instance tab stops and enters the
// group on a group's tab stop, so the Enter row is scoped to those two.
function isCanvasTabStopTarget(target) {
    return (
        target instanceof Element &&
        (target.classList.contains("component-container") ||
            target.classList.contains("group-tab-stop"))
    );
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
                    // browser's own back/forward navigation shortcut, which must never fire here,
                    // and for a Ctrl+Shift+Arrow that finds no stop, which Firefox would otherwise
                    // turn into a text selection and a page scroll.
                    event.preventDefault();
                    if ((event.ctrlKey || event.metaKey) && event.shiftKey && !event.altKey) {
                        dotnetRef.invokeMethodAsync("OnDirectionalFocusPressed", event.code);
                    } else if (event.altKey) {
                        dotnetRef.invokeMethodAsync("OnAltArrowKeyPressed", event.code, event.shiftKey);
                    } else {
                        dotnetRef.invokeMethodAsync("OnArrowKeyPressed", event.code, event.shiftKey);
                    }
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
                if (isCanvasTabStopTarget(event.target)) {
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
            case "KeyA":
                if (
                    (event.ctrlKey || event.metaKey) &&
                    !event.shiftKey &&
                    !isEditableTarget(event.target)
                ) {
                    event.preventDefault();
                    dotnetRef.invokeMethodAsync("OnSelectAllPressed");
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

    // Focus has left the container only once it has landed somewhere else on the page. A window
    // losing focus, or a focused stop removed from the DOM, leaves focus nowhere new, so neither
    // counts.
    const handleFocusOut = (event) => {
        if (element.contains(event.relatedTarget)) {
            return;
        }

        const leaving = event.target;
        setTimeout(() => {
            if (
                document.hasFocus() &&
                leaving.isConnected &&
                !element.contains(document.activeElement)
            ) {
                dotnetRef.invokeMethodAsync("OnFocusLeftContainer");
            }
        }, 0);
    };

    window.addEventListener('keydown', handleKeyDown);
    window.addEventListener('keyup', handleKeyUp);
    element.addEventListener('focusout', handleFocusOut);

    // See addResizeListener above - a disposable handle object, not a bare function.
    return {
        dispose: () => {
            window.removeEventListener('keydown', handleKeyDown);
            window.removeEventListener('keyup', handleKeyUp);
            element.removeEventListener('focusout', handleFocusOut);
        }
    };
}
