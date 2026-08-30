using System.Text;
using Infrastructure.Profiles;
using Monopoly.Core;

namespace Monopoly.Tests.InfrastructureTests;

public sealed class JsonFileGameProfileSourceTests
{
    private static readonly string DemoPath = Path.Combine(
        AppContext.BaseDirectory,
        "TestData",
        "Profiles",
        "Demo",
        "lantern-vale-v1.json");

    [Theory]
    [InlineData(typeof(FileNotFoundException), ProfileSourceErrorKind.NotFound)]
    [InlineData(typeof(DirectoryNotFoundException), ProfileSourceErrorKind.NotFound)]
    [InlineData(typeof(UnauthorizedAccessException), ProfileSourceErrorKind.AccessDenied)]
    [InlineData(typeof(ArgumentException), ProfileSourceErrorKind.InvalidPath)]
    [InlineData(typeof(NotSupportedException), ProfileSourceErrorKind.InvalidPath)]
    [InlineData(typeof(PathTooLongException), ProfileSourceErrorKind.InvalidPath)]
    [InlineData(typeof(IOException), ProfileSourceErrorKind.StorageFailure)]
    public void OpeningFailuresAreTypedAndSanitized(Type exceptionType, ProfileSourceErrorKind expectedKind)
    {
        const string secretPath = @"C:\private-owner\profiles\secret-reference.json";
        Exception failure = (Exception)Activator.CreateInstance(exceptionType, secretPath)!;
        JsonFileGameProfileSource source = new(
            secretPath,
            new ThrowingFileAccess(failure),
            new JsonGameProfileParser());

        ProfileSourceException exception = Assert.Throws<ProfileSourceException>(source.Load);

        Assert.Equal(expectedKind, exception.Kind);
        Assert.DoesNotContain(secretPath, exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(secretPath, exception.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void ReadFailureIsStorageFailureAndDoesNotLeakTheSourcePath()
    {
        const string secretPath = @"C:\private-owner\profiles\read-failure.json";
        JsonFileGameProfileSource source = new(
            secretPath,
            new StreamFileAccess(new ThrowingReadStream(new IOException(secretPath))),
            new JsonGameProfileParser());

        ProfileSourceException exception = Assert.Throws<ProfileSourceException>(source.Load);

        Assert.Equal(ProfileSourceErrorKind.StorageFailure, exception.Kind);
        Assert.DoesNotContain(secretPath, exception.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void BlankPathIsASelectedSourceError()
    {
        ProfileSourceException exception = Assert.Throws<ProfileSourceException>(() =>
            new JsonFileGameProfileSource(" "));

        Assert.Equal(ProfileSourceErrorKind.InvalidPath, exception.Kind);
    }

    [Fact]
    public void DirectoryPathIsRejectedAsAnInvalidFilePath()
    {
        DirectoryInfo directory = Directory.CreateTempSubdirectory("monopoly-profile-source-");
        try
        {
            JsonFileGameProfileSource source = new(directory.FullName);

            ProfileSourceException exception = Assert.Throws<ProfileSourceException>(source.Load);

            Assert.Equal(ProfileSourceErrorKind.InvalidPath, exception.Kind);
            Assert.DoesNotContain(directory.FullName, exception.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public void JsonAndSemanticFailuresKeepTheirExistingTypedBoundaries()
    {
        string invalidJson = File.ReadAllText(DemoPath).Replace(
            "\"profileId\": \"profile.demo-001\"",
            "\"profileId\": \"INVALID\"",
            StringComparison.Ordinal);
        JsonFileGameProfileSource malformed = new(
            "unused",
            new StreamFileAccess(new MemoryStream("{"u8.ToArray())),
            new JsonGameProfileParser());
        JsonFileGameProfileSource invalid = new(
            "unused",
            new StreamFileAccess(new MemoryStream(Encoding.UTF8.GetBytes(invalidJson))),
            new JsonGameProfileParser());

        Assert.Throws<ProfileJsonException>(malformed.Load);
        Assert.Throws<ProfileValidationException>(invalid.Load);
    }

    private sealed class ThrowingFileAccess(Exception exception) : IProfileFileAccess
    {
        public Stream OpenRead(string path) => throw exception;
    }

    private sealed class StreamFileAccess(Stream stream) : IProfileFileAccess
    {
        public Stream OpenRead(string path) => stream;
    }

    private sealed class ThrowingReadStream(Exception exception) : MemoryStream
    {
        public override int Read(byte[] buffer, int offset, int count) => throw exception;
    }
}
