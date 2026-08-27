using System.Text;

namespace Infrastructure.Persistence;

internal interface IFileOperations
{
    bool Exists(string path);

    string ReadAllText(string path);

    IFileWriteSession CreateNewWriteSession(string path);

    void Replace(string sourcePath, string destinationPath);

    void Move(string sourcePath, string destinationPath);

    void Delete(string path);
}

internal interface IFileWriteSession : IDisposable
{
    void Write(string content);

    void FlushToDisk();
}

internal sealed class PhysicalFileOperations : IFileOperations
{
    public bool Exists(string path) => File.Exists(path);

    public string ReadAllText(string path) => File.ReadAllText(path);

    public IFileWriteSession CreateNewWriteSession(string path) => new PhysicalFileWriteSession(path);

    public void Replace(string sourcePath, string destinationPath) =>
        File.Replace(sourcePath, destinationPath, destinationBackupFileName: null);

    public void Move(string sourcePath, string destinationPath) => File.Move(sourcePath, destinationPath);

    public void Delete(string path) => File.Delete(path);
}

internal sealed class PhysicalFileWriteSession : IFileWriteSession
{
    private readonly FileStream _fileStream;
    private readonly StreamWriter _writer;

    internal PhysicalFileWriteSession(string path)
    {
        _fileStream = new FileStream(
            path,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 4096,
            FileOptions.SequentialScan);
        _writer = new StreamWriter(_fileStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), leaveOpen: true);
    }

    public void Write(string content) => _writer.Write(content);

    public void FlushToDisk()
    {
        _writer.Flush();
        _fileStream.Flush(flushToDisk: true);
    }

    public void Dispose()
    {
        try
        {
            _writer.Dispose();
        }
        finally
        {
            _fileStream.Dispose();
        }
    }
}
