// D12Canvas JavaScript Interop functions

window.DiagramCanvas = {
    // Get container dimensions
    getContainerDimensions: async (element) => {
        const rect = element.getBoundingClientRect();
        return {
            width: rect.width,
            height: rect.height,
            left: rect.left,
            top: rect.top
        };
    },

    // Add resize event listener
    addResizeListener: async (element, dotnetRef) => {
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
    },

    // Get element position relative to container
    getElementPosition: async (element, container) => {
        const containerRect = container.getBoundingClientRect();
        const elementRect = element.getBoundingClientRect();
        return {
            x: elementRect.left - containerRect.left,
            y: elementRect.top - containerRect.top
        };
    },

    // Get element dimensions
    getElementDimensions: async (element) => {
        const rect = element.getBoundingClientRect();
        return {
            width: rect.width,
            height: rect.height
        };
    },

    // Add keyboard event listener
    addKeyboardListener: async (element, dotnetRef) => {
        const handleKeyDown = (event) => {
            if (event.code === "PageUp") {
                dotnetRef.invokeMethodAsync("OnZoomIn");
                event.preventDefault();
            } else if (event.code === "PageDown") {
                dotnetRef.invokeMethodAsync("OnZoomOut");
                event.preventDefault();
            }
        };

        // Add event listener
        window.addEventListener('keydown', handleKeyDown);

        // Return cleanup function
        return () => {
            window.removeEventListener('keydown', handleKeyDown);
        };
    },
};
