using System.Security.Cryptography;

namespace D12Canvas.Model;

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

    public string DataUri => _dataUri ??= $"data:{MimeType};base64,{Convert.ToBase64String(Data)}";

    public static string IdFor(byte[] data) =>
        "sha256-" + Convert.ToHexStringLower(SHA256.HashData(data));
}
