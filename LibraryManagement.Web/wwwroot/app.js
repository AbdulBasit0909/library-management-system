// This function will be called automatically by the Blazor framework after it starts.
export function afterStarted(blazor) {
    console.log("Blazor has started. Removing loading styles.");
    // Find the #app element and remove the 'app-loading' class.
    // This instantly removes the centering and dark background.
    document.getElementById('app').classList.remove('app-loading');
}
// This function takes a filename and the file content as a Base64-encoded string.
// By attaching the function directly to the 'window' object,
// we make it globally available for Blazor's JSRuntime to find.
window.saveAsFile = (fileName, byteBase64) => {
    // Create a link element
    var link = document.createElement('a');
    link.download = fileName;
    link.href = "data:application/octet-stream;base64," + byteBase64;

    // Append the link to the body, click it, and then remove it
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};
