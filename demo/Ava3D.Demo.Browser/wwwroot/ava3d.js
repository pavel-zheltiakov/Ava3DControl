// The one thing the demo needs from the page that .NET cannot reach on its own.
//
// A browser tab has no command line, so the demo's scene and tour switches — which exist so a published
// frame rate can be reproduced by whoever reads it — arrive as a query string instead. JSImport binds
// functions rather than properties, which is why location.search is wrapped rather than imported directly.
export function locationSearch() {
    return globalThis.location.search ?? "";
}
