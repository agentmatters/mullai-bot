export function initialize(element, dotNetHelper) {
    if (!element) return;

    element.addEventListener('keydown', (e) => {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            dotNetHelper.invokeMethodAsync('OnEnterPressed');
        }
    });
}

export function scrollToSelected(containerSelector) {
    const container = document.querySelector(containerSelector);
    if (!container) return;

    const selected = container.querySelector('.option.selected');
    if (selected) {
        selected.scrollIntoView({block: 'nearest', behavior: 'smooth'});
    }
}

export function saveToLocalStorage(key, value) {
    localStorage.setItem(key, value);
}

export function loadFromLocalStorage(key) {
    return localStorage.getItem(key);
}
