import { holdImages, takeImageBytes, chooseImageFile } from "./imageFiles.js";

export { takeImageBytes, chooseImageFile };

export async function getContainerDimensions(element) {
    const rect = element.getBoundingClientRect();
    return {
        width: rect.width,
        height: rect.height,
        left: rect.left,
        top: rect.top
    };
}

export async function initialFacts(element) {
    const rect = element.getBoundingClientRect();
    return {
        width: rect.width,
        height: rect.height,
        applePlatform: isApplePlatform(),
        asyncClipboard:
            window.isSecureContext &&
            typeof navigator.clipboard?.writeText === "function" &&
            typeof navigator.clipboard?.read === "function"
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
        stops[index].focus({ preventScroll: true });
    }
}

export function focusCanvas(canvas) {
    canvas.focus({ preventScroll: true });
}

// Every press on the board is classified once, here, by walking up from the event target to the
// nearest marked element, and exactly one owner on the C# side then holds it until its claiming
// button comes up. The decisions that cannot wait for an interop hop are taken synchronously in
// this listener: preventDefault, pointer capture on the canvas element, the single focus write,
// the drag threshold (C# is never called below it) and whether the browser's own context menu is
// suppressed. Moves are coalesced to one call per animation frame, each carrying how fast the
// pointer was travelling when it got there. Coordinates cross to C# as container-relative screen
// pixels, converted here so no round trip stands between a press and its owner.
const PRIMARY_BUTTON = 0;
const SECONDARY_BUTTON = 2;
const MIDDLE_BUTTON = 1;
const MULTI_PRESS_WINDOW_MS = 500;
const MULTI_PRESS_RADIUS_PX = 5;
const NATIVELY_INTERACTIVE = "input, textarea, button, select, a[href], [tabindex]";

// A primary press on these roles carries an edge end, and each of its moves and its release report
// what lies under the pointer, since captured pointer events are targeted at the canvas whatever
// they are over.
const EDGE_END_ROLES = new Set(["port", "edge-endpoint"]);

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
            const locked = element.closest("[data-d12-locked]") !== null;
            if (authorContent === null || element.hasAttribute("data-d12-unaddressable") || locked) {
                return {
                    role,
                    entityId,
                    part: element.getAttribute("data-d12-part"),
                    native: false,
                    locked
                };
            }

            return {
                role: "author-content",
                entityId,
                part: null,
                native: authorContent === "inferred",
                locked
            };
        }

        if (authorContent === null && isNativelyInteractive(element)) {
            authorContent = "inferred";
        } else if (authorContent === null && element.hasAttribute("data-d12-author-content")) {
            authorContent = "marked";
        }
    }

    return CANVAS_HIT;
}

const CANVAS_HIT = Object.freeze({
    role: "canvas",
    entityId: null,
    part: null,
    native: false,
    locked: false
});

// A locked entity takes no primary press, which lands on the canvas instead; the secondary button
// still reaches it so its menu can offer Unlock.
function lockedPrimaryCell(button, hit) {
    return button === PRIMARY_BUTTON && hit.locked ? CANVAS_HIT : hit;
}

// Every marked element under a point, topmost first, each classified as a press on it would be.
// The browser's own hit test answers, so pointer-events and paint order are respected. A locked
// entity is left out, as a primary press passes it by.
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
            hit.locked ||
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

const MENU_VERDICTS = new Set(["browser", "canvas"]);

// The entity whose own content the target sits in, when that entity can be addressed: author
// content, or anything inside an addressable instance's content box. A press that only reaches an
// instance for the primary button, such as an img an author marked for the browser's menu, is
// still content for the secondary one. Null for the canvas's own parts and for a member of a group
// that is not entered.
function contentEntityOf(target, hit) {
    const entity = target.closest("[data-d12-role]");
    if (entity === null || entity === target) {
        return null;
    }

    if (hit.role === "author-content") {
        return entity;
    }

    const content = target.closest(".container-content");
    return hit.role === "instance" &&
        !entity.hasAttribute("data-d12-unaddressable") &&
        content !== null &&
        entity.contains(content)
        ? entity
        : null;
}

// Who owns a menu request on an entity's content, the browser or the canvas. The first rule that
// matches wins. Content the press cannot address is the canvas's. An editable target or a live
// text selection is the browser's, whatever any author says. Then the nearest author marker inside
// the entity decides. Then links and media are the browser's, since their menus hold items found
// nowhere else. Everything else, including a bare img, is the canvas's.
function menuVerdict(target, hit) {
    const entity = contentEntityOf(target, hit);
    if (entity === null) {
        return "canvas";
    }

    if (isEditableTarget(target) || hasLiveTextSelectionInside(entity)) {
        return "browser";
    }

    for (let element = target; element && element !== entity; element = element.parentElement) {
        const marker = element.getAttribute("data-d12-context-menu");
        if (MENU_VERDICTS.has(marker)) {
            return marker;
        }
    }

    const inferred = target.closest("a[href], video, audio");
    if (inferred !== null && entity.contains(inferred)) {
        return "browser";
    }

    return "canvas";
}

// A menu key classifies whatever holds focus. Inside an entity's content that is the five rules;
// on anything else in the container, a tab stop, a menu row or the canvas itself, only an editable
// target or a live text selection keeps the browser's menu.
function keyboardMenuVerdict(target, container) {
    if (!(target instanceof Element)) {
        return "canvas";
    }

    const hit = classify(target, container);
    if (contentEntityOf(target, hit) !== null) {
        return menuVerdict(target, hit);
    }

    return isEditableTarget(target) || hasLiveTextSelectionInside(container) ? "browser" : "canvas";
}

// One stored Menu verdict per canvas, shared by the pointer and keyboard paths: a secondary press
// or a menu keydown writes it, and the first contextmenu after it uses it up.
const menuVerdictSlots = new WeakMap();

function menuVerdictSlotOf(container) {
    let slot = menuVerdictSlots.get(container);
    if (slot === undefined) {
        slot = { verdict: null };
        menuVerdictSlots.set(container, slot);
    }

    return slot;
}

function isMenuKey(event) {
    return (
        event.code === "ContextMenu" ||
        (event.code === "F10" && event.shiftKey && !event.ctrlKey && !event.altKey && !event.metaKey)
    );
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

// There is no one clean API for this: userAgentData is Chromium-only and navigator.platform is
// deprecated, so the first falls back to the second. Read once and kept, so the pointer path and
// the shortcut hints C# draws always agree.
let applePlatform = null;

function isApplePlatform() {
    if (applePlatform === null) {
        const platform = navigator.userAgentData?.platform ?? navigator.platform ?? "";
        applePlatform = /Mac|iPhone|iPad|iPod/.test(platform);
    }

    return applePlatform;
}

const FLIGHT_CLASS = "d12-in-flight";

function preventDefaultOf(event) {
    event.preventDefault();
}

// With classify off, as on the minimap, only the primary button presses and the others are
// swallowed along with the browser's menu, nothing is classified, no flight is watched, and the
// one focus write goes to options.focusTarget, the canvas the press pans, so Escape reaches it.
export async function addPointerListener(canvas, container, dotnetRef, options) {
    const classifyPresses = options?.classify !== false;
    const dragThreshold = options.dragThreshold;
    const focusTarget = options.focusTarget ?? canvas;
    const applePlatform = isApplePlatform();
    let press = null;
    let lastPress = null;
    const menuVerdictSlot = menuVerdictSlotOf(container);

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

    const moveFor = (event, velocity = 0) => {
        const point = containerPoint(event);
        return {
            pointerId: event.pointerId,
            x: point.x,
            y: point.y,
            buttons: event.buttons,
            ...modifiersOf(event),
            velocity
        };
    };

    // Screen pixels per millisecond since the pointer's previous event. Two events stamped in the
    // same millisecond keep the last speed rather than dividing by nothing.
    const velocityAt = (event) => {
        const previous = press.lastSample;
        const elapsed = event.timeStamp - previous.time;
        const velocity =
            elapsed > 0
                ? Math.hypot(event.clientX - previous.clientX, event.clientY - previous.clientY) /
                  elapsed
                : previous.velocity;
        press.lastSample = {
            clientX: event.clientX,
            clientY: event.clientY,
            time: event.timeStamp,
            velocity
        };
        return velocity;
    };

    const pressFor = (event, button, hit, menuVerdict = null) => {
        const point = containerPoint(event);
        return {
            menuVerdict,
            pointerId: event.pointerId,
            button,
            buttons: event.buttons,
            pointerType: event.pointerType,
            role: hit.role,
            entityId: hit.entityId,
            part: hit.part,
            locked: hit.locked,
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

    // Every press anywhere on the page clears the stored verdict before anything else sees it,
    // including one the open menu consumes and one on the host's own markup, so a verdict nothing
    // used up cannot decide a later contextmenu. A secondary press on the canvas writes its own
    // straight after.
    const clearMenuVerdict = () => {
        menuVerdictSlot.verdict = null;
    };

    const handlePointerDown = (event) => {
        const button = buttonOf(event);

        // A second button or another pointer while a press is live is dropped, and the live
        // gesture keeps running.
        if (press !== null) {
            return;
        }

        if (button !== PRIMARY_BUTTON && button !== MIDDLE_BUTTON && button !== SECONDARY_BUTTON) {
            return;
        }

        if (!classifyPresses && button !== PRIMARY_BUTTON) {
            event.preventDefault();
            return;
        }

        const hit = classifyPresses
            ? lockedPrimaryCell(button, altPrimaryCell(event, button, classify(event.target, canvas)))
            : CANVAS_HIT;

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

        // A secondary press is the browser's or the canvas's by its Menu verdict, taken here and
        // kept for the contextmenu this press fires, since on Windows that fires after pointerup
        // at whatever sits under the release point. The browser's press is handled as a primary
        // press on author content is: no capture, nothing prevented, and C# hears only the press.
        let verdict = null;
        if (button === SECONDARY_BUTTON) {
            verdict = hit.locked ? "canvas" : menuVerdict(event.target, hit);
            menuVerdictSlot.verdict = verdict;
            if (verdict === "browser") {
                dotnetRef.invokeMethodAsync("OnPointerPressed", pressFor(event, button, hit, verdict));
                return;
            }
        }

        event.preventDefault();
        canvas.setPointerCapture(event.pointerId);
        focusTarget.focus({ preventScroll: true });

        const pressed = pressFor(event, button, hit, verdict);
        press = {
            pointerId: event.pointerId,
            button,
            physicalButton: event.button,
            carriesEdgeEnd: button === PRIMARY_BUTTON && EDGE_END_ROLES.has(hit.role),
            startClientX: event.clientX,
            startClientY: event.clientY,
            active: false,
            lastMove: moveFor(event),
            lastSample: {
                clientX: event.clientX,
                clientY: event.clientY,
                time: event.timeStamp,
                velocity: 0
            },
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

        const velocity = velocityAt(event);
        if (!press.active) {
            const distance = Math.hypot(
                event.clientX - press.startClientX,
                event.clientY - press.startClientY
            );
            if (distance < dragThreshold) {
                return;
            }

            press.active = true;
        }

        const move = moveFor(event, velocity);
        queueMove(
            press.carriesEdgeEnd
                ? { ...move, hits: hitStackAt(canvas, event.clientX, event.clientY) }
                : move
        );
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

        queueMove({ ...press.lastMove, ...modifiers, velocity: 0 });
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

    // The first contextmenu after a secondary press or a menu keydown uses up the verdict that
    // request stored and classifies nothing itself: after a menu key its target may already be a
    // row of the menu the key opened, or the page body when nothing held focus. It listens on the
    // window for that reason. One with nothing in front of it only answers for the container, and
    // classifies its own target as a menu key would.
    const handleContextMenu = (event) => {
        const stored = menuVerdictSlot.verdict;
        menuVerdictSlot.verdict = null;
        const verdict =
            stored ??
            (container.contains(event.target)
                ? keyboardMenuVerdict(event.target, container)
                : "browser");
        if (verdict === "canvas") {
            event.preventDefault();
        }
    };

    // A framing flight moves C#'s state at once while the pixels are still in transit, so a press
    // during it would land on what is about to be under the pointer. Pointer events are off on the
    // container from the flight's transitionrun to its end or cancel, which a zero duration never
    // starts. A held press keeps them, since every other button is already dropped.
    const isContentTransform = (event) =>
        event.target instanceof Element &&
        event.target.classList.contains("canvas-content") &&
        event.propertyName === "transform";

    const handleTransitionRun = (event) => {
        if (isContentTransform(event) && event.target.hasAttribute("data-d12-flight") && press === null) {
            container.classList.add(FLIGHT_CLASS);
        }
    };

    const handleTransitionStop = (event) => {
        if (!isContentTransform(event)) {
            return;
        }

        const stillFlying =
            event.target.hasAttribute("data-d12-flight") && event.target.getAnimations().length > 0;
        if (!stillFlying) {
            container.classList.remove(FLIGHT_CLASS);
        }
    };

    canvas.addEventListener("pointerdown", handlePointerDown);
    canvas.addEventListener("pointermove", handlePointerMove);
    canvas.addEventListener("pointerup", handlePointerUp);
    canvas.addEventListener("pointercancel", handlePointerCancel);
    canvas.addEventListener("lostpointercapture", handleLostPointerCapture);
    if (classifyPresses) {
        canvas.addEventListener("transitionrun", handleTransitionRun);
        canvas.addEventListener("transitionend", handleTransitionStop);
        canvas.addEventListener("transitioncancel", handleTransitionStop);
        window.addEventListener("pointerdown", clearMenuVerdict, true);
        window.addEventListener("contextmenu", handleContextMenu, true);
    } else {
        canvas.addEventListener("contextmenu", preventDefaultOf);
    }
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
            if (classifyPresses) {
                canvas.removeEventListener("transitionrun", handleTransitionRun);
                canvas.removeEventListener("transitionend", handleTransitionStop);
                canvas.removeEventListener("transitioncancel", handleTransitionStop);
                container.classList.remove(FLIGHT_CLASS);
                window.removeEventListener("pointerdown", clearMenuVerdict, true);
                window.removeEventListener("contextmenu", handleContextMenu, true);
            } else {
                canvas.removeEventListener("contextmenu", preventDefaultOf);
            }
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

// Clipboard keys go to the canvas only while focus is inside its container and not in an editable
// element, and only when no text inside it is selected, so the page's own copy and paste and an
// author's selected text are left to the browser.
function clipboardEventReachesCanvas(container) {
    const active = document.activeElement;
    return (
        active !== null &&
        container.contains(active) &&
        !isEditableTarget(active) &&
        !hasLiveTextSelectionInside(container)
    );
}

// Ctrl+C, Ctrl+X and Ctrl+V are the browser's copy, cut and paste events rather than keydown rows,
// because a paste event's clipboardData is the one read that needs no permission, and setData
// inside a copy event needs none either. setData only works while the event is being dispatched,
// so the payload is asked for synchronously. A host where .NET cannot answer synchronously writes
// through the async clipboard instead, which needs a secure context.
export function addClipboardListener(container, canvas, dotnetRef) {
    let pointer = null;

    const trackPointer = (event) => {
        const rect = container.getBoundingClientRect();
        pointer = { x: event.clientX - rect.left, y: event.clientY - rect.top };
    };

    const forgetPointer = () => {
        pointer = null;
    };

    const writeAsynchronously = async (cut) => {
        const payload = await dotnetRef.invokeMethodAsync("OnCopyRequested");
        if (payload === null || !(await writeClipboardText(payload))) {
            return;
        }

        if (cut) {
            await dotnetRef.invokeMethodAsync("OnCutRequested");
        }
    };

    const handleCopyOrCut = (event) => {
        if (!clipboardEventReachesCanvas(container) || event.clipboardData === null) {
            return;
        }

        const cut = event.type === "cut";
        let payload;
        try {
            payload = dotnetRef.invokeMethod(cut ? "OnCutRequested" : "OnCopyRequested");
        } catch {
            writeAsynchronously(cut);
            return;
        }

        if (payload === null || payload === undefined) {
            return;
        }

        event.clipboardData.setData("text/plain", payload);
        event.preventDefault();

        // A cut can remove the very stop that holds focus, once its render lands, which would drop
        // focus to the body and leave the Ctrl+V that usually follows with nowhere to land.
        if (cut) {
            canvas.focus({ preventScroll: true });
        }
    };

    // A bitmap on the clipboard wins over any text beside it, which for a copied picture is
    // usually its address or markup rather than anything worth a text shape.
    const handlePaste = (event) => {
        if (!clipboardEventReachesCanvas(container) || event.clipboardData === null) {
            return;
        }

        const anchor = pointer;
        const images = Array.from(event.clipboardData.files).filter((file) =>
            file.type.startsWith("image/")
        );
        if (images.length > 0) {
            event.preventDefault();
            holdImages(images).then((held) => {
                if (held.length > 0) {
                    dotnetRef.invokeMethodAsync("OnImagesPasted", held, anchor?.x ?? null, anchor?.y ?? null);
                }
            });
            return;
        }

        const text = event.clipboardData.getData("text/plain");
        if (text.trim() === "") {
            return;
        }

        event.preventDefault();
        dotnetRef.invokeMethodAsync("OnPasteReceived", text, anchor?.x ?? null, anchor?.y ?? null);
    };

    canvas.addEventListener("pointermove", trackPointer);
    canvas.addEventListener("pointerleave", forgetPointer);
    document.addEventListener("copy", handleCopyOrCut);
    document.addEventListener("cut", handleCopyOrCut);
    document.addEventListener("paste", handlePaste);

    return {
        dispose: () => {
            canvas.removeEventListener("pointermove", trackPointer);
            canvas.removeEventListener("pointerleave", forgetPointer);
            document.removeEventListener("copy", handleCopyOrCut);
            document.removeEventListener("cut", handleCopyOrCut);
            document.removeEventListener("paste", handlePaste);
        }
    };
}

// The menu's routes. A click fires no clipboard event, so a row can only use the async clipboard;
// the click's user activation is still live when the payload comes back from .NET.
export async function writeClipboardText(text) {
    try {
        await navigator.clipboard.writeText(text);
        return true;
    } catch {
        return false;
    }
}

export async function readClipboardImages() {
    try {
        const blobs = [];
        for (const item of await navigator.clipboard.read()) {
            const type = item.types.find((candidate) => candidate.startsWith("image/"));
            if (type !== undefined) {
                blobs.push(await item.getType(type));
            }
        }

        return await holdImages(blobs);
    } catch {
        return [];
    }
}

// A file dragged in from outside the page carries "Files" in its types, which a palette drag never
// does, so the palette's own drop path is left alone. The browser's default for a file drop, which
// would navigate away to the file, is already prevented on the canvas. The entities under the drop
// point go along topmost first, so .NET can tell whether an empty image is what the file landed on.
export function addFileDropListener(canvas, container, dotnetRef) {
    const carriesFiles = (event) => event.dataTransfer?.types.includes("Files") ?? false;

    const handleDrop = (event) => {
        if (!carriesFiles(event)) {
            return;
        }

        const rect = container.getBoundingClientRect();
        const x = event.clientX - rect.left;
        const y = event.clientY - rect.top;
        const hits = [];
        for (const element of document.elementsFromPoint(event.clientX, event.clientY)) {
            const entity = container.contains(element)
                ? element.closest("[data-d12-entity]")?.getAttribute("data-d12-entity")
                : null;
            if (entity && !hits.includes(entity)) {
                hits.push(entity);
            }
        }

        holdImages(Array.from(event.dataTransfer.files)).then((held) => {
            if (held.length > 0) {
                dotnetRef.invokeMethodAsync("OnImageFilesDropped", held, x, y, hits);
            }
        });
    };

    canvas.addEventListener("drop", handleDrop);

    return {
        dispose: () => canvas.removeEventListener("drop", handleDrop)
    };
}

export async function readClipboardText() {
    try {
        for (const item of await navigator.clipboard.read()) {
            if (item.types.includes("text/plain")) {
                return await (await item.getType("text/plain")).text();
            }
        }
    } catch {
        // Refused or unavailable: the paste does nothing.
    }

    return null;
}

const FRAMING_COMMANDS = {
    Digit1: "ZoomToFit",
    Digit2: "ZoomToSelection",
    Digit0: "ZoomTo100Percent"
};

export async function addKeyboardListener(element, dotnetRef) {
    const menuVerdictSlot = menuVerdictSlotOf(element);

    // Runs in the capture phase on window, ahead of any author's or built-in's stopPropagation, so
    // a menu key pressed in an editor that swallows its keys still records the browser's verdict.
    // Every other key clears the slot, so a verdict nothing used up cannot strand a later request.
    // A repeat does neither, so a held key keeps the verdict its keyup contextmenu will use.
    const recordMenuVerdict = (event) => {
        if (event.repeat) {
            return;
        }

        menuVerdictSlot.verdict =
            isMenuKey(event) && keyReachesCanvas(element)
                ? keyboardMenuVerdict(event.target, element)
                : null;
    };

    const handleKeyDown = (event) => {
        if (!keyReachesCanvas(element)) {
            return;
        }

        // The menu keys act on keydown, the ContextMenu key included, and only on the canvas's
        // verdict; the browser's verdict leaves the key alone so its own menu follows. The verdict
        // does the typing guard's job for this row. A held key opens one menu, and its repeats
        // are prevented wherever the canvas would own them, so none of them shows the browser's.
        if (isMenuKey(event)) {
            if (event.repeat) {
                if (keyboardMenuVerdict(event.target, element) === "canvas") {
                    event.preventDefault();
                }
            } else if (menuVerdictSlot.verdict === "canvas") {
                event.preventDefault();
                dotnetRef.invokeMethodAsync("OnContextMenuKeyPressed");
            }
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
                    } else if ((event.ctrlKey || event.metaKey) && !event.shiftKey && !event.altKey) {
                        dotnetRef.invokeMethodAsync("OnQuickCreatePressed", event.code);
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
                // An inline editor ends its own edit on Escape; the next Escape reaches the canvas.
                if (!isEditableTarget(event.target)) {
                    event.preventDefault();
                    dotnetRef.invokeMethodAsync("OnEscapePressed");
                }
                break;
            case "F2":
                if (isCanvasTabStopTarget(event.target)) {
                    event.preventDefault();
                    dotnetRef.invokeMethodAsync("OnBeginEditPressed");
                }
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
            case "KeyD":
                if (
                    (event.ctrlKey || event.metaKey) &&
                    !event.shiftKey &&
                    !event.altKey &&
                    !isEditableTarget(event.target)
                ) {
                    event.preventDefault();
                    dotnetRef.invokeMethodAsync("OnDuplicatePressed");
                }
                break;
            case "KeyL":
                if (
                    (event.ctrlKey || event.metaKey) &&
                    event.shiftKey &&
                    !event.altKey &&
                    !isEditableTarget(event.target)
                ) {
                    event.preventDefault();
                    dotnetRef.invokeMethodAsync("OnToggleLockPressed");
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
            case "Digit1":
            case "Digit2":
            case "Digit0":
                // Matched on the physical key, so the chord holds on any layout. Stricter than the
                // table's guard: an embedded canvas that does not hold focus leaves the page its
                // Shift+1.
                if (
                    event.shiftKey &&
                    !event.ctrlKey &&
                    !event.metaKey &&
                    !event.altKey &&
                    element.contains(document.activeElement) &&
                    !isEditableTarget(event.target)
                ) {
                    event.preventDefault();
                    dotnetRef.invokeMethodAsync(FRAMING_COMMANDS[event.code]);
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

    window.addEventListener('keydown', recordMenuVerdict, true);
    window.addEventListener('keydown', handleKeyDown);
    window.addEventListener('keyup', handleKeyUp);
    element.addEventListener('focusout', handleFocusOut);

    // See addResizeListener above - a disposable handle object, not a bare function.
    return {
        dispose: () => {
            window.removeEventListener('keydown', recordMenuVerdict, true);
            window.removeEventListener('keydown', handleKeyDown);
            window.removeEventListener('keyup', handleKeyUp);
            element.removeEventListener('focusout', handleFocusOut);
        }
    };
}
