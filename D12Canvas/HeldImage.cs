using Microsoft.JSInterop;

namespace D12Canvas;

// An image file the browser has decoded and is holding until its bytes are taken by token. Its
// pixel size is what an image made from it is sized by; the file's name never crosses over.
public sealed record HeldImage(int Token, string MimeType, double Width, double Height)
{
    // Read as a stream, so a large picture crosses into a server-hosted canvas whole rather than
    // hitting the interop message size limit. The library sets no size limit of its own. Empty
    // when the type is not a plain image subtype, since the type ends up inside a data: URI.
    internal async Task<byte[]> TakeBytes(IJSObjectReference module)
    {
        if (!ImagePicture.IsAcceptedMimeType(MimeType))
        {
            return [];
        }

        await using var reference = await module.InvokeAsync<IJSStreamReference>(
            "takeImageBytes",
            Token
        );
        await using var stream = await reference.OpenReadStreamAsync(long.MaxValue);
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer);
        return buffer.ToArray();
    }
}
