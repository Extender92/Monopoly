namespace Infrastructure.Persistence;

internal interface IFileOperations
{
    bool Exists(string path);

    byte[] ReadBytes(string path, int maximumBytes);

    IFileWriteSession CreateNewWriteSession(string path);

    void Replace(string sourcePath, string destinationPath);

    void Move(string sourcePath, string destinationPath);

    void Delete(string path);
}

internal interface IFileWriteSession : IDisposable
{
    void Write(ReadOnlyMemory<byte> content);

    void FlushToDisk();
}

internal sealed class PhysicalFileOperations : IFileOperations
{
    public bool Exists(string path) => File.Exists(path);

    public byte[] ReadBytes(string path, int maximumBytes)
    {
        if (maximumBytes < 0) throw new ArgumentOutOfRangeException(nameof(maximumBytes));
        using FileStream stream = new(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            FileOptions.SequentialScan);
        using MemoryStream content = new(capacity: (int)Math.Min(stream.Length, maximumBytes));
        byte[] buffer = new byte[Math.Min(81920, Math.Max(1, maximumBytes))];
        while (content.Length < maximumBytes)
        {
            int remaining = maximumBytes - checked((int)content.Length);
            int read = stream.Read(buffer, 0, Math.Min(buffer.Length, remaining));
            if (read == 0) return content.ToArray();
            content.Write(buffer, 0, read);
        }

        if (stream.ReadByte() != -1)
            throw new FileContentLimitExceededException(maximumBytes);
        return content.ToArray();
    }

    public IFileWriteSession CreateNewWriteSession(string path) => new PhysicalFileWriteSession(path);

    public void Replace(string sourcePath, string destinationPath) =>
        File.Replace(sourcePath, destinationPath, destinationBackupFileName: null);

    public void Move(string sourcePath, string destinationPath) => File.Move(sourcePath, destinationPath);

    public void Delete(string path) => File.Delete(path);
}

internal sealed class PhysicalFileWriteSession : IFileWriteSession
{
    private readonly FileStream _fileStream;

    internal PhysicalFileWriteSession(string path)
    {
        _fileStream = new FileStream(
            path,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 4096,
            FileOptions.SequentialScan);
    }

    public void Write(ReadOnlyMemory<byte> content) => _fileStream.Write(content.Span);

    public void FlushToDisk() => _fileStream.Flush(flushToDisk: true);

    public void Dispose() => _fileStream.Dispose();
}

internal sealed class FileContentLimitExceededException : Exception
{
    internal FileContentLimitExceededException(int maximumBytes)
        : base($"The file exceeds the configured {maximumBytes}-byte input limit.")
    {
    }
}
