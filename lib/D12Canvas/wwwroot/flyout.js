const itemSelector = "[role=menuitem], [role=menuitemradio], [role=menuitemcheckbox]";

// Keys pressed inside the flyout stop here, so neither the toolbar that holds the opener nor the
// canvas's shortcut listener on window also acts on them. An item's own activation by Enter or
// Space is the button's default action, which stopping propagation leaves alone. A press on the
// opener is left to the opener, whose click closes the flyout itself.
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
            // The outside press that closed the flyout can still move focus once .NET has
            // closed it: its mousedown blurs to the body or focuses what it landed on. Focus is
            // judged once the press ends, so it lands where the press left it.
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

// Focus goes back only when the flyout took it with it: a press that moved focus to another
// control keeps it there.
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

// The owner's placement opens the flyout on one side of its opener and the owner's
// [data-d12-flyout-flipped] rule on the other. The flyout takes the flipped side only when its own
// side runs past the box that clips it and the flipped side runs past it less.
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

// The viewport, narrowed by every ancestor that clips its overflow.
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

// A grid of items (an element with data-d12-flyout-columns) moves by whole rows; leaving its
// top or bottom row steps to the item before or after the grid.
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

// Moves to the same row of the next or previous column, or its last row if it is shorter. A
// flyout without columns moves to the next or previous item instead.
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
