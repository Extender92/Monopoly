using Monopoly.Core;

namespace Infrastructure.Profiles;

/// <summary>Loads one explicitly configured profile without exposing its transport to Core.</summary>
public interface IGameProfileSource
{
    ValidatedGameProfile Load();
}
