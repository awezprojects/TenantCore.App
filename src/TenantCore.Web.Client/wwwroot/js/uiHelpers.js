// Small DOM helpers Blazor can't express through attribute binding alone.
window.setIndeterminate = (el, value) => {
    if (el) el.indeterminate = value;
};

// Preserves scroll position of the main scrollable page area across a
// re-render (e.g. after Save), instead of the page jumping to the top.
window.getScrollTop = (elementId) => {
    const el = document.getElementById(elementId);
    return el ? el.scrollTop : 0;
};

window.setScrollTop = (elementId, top) => {
    const el = document.getElementById(elementId);
    if (el) el.scrollTop = top;
};

// Saves a Uint8Array (binary) as a downloaded file (used by the usage Excel export).
window.ccDownloadBinaryFile = (fileName, base64, mimeType) => {
    const bytes = Uint8Array.from(atob(base64), c => c.charCodeAt(0));
    const blob = new Blob([bytes], { type: mimeType });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
};

// Saves a string the page already has in memory as a downloaded file (used by the usage CSV export,
// which must never re-query the API just to produce a file).
window.ccDownloadTextFile = (fileName, text) => {
    const blob = new Blob([text], { type: "text/csv;charset=utf-8" });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
};
