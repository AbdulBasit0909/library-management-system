// By using "export", we make this function available for Blazor to import directly.
export function saveAsFile(fileName, byteBase64) {
    // Create a link element
    var link = document.createElement('a');
    link.download = fileName;
    link.href = "data:application/octet-stream;base64," + byteBase64;

    // Append the link to the body, click it, and then remove it
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}