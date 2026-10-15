using System.Security.Cryptography;

namespace D12Canvas.Model;

// Binary content held once on a Board. Not an entity: it has no bounds, cannot be selected or
// grouped, and is referenced by entities rather than referencing them. Its id is derived from its
// bytes, which is what makes the table add-only, dedupe for free and merge idempotently.
public sealed class Asset
{
    private string? _dataUri;

    internal Asset(string id, string mimeType, byte[] data)
    {
        Id = id;
        MimeType = mimeType;
        Data = data;
    }

    public string Id { get; }
    public string MimeType { get; }
    public byte[] Data { get; }

    // Encoded once per asset rather than once per instance that shows it.
    public string DataUri => _dataUri ??= $"data:{MimeType};base64,{Convert.ToBase64String(Data)}";

    // The algorithm prefix lets a future digest coexist with existing ids instead of invalidating
    // them.
    public static string IdFor(byte[] data) =>
        "sha256-" + Convert.ToHexStringLower(SHA256.HashData(data));
}
