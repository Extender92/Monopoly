using System.Reflection;

namespace Monopoly.Tests.ConsoleTests;

public sealed class ConsoleSourceContractTests
{
    [Fact]
    public void OnlyConsoleWrapperAccessesSystemConsole()
    {
        string consoleRoot = Path.Combine(RepositoryRoot(), "Monopoly.Console");
        string[] matches = SourceFiles(consoleRoot)
            .Where(path => File.ReadAllText(path).Contains("System.Console", StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(consoleRoot, path).Replace('\\', '/'))
            .ToArray();

        Assert.Equal(["GUI/ConsoleWrapper.cs"], matches);
    }

    [Fact]
    public void ExportableConsoleSourceContainsOnlyTheGenericFrontendSurface()
    {
        string consoleRoot = Path.Combine(RepositoryRoot(), "Monopoly.Console");
        string[] expected =
        [
            "ConsoleApplication.cs",
            "ConsoleCommandLineOptions.cs",
            "ConsoleGameSession.cs",
            "ConsoleInputReader.cs",
            "ConsoleNotificationBuffer.cs",
            "ConsoleNotificationFormatter.cs",
            "ConsoleProjectionBuilder.cs",
            "ConsoleProjectionException.cs",
            "ConsoleProjections.cs",
            "ConsoleRenderer.cs",
            "ConsoleText.cs",
            "GUI/ConsolePresentationResolver.cs",
            "GUI/ConsoleWrapper.cs",
            "GUI/IConsoleWrapper.cs",
            "Program.cs"
        ];
        string[] actual = SourceFiles(consoleRoot)
            .Select(path => Path.GetRelativePath(consoleRoot, path).Replace('\\', '/'))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expected.OrderBy(path => path, StringComparer.Ordinal), actual);
    }

    [Fact]
    public void CorePublicApiContainsNoConsoleTypes()
    {
        Type[] publicTypes = typeof(Monopoly.Core.Game).Assembly.GetExportedTypes();
        IEnumerable<Type> signatureTypes = publicTypes.SelectMany(type =>
            type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .SelectMany(method => method.GetParameters().Select(parameter => parameter.ParameterType)
                    .Append(method.ReturnType))
                .Concat(type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                    .Select(property => property.PropertyType)));

        Assert.DoesNotContain(signatureTypes, type =>
            type == typeof(ConsoleColor) ||
            type.Namespace?.StartsWith("Monopoly.Console", StringComparison.Ordinal) == true);
    }

    private static string RepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "Monopoly.sln")))
            current = current.Parent;
        return current?.FullName ?? throw new DirectoryNotFoundException("Could not locate the repository root.");
    }

    private static IEnumerable<string> SourceFiles(string consoleRoot) =>
        Directory.GetFiles(consoleRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path =>
            {
                string relative = Path.GetRelativePath(consoleRoot, path).Replace('\\', '/');
                return !relative.StartsWith("bin/", StringComparison.OrdinalIgnoreCase) &&
                    !relative.StartsWith("obj/", StringComparison.OrdinalIgnoreCase);
            });
}
