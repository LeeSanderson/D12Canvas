function readAsDataUrl(blob) {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = () => resolve(reader.result);
        reader.onerror = () => reject(reader.error);
        reader.readAsDataURL(blob);
    });
}

export function attach(dialog, dotnetRef) {
    const handlePaste = (event) => {
        const image = Array.from(event.clipboardData?.files ?? []).find((file) =>
            file.type.startsWith("image/")
        );
        if (image === undefined) {
            return;
        }

        event.preventDefault();
        readAsDataUrl(image).then((dataUrl) =>
            dotnetRef.invokeMethodAsync("OnScreenshotPasted", dataUrl)
        );
    };

    const handleKeyDown = (event) => {
        if (event.key === "Enter" && (event.ctrlKey || event.metaKey)) {
            event.preventDefault();
            dotnetRef.invokeMethodAsync("OnSaveShortcut");
        }
    };

    const handleClose = () => dotnetRef.invokeMethodAsync("OnDialogClosed");

    dialog.addEventListener("paste", handlePaste);
    dialog.addEventListener("keydown", handleKeyDown);
    dialog.addEventListener("close", handleClose);

    return {
        open: () => {
            if (!dialog.open) {
                dialog.showModal();
            }
            dialog.querySelector("input")?.focus();
        },
        close: () => dialog.close(),
        dispose: () => {
            dialog.removeEventListener("paste", handlePaste);
            dialog.removeEventListener("keydown", handleKeyDown);
            dialog.removeEventListener("close", handleClose);
        }
    };
}

export async function readClipboardImage() {
    try {
        for (const item of await navigator.clipboard.read()) {
            const type = item.types.find((candidate) => candidate.startsWith("image/"));
            if (type !== undefined) {
                return await readAsDataUrl(await item.getType(type));
            }
        }
    } catch {
        // Refused or unavailable: the caller says there was no image to paste.
    }

    return null;
}
