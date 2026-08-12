// Hands bytes to the browser as a file. A Blazor byte[] arrives here as a Uint8Array, so nothing
// is base64-encoded on the way through.
export function save(fileName, contentType, bytes) {
    const url = URL.createObjectURL(new Blob([bytes], { type: contentType }));

    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;

    document.body.appendChild(link);
    link.click();
    link.remove();

    URL.revokeObjectURL(url);
}
