using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;

namespace Avarice;

internal readonly struct TargetPositionalInfo
{
    internal static readonly TargetPositionalInfo None = new();

    internal readonly bool IsBattleNpc;
    internal readonly bool IsHostile;
    internal readonly bool IsEnemyKind;
    internal readonly bool DataMarksOmnidirectional;
    internal readonly bool HasNonPositionalStatus;
    internal readonly bool NonPositionalStatusIgnored;
    internal readonly bool EffectiveHasPositionals;

    internal bool IsRenderableHostileTarget => IsBattleNpc && IsHostile;

    internal bool AllowsPositionalOverlays(bool onlyPositionalTargets)
    {
        return IsRenderableHostileTarget && (!onlyPositionalTargets || EffectiveHasPositionals);
    }

    internal bool AllowsAnyOverlays(bool onlyPositionalTargets, bool allowRangeFallback)
    {
        return IsRenderableHostileTarget
            && (!onlyPositionalTargets || EffectiveHasPositionals || allowRangeFallback);
    }

    internal bool UsesSimpleRangeCircle(bool allowRangeFallback)
    {
        return IsRenderableHostileTarget && allowRangeFallback && !EffectiveHasPositionals;
    }

    internal string Reason
    {
        get
        {
            if (!IsBattleNpc) return "Not a battle NPC";
            if (!IsHostile) return "Target is not hostile";
            if (!IsEnemyKind) return "Target is not an enemy BNPC";
            if (DataMarksOmnidirectional) return "BNpcBase marks target as omnidirectional";
            if (HasNonPositionalStatus && !NonPositionalStatusIgnored) return "Target has temporary non-positional status 3808";
            if (HasNonPositionalStatus && NonPositionalStatusIgnored) return "Status 3808 ignored by profile override";
            return EffectiveHasPositionals ? "Target supports positional overlays" : "No positional vulnerability detected";
        }
    }

    internal TargetPositionalInfo(
        bool isBattleNpc,
        bool isHostile,
        bool isEnemyKind,
        bool dataMarksOmnidirectional,
        bool hasNonPositionalStatus,
        bool nonPositionalStatusIgnored,
        bool effectiveHasPositionals)
    {
        IsBattleNpc = isBattleNpc;
        IsHostile = isHostile;
        IsEnemyKind = isEnemyKind;
        DataMarksOmnidirectional = dataMarksOmnidirectional;
        HasNonPositionalStatus = hasNonPositionalStatus;
        NonPositionalStatusIgnored = nonPositionalStatusIgnored;
        EffectiveHasPositionals = effectiveHasPositionals;
    }
}
