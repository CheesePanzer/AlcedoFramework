function showAlcedoModal({title, message, confirm = false, isError = false}) {
    const modalId = 'alcedoModal-' + Math.random().toString(36).substr(2, 9);

    const headerClass = isError ? 'text-error' : 'text-primary';
    const btnClass = isError ? 'btn-error' : 'btn-primary';

    const modalHtml = `
        <dialog id="${modalId}" class="modal modal-bottom sm:modal-middle">
            <div class="modal-box">
                <h3 class="font-bold text-lg ${headerClass}">${title}</h3>
                <p class="py-4">${message}</p>
                <div class="modal-action">
                    <form method="dialog" class="flex gap-2">
                        ${confirm ? `<button class="btn btn-ghost" data-bs-result="false">Cancel</button>` : ''}
                        <button class="btn ${btnClass}" data-bs-result="true">Confirm</button>
                    </form>
                </div>
            </div>
            <form method="dialog" class="modal-backdrop">
                <button>Close</button>
            </form>
        </dialog>`;

    document.body.insertAdjacentHTML('beforeend', modalHtml);
    const modalElement = document.getElementById(modalId);

    modalElement.showModal();

    return new Promise((resolve) => {
        let actionResult = false;

        modalElement.querySelectorAll('button[data-bs-result]').forEach(btn => {
            btn.addEventListener('click', () => {
                actionResult = btn.getAttribute('data-bs-result') === 'true';
            });
        });

        modalElement.addEventListener('close', () => {
            modalElement.remove();
            resolve(actionResult);
        });
    });
}