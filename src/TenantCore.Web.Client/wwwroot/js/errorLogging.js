// Catches JS-level errors that never reach Blazor's own error handling (uncaught
// exceptions and unhandled promise rejections in plain JS or interop calls) and
// forwards them to the same frontend-error endpoint the C# AppErrorBoundary uses.
// Resolves the API base URL itself from wwwroot/appsettings.json so it works before
// Blazor has finished booting. Never throws — logging must never break the page.
(function () {
    var apiBaseUrl = null;

    function resolveBaseUrl() {
        if (apiBaseUrl !== null) return Promise.resolve(apiBaseUrl);
        return fetch('appsettings.json')
            .then(function (r) { return r.json(); })
            .then(function (cfg) { apiBaseUrl = cfg.TenantApiBaseUrl || ''; return apiBaseUrl; })
            .catch(function () { apiBaseUrl = ''; return apiBaseUrl; });
    }

    function post(message, stackTrace, exceptionType, source) {
        resolveBaseUrl().then(function (baseUrl) {
            try {
                fetch(baseUrl + 'api/logs/frontend-error', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    keepalive: true,
                    body: JSON.stringify({
                        message: message || 'Unknown frontend error',
                        source: source || (window.location && window.location.pathname) || 'Frontend.Unknown',
                        exceptionType: exceptionType || 'JsError',
                        stackTrace: stackTrace || '',
                        additionalContext: navigator.userAgent
                    })
                }).catch(function () { /* swallow — logging must never break the app */ });
            } catch (e) { /* swallow */ }
        });
    }

    window.addEventListener('error', function (event) {
        post(event.message, event.error && event.error.stack, 'JsError', window.location.pathname);
    });

    window.addEventListener('unhandledrejection', function (event) {
        var reason = event.reason;
        var message = reason && reason.message ? reason.message : String(reason);
        var stack = reason && reason.stack ? reason.stack : '';
        post(message, stack, 'UnhandledPromiseRejection', window.location.pathname);
    });
})();
