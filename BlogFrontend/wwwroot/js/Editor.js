window.setEditorContent = (elementRef, html) => {
    if (!elementRef) return;
    elementRef.innerHTML = html ?? "";
};