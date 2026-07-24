// D12Canvas JavaScript Interop functions

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

    // Initial call
    handleResize();
    const resizeObserver = new ResizeObserver(handleResize);
    resizeObserver.observe(element);

    // Return cleanup function
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

export async function addKeyboardListener(element, dotnetRef) {
    const handleKeyDown = (event) => {
        switch (event.code) {
            case "PageUp":
                dotnetRef.invokeMethodAsync("OnZoomIn");
                break;
            case "PageDown":
                dotnetRef.invokeMethodAsync("OnZoomOut");
                break;
            case "ArrowLeft":
                dotnetRef.invokeMethodAsync("OnPanLeft");
                break;
            case "ArrowRight":
                dotnetRef.invokeMethodAsync("OnPanRight");
                break;
            case "ArrowUp":
                dotnetRef.invokeMethodAsync("OnPanUp");
                break;
            case "ArrowDown":
                dotnetRef.invokeMethodAsync("OnPanDown");
                break;
        }
        event.preventDefault();
    };

    // Add event listener
    window.addEventListener('keydown', handleKeyDown);

    // Return cleanup function
    return () => {
        window.removeEventListener('keydown', handleKeyDown);
    };
}
