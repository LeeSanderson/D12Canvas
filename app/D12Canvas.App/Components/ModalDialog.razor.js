export function showModal(dialog) {
    if (!dialog.open) {
        dialog.showModal();
    }
}

export function close(dialog) {
    dialog.close();
}
