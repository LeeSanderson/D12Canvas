let handler = null;

export function registerClickOutside(containerElement, dotNetHelper) {
    unregisterClickOutside();
    handler = function (event) {
        if (!containerElement || !containerElement.contains(event.target)) {
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

// The click-driven half of focus-follows-selection - a plain click already selects
// (HandleClick), this moves real DOM focus to match. A no-op if the element has no tabindex
// (e.g. a grouped member, which isn't individually focusable).
export function focusElement(element) {
    if (element) {
        element.focus();
    }
}
