using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.GameFonts;
using ECommons.GameFunctions;
using static Avarice.Drawing.DrawFunctions;
using static Avarice.Drawing.Functions;
using static Avarice.Util;

namespace Avarice.Drawing;

internal unsafe class Canvas : Window
{
    public Canvas() : base("Avarice overlay",
      ImGuiWindowFlags.NoInputs
      | ImGuiWindowFlags.NoTitleBar
      | ImGuiWindowFlags.NoScrollbar
      | ImGuiWindowFlags.NoBackground
      | ImGuiWindowFlags.AlwaysUseWindowPadding
      | ImGuiWindowFlags.NoSavedSettings
      | ImGuiWindowFlags.NoFocusOnAppearing
      , true)
    {
        IsOpen = true;
        RespectCloseHotkey = false;
    }

    public override void PreDraw()
    {
        base.PreDraw();
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
        ImGuiHelpers.SetNextWindowPosRelativeMainViewport(Vector2.Zero);
        ImGui.SetNextWindowSize(ImGuiHelpers.MainViewport.Size);
    }

    public override bool DrawConditions()
    {
        if (Svc.Objects.LocalPlayer == null)
            return false;

        if (Svc.Condition[ConditionFlag.OccupiedInCutSceneEvent] || Svc.Condition[ConditionFlag.WatchingCutscene78])
            return false;

        if (!P.currentProfile.DrawingEnabled)
            return false;

        if (!P.config.OnlyDrawIfPositional)
            return true;

        return ShouldDrawForTarget(Svc.Targets.Target)
            || ShouldDrawForTarget(Svc.Targets.FocusTarget);
    }

    private static bool ShouldDrawForTarget(IGameObject target)
    {
        var evaluation = LuminaSheets.EvaluateTarget(target);
        var allowRangeFallback = P.currentProfile.EnableMaxMeleeRing && P.currentProfile.MaxMeleeIgnorePositionalCheck;
        return evaluation.AllowsAnyOverlays(P.config.OnlyDrawIfPositional, allowRangeFallback);
    }

    private static bool ShouldShowPositionalFeaturesForTarget(IGameObject target)
    {
        return LuminaSheets.EvaluateTarget(target).AllowsPositionalOverlays(P.config.OnlyDrawIfPositional);
    }

    private static bool ShouldDrawFocusTarget(IGameObject focusTarget, IGameObject target)
    {
        return focusTarget != null && focusTarget.Address != target?.Address;
    }

    private static bool IsInHousingZone()
    {
        var territoryIntendedUse = ECommons.GameHelpers.Player.TerritoryIntendedUseEnum;
        return territoryIntendedUse == ECommons.ExcelServices.TerritoryIntendedUseEnum.Residential_Area
            || territoryIntendedUse == ECommons.ExcelServices.TerritoryIntendedUseEnum.Housing_Instances;
    }

    private void DrawMaxMeleeForTarget(IBattleNpc bnpc, TargetPositionalInfo evaluation)
    {
        var useSimpleCircle = evaluation.UsesSimpleRangeCircle(P.currentProfile.MaxMeleeIgnorePositionalCheck);
        var pos = IsInHousingZone() ? GroundDetection.GetAutoGroundedPosition(bnpc.Position) : bnpc.Position;

        if (useSimpleCircle)
        {
            if (P.currentProfile.Radius3)
            {
                CircleXZ(pos, bnpc.HitboxRadius + GetSkillRadius(), P.currentProfile.MaxMeleeSettingsN);
                if (P.currentProfile.Radius2)
                    CircleXZ(pos, bnpc.HitboxRadius + GetAttackRadius(), P.currentProfile.MaxMeleeSettingsN);
            }
            else if (P.currentProfile.Radius2)
                CircleXZ(pos, bnpc.HitboxRadius + GetAttackRadius(), P.currentProfile.MaxMeleeSettingsN);
        }
        else
        {
            if (P.currentProfile.Radius3)
            {
                DrawSegmentedCircle(bnpc, GetSkillRadius(), P.currentProfile.DrawLines);
                if (P.currentProfile.Radius2)
                    DrawSegmentedCircle(bnpc, GetAttackRadius(), false);
            }
            else if (P.currentProfile.Radius2)
                DrawSegmentedCircle(bnpc, GetAttackRadius(), P.currentProfile.DrawLines);
        }
    }

    public override void Draw()
    {
        PictomancyRenderer.BeginFrame();

        try
        {
            DrawAllOverlays();
        }
        finally
        {
            PictomancyRenderer.EndFrame();
        }
    }

    private void DrawAllOverlays()
    {
        var target = Svc.Targets.Target;
        var focusTarget = Svc.Targets.FocusTarget;

        DrawTankMiddle();
        if (P.currentProfile.CompassEnable && IsConditionMatching(P.currentProfile.CompassCondition))
        {
            static void DrawLetter(string l, Vector2 pos, Vector4? color = null)
            {
                var size = ImGui.CalcTextSize(l);
                ImGui.SetCursorPos(new(pos.X - (size.X / 2), pos.Y - (size.Y / 2)));
                ImGuiEx.Text(color ?? Prof.CompassColor, l);
            }

            if (Prof.CompassFont != GameFontFamilyAndSize.Undefined)
            {
                //ImGui.PushFont(Svc.PluginInterface.UiBuilder.GetGameFontHandle(new(Prof.CompassFont)).ImFont);
            }

            ImGui.SetWindowFontScale(Prof.CompassFontScale);
            {
                if (Svc.GameGui.WorldToScreen(LP.Position with { Z = LP.Position.Z - Prof.CompassDistance }, out var pos))
                {
                    DrawLetter("N", pos, Prof.CompassColorN);
                }
            }
            {
                if (Svc.GameGui.WorldToScreen(LP.Position with { Z = LP.Position.Z + Prof.CompassDistance }, out var pos))
                {
                    DrawLetter("S", pos, Prof.CompassColor);
                }
            }
            {
                if (Svc.GameGui.WorldToScreen(LP.Position with { X = LP.Position.X - Prof.CompassDistance }, out var pos))
                {
                    DrawLetter("W", pos, Prof.CompassColor);
                }
            }
            {
                if (Svc.GameGui.WorldToScreen(LP.Position with { X = LP.Position.X + Prof.CompassDistance }, out var pos))
                {
                    DrawLetter("E", pos, Prof.CompassColor);
                }
            }
            ImGui.SetWindowFontScale(1f);
            if (Prof.CompassFont != GameFontFamilyAndSize.Undefined)
            {
                //ImGui.PopFont();
            }
        }

        if (P.currentProfile.EnableCurrentPie && IsConditionMatching(P.currentProfile.CurrentPieSettings.DisplayCondition))
        {
            if (ShouldShowPositionalFeaturesForTarget(target) && target is IBattleNpc targetBnpc)
                DrawCurrentPos(targetBnpc);

            if (ShouldDrawFocusTarget(focusTarget, target)
              && ShouldShowPositionalFeaturesForTarget(focusTarget)
              && focusTarget is IBattleNpc focusBnpc)
                DrawCurrentPos(focusBnpc);
        }

        if (P.currentProfile.EnableMaxMeleeRing && IsConditionMatching(P.currentProfile.MaxMeleeSettingsN.DisplayCondition))
        {
            var targetEvaluation = LuminaSheets.EvaluateTarget(target);
            if (targetEvaluation.AllowsAnyOverlays(P.config.OnlyDrawIfPositional, P.currentProfile.MaxMeleeIgnorePositionalCheck)
              && target is IBattleNpc targetBnpc)
                DrawMaxMeleeForTarget(targetBnpc, targetEvaluation);

            var focusEvaluation = LuminaSheets.EvaluateTarget(focusTarget);
            if (ShouldDrawFocusTarget(focusTarget, target)
              && focusEvaluation.AllowsAnyOverlays(P.config.OnlyDrawIfPositional, P.currentProfile.MaxMeleeIgnorePositionalCheck)
              && focusTarget is IBattleNpc focusBnpc)
                DrawMaxMeleeForTarget(focusBnpc, focusEvaluation);
        }

        if (P.currentProfile.EnablePlayerRing && IsConditionMatching(P.currentProfile.PlayerRingSettings.DisplayCondition))
        {
            CircleXZ(Svc.Objects.LocalPlayer.Position, Svc.Objects.LocalPlayer.HitboxRadius, P.currentProfile.PlayerRingSettings);
        }

        if (P.currentProfile.EnableFrontSegment && IsConditionMatching(P.currentProfile.FrontSegmentIndicator.DisplayCondition))
        {
            if (ShouldShowPositionalFeaturesForTarget(target))
                DrawFrontalPosition(target);

            if (ShouldDrawFocusTarget(focusTarget, target) && ShouldShowPositionalFeaturesForTarget(focusTarget))
                DrawFrontalPosition(focusTarget);
        }

        if (P.currentProfile.EnableAnticipatedPie && IsConditionMatching(P.currentProfile.AnticipatedPieSettings.DisplayCondition)
           && (!P.currentProfile.AnticipatedDisableTrueNorth || !Svc.Objects.LocalPlayer.StatusList.Any(x => x.StatusId.EqualsAny(1250u))))
        {
            if (ShouldShowPositionalFeaturesForTarget(target) && target is IBattleNpc targetBnpc)
                DrawAnticipatedPos(targetBnpc);

            if (ShouldDrawFocusTarget(focusTarget, target)
              && ShouldShowPositionalFeaturesForTarget(focusTarget)
              && focusTarget is IBattleNpc focusBnpc)
                DrawAnticipatedPos(focusBnpc);
        }

        if (P.currentProfile.EnablePlayerDot && IsConditionMatching(P.currentProfile.PlayerDotSettings.DisplayCondition))
        {
            var dotColor = TabSplatoon.IsUnsafe() ? P.config.SplatoonPixelCol : P.currentProfile.PlayerDotSettings.Color;
            if (PictomancyRenderer.IsDrawing)
            {
                var worldRadius = P.currentProfile.PlayerDotSettings.Thickness * 0.03f;
                PictomancyRenderer.DrawCircleFilled(Svc.Objects.LocalPlayer.Position, worldRadius, ImGui.ColorConvertFloat4ToU32(dotColor));
            }
            else if (Svc.GameGui.WorldToScreen(Svc.Objects.LocalPlayer.Position, out var pos))
            {
                ImGui.GetWindowDrawList().AddCircleFilled(
                new Vector2(pos.X, pos.Y),
                P.currentProfile.PlayerDotSettings.Thickness,
                ImGui.ColorConvertFloat4ToU32(dotColor),
                100);
            }
        }

        if (P.currentProfile.PartyDot && IsConditionMatching(P.currentProfile.PartyDotSettings.DisplayCondition))
        {
            foreach (var x in Svc.Party)
            {
                if (x.GameObject is IPlayerCharacter pc && x.GameObject.Address != Svc.Objects.LocalPlayer.Address)
                {
                    if (PictomancyRenderer.IsDrawing)
                    {
                        var worldRadius = P.currentProfile.PartyDotSettings.Thickness * 0.03f;
                        PictomancyRenderer.DrawCircleFilled(x.GameObject.Position, worldRadius, ImGui.ColorConvertFloat4ToU32(P.currentProfile.PartyDotSettings.Color));
                    }
                    else if (Svc.GameGui.WorldToScreen(x.GameObject.Position, out var pos))
                    {
                        ImGui.GetWindowDrawList().AddCircleFilled(
                        new Vector2(pos.X, pos.Y),
                        P.currentProfile.PartyDotSettings.Thickness,
                        ImGui.ColorConvertFloat4ToU32(P.currentProfile.PartyDotSettings.Color),
                        100);
                    }
                }
            }
        }

        if (P.currentProfile.AllDot && IsConditionMatching(P.currentProfile.AllDotSettings.DisplayCondition))
        {
            foreach (var x in Svc.Objects)
            {
                if (x is IPlayerCharacter pc && x.Address != Svc.Objects.LocalPlayer.Address
                  && (!P.currentProfile.PartyDot || !Svc.Party.Any(x => x.Address == x.GameObject?.Address)))
                {
                    if (PictomancyRenderer.IsDrawing)
                    {
                        var worldRadius = P.currentProfile.AllDotSettings.Thickness * 0.03f;
                        PictomancyRenderer.DrawCircleFilled(x.Position, worldRadius, ImGui.ColorConvertFloat4ToU32(P.currentProfile.AllDotSettings.Color));
                    }
                    else if (Svc.GameGui.WorldToScreen(x.Position, out var pos))
                    {
                        ImGui.GetWindowDrawList().AddCircleFilled(
                        new Vector2(pos.X, pos.Y),
                        P.currentProfile.AllDotSettings.Thickness,
                        ImGui.ColorConvertFloat4ToU32(P.currentProfile.AllDotSettings.Color),
                        100);
                    }
                }
            }
        }
    }

    public override void PostDraw()
    {
        base.PostDraw();
        ImGui.PopStyleVar();
    }
}
