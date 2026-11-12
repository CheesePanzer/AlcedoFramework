const AlcedoUI = {
    _modal: null,
    _overlay: null,
    _loadingBtn: null,

    register({ modal, overlay, loadingBtn }) {
        if (modal) this._modal = modal;
        if (overlay) this._overlay = overlay;
        if (loadingBtn) this._loadingBtn = loadingBtn;
    },

    showModal(options) {
        return this._modal?.show(options) ?? Promise.resolve(false);
    },
    createOverlay(target) {
        return this._overlay?.create(target) ?? null;
    },
    removeOverlay(el) {
        this._overlay?.remove(el);
    },
    setButtonLoading(btn, loadingText) {
        this._loadingBtn?.set(btn, loadingText);
    },
    resetButton(btn) {
        this._loadingBtn?.reset(btn);
    }
};