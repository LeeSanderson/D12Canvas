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
    }
};
