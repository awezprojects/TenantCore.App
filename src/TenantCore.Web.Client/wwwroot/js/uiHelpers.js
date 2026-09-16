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
