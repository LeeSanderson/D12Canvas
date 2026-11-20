using Microsoft.JSInterop;

namespace D12Canvas;

public sealed record HeldImage(int Token, string MimeType, double Width, double Height)
{
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
