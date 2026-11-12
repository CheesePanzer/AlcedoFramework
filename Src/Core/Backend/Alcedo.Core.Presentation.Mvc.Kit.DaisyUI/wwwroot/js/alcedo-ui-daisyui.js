AlcedoUI.register({
    modal: {
        show: function (options) {
            return showAlcedoModal(options);
        }
    },

    overlay: {
        create: function (target) {
            return AlcedoOverlay.create(target);
        },
        remove: function (overlayEl) {
            AlcedoOverlay.remove(overlayEl);
        }
    },

    loadingBtn: {
        set: function (btn, loadingText) {
            btn.disabled = true;
            btn.dataset.oldHtml = btn.innerHTML;
            btn.innerHTML = `<span class="loading loading-spinner loading-xs mr-2"></span>${loadingText}`;
        },
        reset: function (btn) {
            if (!btn.dataset.oldHtml) return;
            btn.disabled = false;
            btn.innerHTML = btn.dataset.oldHtml;
            delete btn.dataset.oldHtml;
        }
    }
});