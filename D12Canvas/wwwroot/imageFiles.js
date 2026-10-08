// Files picked, dropped or pasted wait here, by token, until .NET takes their bytes as a stream:
// a stream is the one way bytes cross into a Blazor Server host without hitting its message size
// limit. A file .NET never takes, such as one it refused by type, is let go once it has been held
// a minute, which is far longer than any take waits.
const heldImages = new Map();
const holdLimitMs = 60000;
let nextToken = 1;

function releaseStaleImages() {
    const now = Date.now();
    for (const [token, held] of heldImages) {
        if (now - held.heldAt > holdLimitMs) {
            heldImages.delete(token);
        }
    }
}

async function decodedSize(blob) {
    const url = URL.createObjectURL(blob);
    try {
        const image = new Image();
        image.src = url;
        await image.decode();
        return { width: image.naturalWidth, height: image.naturalHeight };
    } catch {
        return null;
    } finally {
        URL.revokeObjectURL(url);
    }
}

// Only image types, and only ones the browser can decode, are held. The file's name is never read.
export async function holdImages(blobs) {
    releaseStaleImages();
    const held = [];
    for (const blob of blobs) {
        if (typeof blob.type !== "string" || !blob.type.startsWith("image/")) {
            continue;
        }

        const size = await decodedSize(blob);
        if (size === null) {
            continue;
        }

        const token = nextToken++;
        heldImages.set(token, { blob, heldAt: Date.now() });
        held.push({ token, mimeType: blob.type, width: size.width, height: size.height });
    }

    return held;
}

export async function takeImageBytes(token) {
    const held = heldImages.get(token);
    heldImages.delete(token);
    return held === undefined ? new Uint8Array(0) : new Uint8Array(await held.blob.arrayBuffer());
}

// The input is clicked straight away, while the click that asked for it still counts as user
// activation. Cancelling the picker resolves with nothing. A browser without the input's cancel
// event only gives focus back to the page, so a picker that has handed back focus and no file
// within a second is taken as cancelled too.
export function chooseImageFile() {
    return new Promise((resolve) => {
        const input = document.createElement("input");
        input.type = "file";
        input.accept = "image/*";
        input.style.display = "none";
        let finished = false;

        const finish = async (files) => {
            if (finished) {
                return;
            }

            finished = true;
            input.remove();
            const held = files.length === 0 ? [] : await holdImages([files[0]]);
            resolve(held.length === 0 ? null : held[0]);
        };

        const finishOnceFocusReturns = () =>
            setTimeout(() => {
                if ((input.files?.length ?? 0) === 0) {
                    finish([]);
                }
            }, 1000);

        input.addEventListener("change", () => finish(Array.from(input.files ?? [])), { once: true });
        input.addEventListener("cancel", () => finish([]), { once: true });
        window.addEventListener("focus", finishOnceFocusReturns, { once: true });
        document.body.appendChild(input);
        input.click();
    });
}
