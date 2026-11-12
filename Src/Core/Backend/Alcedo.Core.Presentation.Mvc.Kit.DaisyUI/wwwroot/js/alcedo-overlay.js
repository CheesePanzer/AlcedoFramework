window.AlcedoOverlay = {
    create: function (target = null) {
        const targetEl = typeof target === 'string' ? document.querySelector(target) : target;
        const isGlobal = !targetEl || targetEl === document.body;

        const overlay = document.createElement('div');
        overlay.className = 'fade flex items-center justify-center';
        const config = {
            position: isGlobal ? 'fixed' : 'absolute',
            zIndex: isGlobal ? '1050' : '50',
            bg: 'rgba(0,0,0,0.3)'
        };

        overlay.style.cssText = `
            position: ${config.position}; top: 0; left: 0; width: 100%; height: 100%;
            background: ${config.bg}; z-index: ${config.zIndex}; transition: opacity 0.15s linear;
            pointer-events: all;
        `;
        overlay.innerHTML = `
            <div class="flex flex-col items-center gap-2 text-white">
                <span class="loading loading-spinner loading-md"></span>
                <span class="text-sm font-medium select-none">Processing...</span>
            </div>
        `;
        if (!isGlobal) {
            if (window.getComputedStyle(targetEl).position === 'static') {
                targetEl.style.position = 'relative';
            }
            targetEl.appendChild(overlay);
        } else {
            document.body.appendChild(overlay);
        }
        overlay.offsetWidth;
        overlay.classList.add('show');

        return overlay;
    },
    remove: function (overlayEl) {
        if (!overlayEl) return;

        overlayEl.classList.remove('show');

        const cleanup = () => {
            if (overlayEl.parentNode) {
                overlayEl.remove();
            }
        };
        overlayEl.addEventListener('transitionend', cleanup, {once: true});
        setTimeout(cleanup, 500);
    }
};