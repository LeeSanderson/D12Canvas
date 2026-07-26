namespace D12Canvas.Persistence;

public sealed class UnsupportedSchemaVersionException : Exception
{
    public int ExpectedSchemaVersion { get; }
    public int ActualSchemaVersion { get; }

    public UnsupportedSchemaVersionException(int expectedSchemaVersion, int actualSchemaVersion)
        : base(
            $"Cannot deserialize a board with schema version {actualSchemaVersion}; only schema version {expectedSchemaVersion} is supported."
        )
    {
        ExpectedSchemaVersion = expectedSchemaVersion;
        ActualSchemaVersion = actualSchemaVersion;
    }
}
