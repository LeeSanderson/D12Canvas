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
