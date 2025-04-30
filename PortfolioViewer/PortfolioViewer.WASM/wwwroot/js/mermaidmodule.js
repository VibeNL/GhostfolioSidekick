import mermaid from 'https://cdn.jsdelivr.net/npm/mermaid@9/dist/mermaid.esm.min.mjs';

export function Initialize() {
    mermaid.initialize({ startOnLoad: true, flowchart: { useMaxWidth: true } });
}

export function Render(componentId, definition) {
    const cb = function (svgGraph) {
        //console.logg(svgGraph);
    };
    var elements = document.getElementsByClassName(componentId);
    for (const element of elements) {
        const diagramdefinition = htmlDecode(element.innerHTML);
        const id = "mmd" + Math.round(Math.random() * 10000);
        mermaid.render(`${id}-mermaid-svg`, diagramdefinition, (svg, bind) => {
            const host = element;
            host.innerHTML = svg
            //bind(host);

        });
    }
}

function htmlDecode(input) {
    var doc = new DOMParser().parseFromString(input, "text/html");
    return doc.documentElement.textContent;
}