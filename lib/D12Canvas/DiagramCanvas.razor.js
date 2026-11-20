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
            typeof navigator.clipboard?.read === "function",
        keyLabels: await keyLabels()
    };
}

const HINTED_KEY_CODES = ["KeyA", "KeyD", "KeyG", "KeyL", "BracketLeft", "BracketRight", "Quote"];

async function keyLabels() {
    try {
        const layout = await navigator.keyboard?.getLayoutMap?.();
        if (!layout) {
            return null;
        }

        const labels = {};
        for (const code of HINTED_KEY_CODES) {
            const label = layout.get(code);
            if (label) {
                labels[code] = label;
            }
        }

        return labels;
    } catch {
        return null;
    }
}

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

export function focusGroupTabStop(container) {
    const stop = container.querySelector('.group-tab-stop[aria-selected="true"]');
    if (stop) {
        stop.focus();
    }
}

export function focusTabStopAt(container, index) {
    const stops = container.querySelectorAll('.instance-layer [tabindex="0"]');
    if (index >= 0 && index < stops.length) {
        stops[index].focus({ preventScroll: true });
    }
}

export function focusCanvas(canvas) {
    canvas.focus({ preventScroll: true });
}

const PRIMARY_BUTTON = 0;
const SECONDARY_BUTTON = 2;
const MIDDLE_BUTTON = 1;
const MULTI_PRESS_WINDOW_MS = 500;
const MULTI_PRESS_RADIUS_PX = 5;
const NATIVELY_INTERACTIVE = "input, textarea, button, select, a[href], [tabindex]";

const EDGE_END_ROLES = new Set(["port", "edge-endpoint"]);

const ALT_CYCLE_ROLES = new Set(["instance", "edge", "edge-label"]);

function isNativelyInteractive(element) {
    return element.matches(NATIVELY_INTERACTIVE) || element.isContentEditable === true;
}

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

function lockedPrimaryCell(button, hit) {
    return button === PRIMARY_BUTTON && hit.locked ? CANVAS_HIT : hit;
}

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

const menuVerdictSlots = new WeakMap();

function menuVerdictSlotOf(container) {
    let slot = menuVerdictSlots.get(container);
    if (slot === undefined) {
        slot = { verdict: null, focusedAtMenuKey: null };
        menuVerdictSlots.set(container, slot);
    }

    return slot;
}

export function focusedAtMenuKey(container) {
    return menuVerdictSlots.get(container)?.focusedAtMenuKey ?? null;
}

function isMenuKey(event) {
    return (
        event.code === "ContextMenu" ||
        (event.code === "F10" && event.shiftKey && !event.ctrlKey && !event.altKey && !event.metaKey)
    );
}

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

    const buttonOf = (event) =>
        applePlatform && event.button === PRIMARY_BUTTON && event.ctrlKey
            ? SECONDARY_BUTTON
            : event.button;

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

    const clearMenuVerdict = () => {
        menuVerdictSlot.verdict = null;
    };

    const handlePointerDown = (event) => {
        const button = buttonOf(event);

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

        if (button === PRIMARY_BUTTON && hit.role === "author-content") {
            if (!hit.native) {
                event.preventDefault();
                canvas.focus({ preventScroll: true });
            }

            dotnetRef.invokeMethodAsync("OnPointerPressed", pressFor(event, button, hit));
            return;
        }

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
        }

        dotnetRef.invokeMethodAsync("OnPointerCancelled", reason);
    };

    const handlePointerCancel = (event) => {
        if (press !== null && event.pointerId === press.pointerId) {
            cancelPress("pointercancel");
        }
    };

    const handleLostPointerCapture = (event) => {
        if (press !== null && event.pointerId === press.pointerId) {
            console.error("D12Canvas: pointer capture was lost while a pointer gesture was live.");
            cancelPress("lostpointercapture");
        }
    };

    const handleWindowBlur = () => {
        if (press !== null) {
            cancelPress("blur");
        }
    };

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

function isEditableTarget(target) {
    return (
        target instanceof HTMLInputElement ||
        target instanceof HTMLTextAreaElement ||
        target.isContentEditable
    );
}

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

function keyReachesCanvas(container) {
    if (isNothingFocused()) {
        return !hasLiveTextSelectionOutside(container);
    }

    return container.contains(document.activeElement);
}

function clipboardEventReachesCanvas(container) {
    const active = document.activeElement;
    return (
        active !== null &&
        container.contains(active) &&
        !isEditableTarget(active) &&
        !hasLiveTextSelectionInside(container)
    );
}

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

        if (cut) {
            canvas.focus({ preventScroll: true });
        }
    };

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

    const recordMenuVerdict = (event) => {
        if (event.repeat) {
            return;
        }

        const menuKey = isMenuKey(event) && keyReachesCanvas(element);
        menuVerdictSlot.verdict = menuKey ? keyboardMenuVerdict(event.target, element) : null;
        menuVerdictSlot.focusedAtMenuKey = menuKey ? document.activeElement : null;
    };

    const handleKeyDown = (event) => {
        if (!keyReachesCanvas(element)) {
            return;
        }

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
                if (!isEditableTarget(event.target)) {
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
                if (!isEditableTarget(event.target)) {
                    event.preventDefault();
                    dotnetRef.invokeMethodAsync("OnSpacePressed");
                }
                break;
            case "Escape":
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
            case "Enter": {
                const propertyBarControl =
                    (event.ctrlKey || event.metaKey) &&
                    !event.shiftKey &&
                    !event.altKey &&
                    !isEditableTarget(event.target)
                        ? element.querySelector(".d12-property-bar .d12-property-bar-control")
                        : null;
                if (propertyBarControl !== null) {
                    event.preventDefault();
                    propertyBarControl.focus({ preventScroll: true });
                } else if (isCanvasTabStopTarget(event.target)) {
                    event.preventDefault();
                    dotnetRef.invokeMethodAsync("OnEnterPressed");
                }
                break;
            }
            case "Delete":
            case "Backspace":
                if (!isEditableTarget(event.target)) {
                    event.preventDefault();
                    dotnetRef.invokeMethodAsync("OnDeletePressed");
                }
                break;
            case "KeyZ":
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

    return {
        dispose: () => {
            window.removeEventListener('keydown', recordMenuVerdict, true);
            window.removeEventListener('keydown', handleKeyDown);
            window.removeEventListener('keyup', handleKeyUp);
            element.removeEventListener('focusout', handleFocusOut);
        }
    };
}
