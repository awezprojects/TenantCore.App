// Small DOM helpers Blazor can't express through attribute binding alone.
window.setIndeterminate = (el, value) => {
    if (el) el.indeterminate = value;
};
