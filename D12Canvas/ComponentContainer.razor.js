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
