// Ticket 62: dismiss-on-outside-click, the same technique ComponentContainer.razor.js already uses
// for exiting inline-edit mode - a capture-phase mousedown listener on document, so it fires before
// any other handler on the clicked element itself.
let handler = null;

export function registerClickOutside(menuElement, dotNetHelper) {
    unregisterClickOutside();
    handler = function (event) {
        if (!menuElement || !menuElement.contains(event.target)) {
            dotNetHelper.invokeMethodAsync('OnClickOutside');
        }
    };
    document.addEventListener('mousedown', handler, true);
}

export function unregisterClickOutside() {
    if (handler) {
        document.removeEventListener('mousedown', handler, true);
        handler = null;
    }
}

// ADR 0009/role="menu": arrow-key roving focus between this menu's own items - the actual
// keyboard contract role="menuitem" signals to assistive tech, on top of the native Tab order
// every plain <button> already gives for free. Wraps at either end.
export function focusAdjacentItem(menuElement, direction) {
    const items = Array.from(menuElement.querySelectorAll('.d12-context-menu-item'));
    if (items.length === 0) {
        return;
    }

    const currentIndex = items.indexOf(document.activeElement);
    const nextIndex =
        currentIndex === -1 ? 0 : (currentIndex + direction + items.length) % items.length;
    items[nextIndex].focus();
}
