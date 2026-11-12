(function () {
    htmx.defineExtension('alcedo-core', {
        onEvent: function (name, evt) {
            const elt = evt.detail.elt;
            const form = elt.closest('form') || (elt.tagName === 'FORM' ? elt : null);

            switch (name) {
                case "htmx:confirm":
                    this._handleConfirm(elt, form, evt);
                    break;
                case "htmx:configRequest":
                    this._handleConfigRequest(elt, form, evt);
                    break;
                case "htmx:afterRequest":
                    this._handleAfterRequest(elt, form, evt);
                    break;
            }
        },

        _getUIInstance: function (elt) {
            const customDriverName = elt.getAttribute('hx-custom-ui')
            if (customDriverName && typeof window[customDriverName] === 'object') {
                return window[customDriverName];
            }
            
            if (typeof AlcedoUI !== 'undefined') {
                return AlcedoUI;
            }

            return null;
        },

        _handleConfirm: function (elt, form, evt) {
            if (form && typeof $ !== 'undefined' && $.validator?.unobtrusive) {
                $.validator.unobtrusive.parse(form);
                if (!$(form).valid()) {
                    evt.detail.valid = false;
                    return;
                }
            }

            const confirmMsg = elt.getAttribute('hx-custom-confirm');
            if (confirmMsg) {
                evt.preventDefault();
                const ui = this._getUIInstance(elt);
                ui.showModal({
                    title: "Confirm",
                    message: confirmMsg,
                    confirm: true
                }).then(confirmed => {
                    if (confirmed) {
                        evt.detail.issueRequest();
                    }
                });
            }
        },

        _handleConfigRequest: function (elt, form, evt) {
            const hxAccept = elt.getAttribute('hx-accept');
            if (hxAccept) {
                evt.detail.headers['Accept'] = hxAccept;
            } else {
                const hxTarget = elt.getAttribute('hx-target');
                const hxSwap = elt.getAttribute('hx-swap');
                const hasHxTarget = hxTarget || (hxSwap && hxSwap !== 'none');
                evt.detail.headers['Accept'] = hasHxTarget ? 'text/html' : 'application/json';
            }

            const csrfToken = form?.querySelector(
                'input[name="__RequestVerificationToken"]'
            )?.value;

            if (csrfToken) {
                evt.detail.headers['RequestVerificationToken'] = csrfToken;
            }

            const hex = (s) => [...Array(s)].map(() => Math.floor(Math.random() * 16).toString(16)).join('');
            evt.detail.headers['traceparent'] = `00-${hex(32)}-${hex(16)}-01`;
            evt.detail.headers['X-MvcJs-Submit'] = 'true';

            const tokenInput = (form ? form.querySelector('input[name="__FormUniqueRequestToken"]') : null)
                || (elt.name === "__FormUniqueRequestToken" ? elt : elt.querySelector('input[name="__FormUniqueRequestToken"]'));

            if (tokenInput?.value) {
                evt.detail.headers['X-Idempotency-Key'] = tokenInput.value;
            }

            const btn = elt.querySelector('[type="submit"]') || document.getElementById(elt.id + '-submit-btn');
            if (btn) {
                const loadingText = elt.getAttribute('hx-loading-text') || 'Submitting...';
                const ui = this._getUIInstance(elt);
                ui.setButtonLoading(btn, loadingText);
            }

            if (elt.getAttribute('hx-show-overlay') === 'true') {
                const targetSelector = elt.getAttribute('hx-overlay-target');
                const ui = this._getUIInstance(elt);
                elt._alcedoOverlay = ui.createOverlay(targetSelector);
            }
        },

        _handleAfterRequest: function (elt, form, evt) {
            this._resetUniqueToken(form || elt);

            const btn = elt.querySelector('[type="submit"]') || document.getElementById(elt.id + '-submit-btn');
            if (btn) {
                const ui = this._getUIInstance(elt);
                ui.resetButton(btn);
            }

            if (elt._alcedoOverlay) {
                const ui = this._getUIInstance(elt);
                ui.removeOverlay(elt._alcedoOverlay);
                elt._alcedoOverlay = null;
            }

            if (evt.detail.successful) {
                this._processSuccessAction(elt, form, evt.detail.xhr);
            } else {
                this._handleError(elt, form, evt.detail.xhr);
            }
        },

        _handleError: function (elt, form, xhr) {
            let errorMsg = "";
            let isNetworkError = false;

            if (!xhr || xhr.status === 0) {
                isNetworkError = true;
                errorMsg = "Failed to connect to server, please check your network";
            } else {
                try {
                    const json = JSON.parse(xhr.responseText);
                    errorMsg = json.message || json.error || json.detail || xhr.statusText;
                } catch (e) {
                    errorMsg = xhr.responseText && xhr.responseText.length > 200
                        ? `Server Error (HTTP ${xhr.status})`
                        : (xhr.responseText || "Error");
                }
            }
            const ui = this._getUIInstance(elt);
            ui.showModal({
                title: isNetworkError ? "Network Error" : "Failed to process",
                message: errorMsg,
                isError: true
            }).then(() => {
                if (!isNetworkError) {
                    this._processErrorAction(elt, form);
                }
            });
        },

        _processSuccessAction: function (elt, form, xhr) {
            const action = elt.getAttribute('hx-on-success') || 'refresh';
            const target = elt.getAttribute('hx-success-target');
            const showSuccessModal = elt.getAttribute('hx-show-success-modal') === 'true';
            const notJammingWindow = elt.getAttribute('hx-success-modal-not-jamming') === 'true';

            const execute = () => {
                switch (action) {
                    case 'refresh':
                        location.reload();
                        break;
                    case 'redirect':
                        if (target) {
                            window.location.href = this._interpolateUrl(target, elt, xhr);
                        }
                        break;
                    case 'clear':
                        if (form) form.reset();
                        break;
                    case 'updateToken':
                        this._resetUniqueToken(form || elt);
                        break;
                    case 'stay':
                        break;
                    default:
                        location.reload();
                        break;
                }
            };

            if (showSuccessModal) {
                const successModalText = elt.getAttribute('hx-success-modal-text');
                const ui = this._getUIInstance(elt);
                if (notJammingWindow)
                {
                    ui.showModal({
                        title: "Info",
                        message: successModalText || xhr.responseText || "Success",
                        isError: false
                    });
                    execute();
                }
                else{
                    ui.showModal({
                        title: "Info",
                        message: successModalText || xhr.responseText || "Success",
                        isError: false
                    }).then(() => execute());
                }
            } else {
                execute();
            }
        },

        _interpolateUrl: function (targetUrl, elt, xhr) {
            if (!targetUrl.includes('{')) return targetUrl;

            let dataSource = {};
            const rawResponse = xhr ? xhr.responseText?.trim() : '';

            if (rawResponse) {
                try {
                    const json = JSON.parse(rawResponse);
                    if (typeof json === 'object' && json !== null) {
                        dataSource = { ...json };
                    } else {
                        dataSource['value'] = json;
                        dataSource['id'] = json;
                    }
                } catch (e) {
                    dataSource['value'] = rawResponse;
                    dataSource['id'] = rawResponse;
                }
            }

            if (elt && elt.attributes) {
                for (let i = 0; i < elt.attributes.length; i++) {
                    const attr = elt.attributes[i];
                    if (attr.name.startsWith('data-')) {
                        const key = attr.name.slice(5);
                        if (dataSource[key] === undefined) {
                            dataSource[key] = attr.value;
                        }
                    }
                }
            }

            return targetUrl.replace(/\{([^}]+)\}/g, function (match, key) {
                const val = dataSource[key];
                if (val !== undefined) {
                    const valStr = String(val);
                    if (/^https?:\/\//i.test(valStr) && targetUrl.trim() === match) {
                        return valStr;
                    }
                    return encodeURIComponent(valStr);
                }
                return match;
            });
        },

        _processErrorAction: function (elt, form) {
            const action = elt.getAttribute('hx-on-error') || 'stay';
            const target = elt.getAttribute('hx-error-target');

            switch (action) {
                case 'refresh':
                    location.reload();
                    break;
                case 'redirect':
                    if (target) window.location.href = target;
                    break;
                case 'clear':
                    if (form) form.reset();
                    break;
                case 'updateToken':
                    this._resetUniqueToken(form || elt);
                    break;
                case 'stay':
                    break;
                default:
                    location.reload();
                    break;
            }
        },

        _resetUniqueToken: function (container) {
            if (!container) return;
            const input = container.querySelector('input[name="__FormUniqueRequestToken"]');
            if (input) {
                const token = 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
                    var r = Math.random() * 16 | 0, v = c == 'x' ? r : (r & 0x3 | 0x8);
                    return v.toString(16);
                });
                input.value = token;
            }
        }
    });
})();
