(() => {
    if (!window.mermaid) return;

    window.mermaid.initialize({startOnLoad: false});

    const runMermaid = () => {
        const nodes = document.querySelectorAll(".mermaid");
        if (nodes.length === 0) return;
        try {
            window.mermaid.run({nodes});
        } catch {
            // ignore rendering errors
        }
    };

    const observer = new MutationObserver(() => {
        runMermaid();
    });

    observer.observe(document.body, {childList: true, subtree: true});

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", runMermaid);
    } else {
        runMermaid();
    }
})();
