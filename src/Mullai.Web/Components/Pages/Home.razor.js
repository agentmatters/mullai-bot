export function scrollToBottom(element) {
    if (!element) return;
    element.scrollTop = element.scrollHeight;
}

export function smartScrollToBottom(element) {
    if (!element) return;

    // threshold in pixels to consider "at the bottom"
    const threshold = 100;
    const isAtBottom = element.scrollHeight - element.scrollTop - element.clientHeight < threshold;

    if (isAtBottom) {
        element.scrollTop = element.scrollHeight;
    }
}
