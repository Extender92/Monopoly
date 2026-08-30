using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Monopoly.Core.Models.Board;
using Monopoly.Core.Presentation;

namespace Monopoly.Core;

internal static class ProfileFingerprintCalculator
{
    internal static ProfileFingerprint Calculate(GameProfileDefinition definition, ProfileRuleGraph graph)
    {
        using MemoryStream stream = new();
        CanonicalWriter writer = new(stream);
        writer.String("property-trading-profile-canonical-v1");
        writer.Int32(definition.SchemaVersion);
        writer.String(definition.Id.Value);
        writer.Int32(definition.Revision.Value);
        writer.String(definition.PresentationToken.Value);

        writer.List(definition.Presentation.Entries, metadata =>
        {
            writer.String(metadata.Token.Value);
            writer.NullableString(metadata.DisplayText);
            writer.NullableString(metadata.ShortText);
            writer.NullableString(metadata.Description);
            writer.NullableString(metadata.Symbol);
            writer.NullableString(metadata.ColorToken?.Value);
            writer.NullableString(metadata.LayoutToken?.Value);
        });

        writer.List(definition.Resources.OrderBy(resource => resource.Id), resource =>
        {
            writer.String(resource.Id.Value);
            writer.String(resource.PresentationToken.Value);
        });

        ProfileSetupDefinition setup = definition.Setup;
        writer.Int32(setup.MinimumPlayers);
        writer.Int32(setup.MaximumPlayers);
        writer.Int32(setup.DiceCount);
        writer.Int32(setup.DieSides);
        writer.String(setup.StartSpaceId.Value);
        writer.String(StartingPlayerPolicy(setup.StartingPlayerPolicy));
        writer.List(setup.StartingResources, amount => WriteAmount(writer, amount));

        writer.List(graph.Track.SpaceIds, id => writer.String(id.Value));
        writer.List(graph.ProfileCapabilities.Entries, capability => WriteCapability(writer, capability));
        writer.List(graph.Spaces, space =>
        {
            writer.String(space.Id.Value);
            writer.String(space.PresentationToken.Value);
            writer.List(space.Capabilities.Entries, capability => WriteCapability(writer, capability));
        });
        writer.List(graph.Decks, deck =>
        {
            writer.String(deck.Id.Value);
            writer.String(deck.PresentationToken.Value);
            writer.List(deck.Cards, card =>
            {
                writer.String(card.Id.Value);
                writer.String(card.PresentationToken.Value);
                writer.List(card.Effects.Entries, effect => WriteEffect(writer, effect));
            });
        });
        writer.List(graph.Statuses, status =>
        {
            writer.String(status.Id.Value);
            writer.String(status.PresentationToken.Value);
            writer.Int32(status.MaximumValue);
        });

        writer.Bool(definition.Policies.PassOriginReward.HasValue);
        if (definition.Policies.PassOriginReward is { } reward) WriteAmount(writer, reward);
        writer.String(PurchaseDeclinePolicy(definition.Policies.PurchaseDecline));
        writer.Int32(definition.Policies.MatchEnd.RoundLimit);
        writer.String(definition.Policies.MatchEnd.ScoreResourceId.Value);
        writer.String(TieBreakPolicy(definition.Policies.MatchEnd.TieBreak));

        string hash = Convert.ToHexString(SHA256.HashData(stream.ToArray())).ToLowerInvariant();
        return new ProfileFingerprint(hash);
    }

    private static void WriteCapability(CanonicalWriter writer, CapabilityDefinition capability)
    {
        writer.String(capability.Id.Value);
        switch (capability)
        {
            case MoveCapabilityDefinition:
                break;
            case OwnableCapabilityDefinition ownable:
                writer.NullableString(ownable.GroupId?.Value);
                break;
            case PurchasableCapabilityDefinition purchasable:
                WriteAmount(writer, purchasable.Price);
                break;
            case UsageFeeCapabilityDefinition usageFee:
                WriteAmount(writer, usageFee.Amount);
                break;
            case DrawCapabilityDefinition draw:
                writer.String(draw.DeckId.Value);
                break;
            default:
                throw new ProfileValidationException(ProfileValidationErrorKind.UnknownComponent, "capabilities", $"Capability '{capability.Id}' has no canonical representation.");
        }
    }

    private static void WriteEffect(CanonicalWriter writer, EffectDefinition effect)
    {
        writer.String(effect.Kind.Value);
        switch (effect)
        {
            case MoveEffectDefinition move:
                switch (move.Target)
                {
                    case RelativeMoveTarget relative:
                        writer.String("relative");
                        writer.Int32(relative.Offset);
                        break;
                    case AbsoluteMoveTarget absolute:
                        writer.String("absolute");
                        writer.String(absolute.SpaceId.Value);
                        break;
                    default:
                        throw new ProfileValidationException(ProfileValidationErrorKind.UnknownComponent, "effects.move.target", "The move target has no canonical representation.");
                }
                writer.String(PassOrigin(move.PassOriginPolicy));
                writer.Bool(move.ResolveDestination);
                break;
            case ResourceChangeEffectDefinition resource:
                writer.String(resource.ResourceId.Value);
                writer.Int32(resource.Delta);
                break;
            case StatusEffectDefinition status:
                writer.String(status.StatusId.Value);
                writer.String(StatusOperation(status.Operation));
                writer.Int32(status.Value);
                break;
            default:
                throw new ProfileValidationException(ProfileValidationErrorKind.UnknownComponent, "effects", $"Effect '{effect.Kind}' has no canonical representation.");
        }
    }

    private static void WriteAmount(CanonicalWriter writer, ResourceAmount amount)
    {
        writer.String(amount.ResourceId.Value);
        writer.Int32(amount.Value);
    }

    private static string StartingPlayerPolicy(StartingPlayerPolicyKind kind) => kind switch
    {
        StartingPlayerPolicyKind.FixedOrder => "fixed-order",
        StartingPlayerPolicyKind.Random => "random",
        StartingPlayerPolicyKind.HighestRoll => "highest-roll",
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    private static string PurchaseDeclinePolicy(PurchaseDeclinePolicyKind kind) => kind switch
    {
        PurchaseDeclinePolicyKind.LeaveUnowned => "leave-unowned",
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    private static string TieBreakPolicy(MatchTieBreakPolicy kind) => kind switch
    {
        MatchTieBreakPolicy.LowestPlayerId => "lowest-player-id",
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    private static string PassOrigin(PassOriginPolicy kind) => kind switch
    {
        PassOriginPolicy.Ignore => "ignore",
        PassOriginPolicy.ApplyProfileReward => "apply-profile-reward",
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    private static string StatusOperation(StatusEffectOperation kind) => kind switch
    {
        StatusEffectOperation.Apply => "apply",
        StatusEffectOperation.Remove => "remove",
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    private sealed class CanonicalWriter(Stream stream)
    {
        private static readonly Encoding Utf8 = new UTF8Encoding(false, true);

        internal void Bool(bool value) => stream.WriteByte(value ? (byte)1 : (byte)0);

        internal void Int32(int value)
        {
            Span<byte> bytes = stackalloc byte[sizeof(int)];
            BinaryPrimitives.WriteInt32BigEndian(bytes, value);
            stream.Write(bytes);
        }

        internal void String(string value)
        {
            byte[] bytes = Utf8.GetBytes(value);
            Int32(bytes.Length);
            stream.Write(bytes);
        }

        internal void NullableString(string? value)
        {
            Bool(value is not null);
            if (value is not null) String(value);
        }

        internal void List<T>(IEnumerable<T> values, Action<T> write)
        {
            T[] entries = values.ToArray();
            Int32(entries.Length);
            foreach (T entry in entries) write(entry);
        }
    }
}
