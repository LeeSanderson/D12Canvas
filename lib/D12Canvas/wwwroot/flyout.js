const itemSelector = "[role=menuitem], [role=menuitemradio], [role=menuitemcheckbox]";

export function registerFlyout(flyoutElement, openerElement, dotNetHelper) {
    if (!flyoutElement?.isConnected) {
        return { dispose: () => {} };
    }

    const requestClose = () => dotNetHelper.invokeMethodAsync("RequestClose");

    let outsidePressHeld = false;

    const handlePointerDown = (event) => {
        if (flyoutElement.contains(event.target) || openerElement?.contains(event.target)) {
            return;
        }

        outsidePressHeld = true;
        requestClose();
    };

    const handlePointerUp = () => {
        outsidePressHeld = false;
    };

    const handleKeyDown = (event) => {
        event.stopPropagation();
        switch (event.key) {
            case "Escape":
            case "Tab":
                event.preventDefault();
                requestClose();
                break;
            case "ArrowDown":
                event.preventDefault();
                focusItemBelowOrAbove(flyoutElement, 1);
                break;
            case "ArrowUp":
                event.preventDefault();
                focusItemBelowOrAbove(flyoutElement, -1);
                break;
            case "ArrowRight":
                event.preventDefault();
                focusAdjacentGroup(flyoutElement, 1);
                break;
            case "ArrowLeft":
                event.preventDefault();
                focusAdjacentGroup(flyoutElement, -1);
                break;
            case "Home":
                event.preventDefault();
                items(flyoutElement)[0]?.focus();
                break;
            case "End":
                event.preventDefault();
                items(flyoutElement).at(-1)?.focus();
                break;
        }
    };

    document.addEventListener("pointerdown", handlePointerDown, true);
    document.addEventListener("pointerup", handlePointerUp, true);
    flyoutElement.addEventListener("keydown", handleKeyDown);
    fitInsideClip(flyoutElement);
    (items(flyoutElement)[0] ?? flyoutElement).focus({ preventScroll: true });

    return {
        dispose: () => {
            document.removeEventListener("pointerdown", handlePointerDown, true);
            document.removeEventListener("pointerup", handlePointerUp, true);
            flyoutElement.removeEventListener("keydown", handleKeyDown);
            if (outsidePressHeld) {
                document.addEventListener(
                    "pointerup",
                    () => returnFocus(flyoutElement, openerElement),
                    { capture: true, once: true }
                );
            } else {
                returnFocus(flyoutElement, openerElement);
            }
        }
    };
}

function returnFocus(flyoutElement, openerElement) {
    const active = document.activeElement;
    const focusWasLost =
        active === null ||
        active === document.body ||
        active === document.documentElement ||
        flyoutElement.contains(active);
    if (focusWasLost && openerElement?.isConnected) {
        openerElement.focus({ preventScroll: true });
    }
}

function fitInsideClip(flyoutElement) {
    const unflipped = overflowPastClip(flyoutElement);
    if (unflipped === 0) {
        return;
    }

    flyoutElement.setAttribute("data-d12-flyout-flipped", "");
    if (overflowPastClip(flyoutElement) >= unflipped) {
        flyoutElement.removeAttribute("data-d12-flyout-flipped");
    }
}

function overflowPastClip(element) {
    const clip = clipBox(element);
    const box = element.getBoundingClientRect();
    return Math.max(0, clip.top - box.top) + Math.max(0, box.bottom - clip.bottom);
}

function clipBox(element) {
    let top = 0;
    let bottom = window.innerHeight;
    for (let ancestor = element.parentElement; ancestor; ancestor = ancestor.parentElement) {
        const style = getComputedStyle(ancestor);
        if (style.overflowX === "visible" && style.overflowY === "visible") {
            continue;
        }

        const box = ancestor.getBoundingClientRect();
        top = Math.max(top, box.top);
        bottom = Math.min(bottom, box.bottom);
    }

    return { top, bottom };
}

function items(flyoutElement) {
    return Array.from(flyoutElement.querySelectorAll(itemSelector));
}

function focusAdjacentItem(flyoutElement, direction, from = document.activeElement) {
    const all = items(flyoutElement);
    if (all.length === 0) {
        return;
    }

    const currentIndex = all.indexOf(from);
    const nextIndex =
        currentIndex === -1
            ? direction > 0 ? 0 : all.length - 1
            : (currentIndex + direction + all.length) % all.length;
    all[nextIndex].focus();
}

function focusItemBelowOrAbove(flyoutElement, direction) {
    const grid = document.activeElement?.closest("[data-d12-flyout-columns]");
    if (!grid || !flyoutElement.contains(grid)) {
        focusAdjacentItem(flyoutElement, direction);
        return;
    }

    const columns = Number(grid.dataset.d12FlyoutColumns);
    const cells = Array.from(grid.querySelectorAll(itemSelector));
    const target = cells[cells.indexOf(document.activeElement) + direction * columns];
    if (target) {
        target.focus();
    } else {
        focusAdjacentItem(flyoutElement, direction, direction > 0 ? cells.at(-1) : cells[0]);
    }
}

function focusAdjacentGroup(flyoutElement, direction) {
    const groups = Array.from(flyoutElement.querySelectorAll("[role=group]"));
    if (groups.length === 0) {
        focusAdjacentItem(flyoutElement, direction);
        return;
    }

    const currentGroup = document.activeElement?.closest("[role=group]");
    const groupIndex = groups.indexOf(currentGroup);
    if (groupIndex === -1 || groups.length < 2) {
        return;
    }

    const rowIndex = Array.from(currentGroup.querySelectorAll(itemSelector)).indexOf(
        document.activeElement
    );
    const nextGroup = groups[(groupIndex + direction + groups.length) % groups.length];
    const nextRows = Array.from(nextGroup.querySelectorAll(itemSelector));
    nextRows[Math.min(rowIndex, nextRows.length - 1)]?.focus();
}
