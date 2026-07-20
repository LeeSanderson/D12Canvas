// FPS/DOM-count HUD for the virtualization stress test dev tool. Runs its own
// requestAnimationFrame loop so the reading reflects real browser frame pacing,
// independent of Blazor's render cycle.

let rafId = null;

export function startMonitor(hudElement, mountedSelector) {
    stopMonitor();

    let frames = 0;
    let lastReportTime = performance.now();

    function loop(now) {
        frames++;
        const elapsed = now - lastReportTime;

        if (elapsed >= 500) {
            const fps = (frames * 1000) / elapsed;
            const frameMs = elapsed / frames;
            const mounted = document.getElementsByClassName(mountedSelector).length;
            hudElement.textContent =
                `${fps.toFixed(0)} fps | ${frameMs.toFixed(2)} ms/frame | ${mounted} mounted DOM nodes`;
            frames = 0;
            lastReportTime = now;
        }

        rafId = requestAnimationFrame(loop);
    }

    rafId = requestAnimationFrame(loop);
}

export function stopMonitor() {
    if (rafId !== null) {
        cancelAnimationFrame(rafId);
        rafId = null;
    }
}
