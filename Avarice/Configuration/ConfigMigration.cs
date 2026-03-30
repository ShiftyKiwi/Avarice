namespace Avarice.Configuration;

internal static class ConfigMigration
{
    internal const int CurrentVersion = 2;

    internal static Config Migrate(Config loadedConfig, out bool changed)
    {
        changed = false;

        var config = loadedConfig ?? new();
        var defaultConfig = new Config();

        changed |= EnsureReference(ref config.Profiles, new List<Profile>());
        changed |= EnsureReference(ref config.JobProfiles, new Dictionary<uint, string>());
        changed |= EnsureReference(ref config.DutyMiddleOverrides, new Dictionary<uint, Vector3?>());
        changed |= EnsureReference(ref config.DutyMiddleExtras, new List<ExtraPoint>());
        if (config.VisualFeedbackSettings == null)
        {
            config.VisualFeedbackSettings = new VisualFeedbackSettings();
            changed = true;
        }

        if (config.AudioFeedbackSettings == null)
        {
            config.AudioFeedbackSettings = new AudioFeedbackSettings();
            changed = true;
        }

        changed |= NormalizeColor(ref config.DutyMidPixelCol, defaultConfig.DutyMidPixelCol);
        changed |= NormalizeColor(ref config.CenteredPixelColor, defaultConfig.CenteredPixelColor);
        changed |= NormalizeColor(ref config.UncenteredPixelColor, defaultConfig.UncenteredPixelColor);
        changed |= NormalizeColor(ref config.SplatoonPixelCol, defaultConfig.SplatoonPixelCol);

        changed |= ClampFloat(ref config.DutyMidRadius, 0.5f, 5f, defaultConfig.DutyMidRadius);
        changed |= ClampFloat(ref config.CenterPixelThickness, 0.5f, 5f, defaultConfig.CenterPixelThickness);
        changed |= NormalizeVisualFeedback(config.VisualFeedbackSettings);
        changed |= NormalizeAudioFeedback(config.AudioFeedbackSettings);

        config.Profiles.RemoveAll(profile => profile == null);
        if (config.Profiles.Count == 0)
        {
            config.Profiles.Add(new Profile { Name = "Default profile", IsDefault = true });
            changed = true;
        }

        for (var i = 0; i < config.Profiles.Count; i++)
        {
            changed |= NormalizeProfile(config.Profiles[i], i);
        }

        var defaultProfiles = config.Profiles.Where(x => x.IsDefault).ToArray();
        if (defaultProfiles.Length != 1)
        {
            foreach (var profile in config.Profiles)
            {
                profile.IsDefault = false;
            }

            config.Profiles[0].IsDefault = true;
            if (string.IsNullOrWhiteSpace(config.Profiles[0].Name))
            {
                config.Profiles[0].Name = "Default profile";
            }
            changed = true;
        }

        var validGuids = config.Profiles.Select(x => x.GUID).Where(x => !string.IsNullOrWhiteSpace(x)).ToHashSet();
        foreach (var jobMapping in config.JobProfiles.Where(x => !validGuids.Contains(x.Value)).ToArray())
        {
            config.JobProfiles.Remove(jobMapping.Key);
            changed = true;
        }

        if (config.Version != CurrentVersion)
        {
            config.Version = CurrentVersion;
            changed = true;
        }

        return config;
    }

    private static bool NormalizeProfile(Profile profile, int index)
    {
        var changed = false;
        var defaults = new Profile();

        if (string.IsNullOrWhiteSpace(profile.Name))
        {
            profile.Name = index == 0 ? "Default profile" : $"Profile {index + 1}";
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(profile.GUID))
        {
            profile.GUID = Guid.NewGuid().ToString();
            changed = true;
        }

        changed |= EnsureReference(ref profile.Stats, new Dictionary<uint, Stats>());
        changed |= EnsureReference(ref profile.CurrentEncounterStats, new Stats());

        foreach (var statKey in profile.Stats.Where(x => x.Value == null).Select(x => x.Key).ToArray())
        {
            profile.Stats[statKey] = new Stats();
            changed = true;
        }

        changed |= ClampFloat(ref profile.MeleeSkillAtk, 0.1f, 10f, defaults.MeleeSkillAtk);
        changed |= ClampFloat(ref profile.MeleeAutoAtk, 0.1f, 10f, defaults.MeleeAutoAtk);
        changed |= ClampFloat(ref profile.CompassDistance, 0.1f, 20f, defaults.CompassDistance);
        changed |= ClampFloat(ref profile.CompassFontScale, 0.25f, 4f, defaults.CompassFontScale);

        if (!profile.Radius2 && !profile.Radius3)
        {
            profile.Radius3 = true;
            changed = true;
        }

        changed |= NormalizeBrush(ref profile.CurrentPieSettings, defaults.CurrentPieSettings);
        changed |= NormalizeBrush(ref profile.CurrentPieSettingsFlank, defaults.CurrentPieSettingsFlank);
        changed |= NormalizeBrush(ref profile.AnticipatedPieSettings, defaults.AnticipatedPieSettings);
        changed |= NormalizeBrush(ref profile.AnticipatedPieSettingsFlank, defaults.AnticipatedPieSettingsFlank);
        changed |= NormalizeBrush(ref profile.MaxMeleeSettingsN, defaults.MaxMeleeSettingsN);
        changed |= NormalizeBrush(ref profile.PlayerDotSettings, defaults.PlayerDotSettings);
        changed |= NormalizeBrush(ref profile.PlayerRingSettings, defaults.PlayerRingSettings);
        changed |= NormalizeBrush(ref profile.PartyDotSettings, defaults.PartyDotSettings);
        changed |= NormalizeBrush(ref profile.AllDotSettings, defaults.AllDotSettings);
        changed |= NormalizeBrush(ref profile.FrontSegmentIndicator, defaults.FrontSegmentIndicator);

        changed |= NormalizeColor(ref profile.MaxMeleeSettingsS, defaults.MaxMeleeSettingsS);
        changed |= NormalizeColor(ref profile.MaxMeleeSettingsE, defaults.MaxMeleeSettingsE);
        changed |= NormalizeColor(ref profile.MaxMeleeSettingsW, defaults.MaxMeleeSettingsW);
        changed |= NormalizeColor(ref profile.CompassColorN, defaults.CompassColorN);
        changed |= NormalizeColor(ref profile.CompassColor, defaults.CompassColor);

        if (profile.CurrentPieSettingsFlank.DisplayCondition != profile.CurrentPieSettings.DisplayCondition)
        {
            profile.CurrentPieSettingsFlank.DisplayCondition = profile.CurrentPieSettings.DisplayCondition;
            changed = true;
        }

        if (profile.AnticipatedPieSettingsFlank.DisplayCondition != profile.AnticipatedPieSettings.DisplayCondition)
        {
            profile.AnticipatedPieSettingsFlank.DisplayCondition = profile.AnticipatedPieSettings.DisplayCondition;
            changed = true;
        }

        return changed;
    }

    private static bool NormalizeVisualFeedback(VisualFeedbackSettings settings)
    {
        var changed = false;
        var defaults = new VisualFeedbackSettings();

        if (!Enum.IsDefined(typeof(VisualFeedbackMode), settings.Mode))
        {
            settings.Mode = defaults.Mode;
            changed = true;
        }

        var iconSize = settings.IconSize;
        changed |= ClampFloat(ref iconSize, 5f, 100f, defaults.IconSize);
        settings.IconSize = iconSize;

        var successColor = settings.SuccessColor;
        changed |= NormalizeColor(ref successColor, defaults.SuccessColor);
        settings.SuccessColor = successColor;

        var failureColor = settings.FailureColor;
        changed |= NormalizeColor(ref failureColor, defaults.FailureColor);
        settings.FailureColor = failureColor;

        return changed;
    }

    private static bool NormalizeAudioFeedback(AudioFeedbackSettings settings)
    {
        var changed = false;
        var defaults = new AudioFeedbackSettings();

        if (settings.SuccessSoundId == 0 || settings.SuccessSoundId > 16)
        {
            settings.SuccessSoundId = defaults.SuccessSoundId;
            changed = true;
        }

        if (settings.FailureSoundId == 0 || settings.FailureSoundId > 16)
        {
            settings.FailureSoundId = defaults.FailureSoundId;
            changed = true;
        }

        return changed;
    }

    private static bool EnsureReference<T>(ref T field, T fallback) where T : class
    {
        if (field != null)
        {
            return false;
        }

        field = fallback;
        return true;
    }

    private static bool NormalizeBrush(ref Brush brush, Brush fallback)
    {
        var changed = false;

        if (!Enum.IsDefined(typeof(DisplayCondition), brush.DisplayCondition))
        {
            brush.DisplayCondition = fallback.DisplayCondition;
            changed = true;
        }

        changed |= NormalizeColor(ref brush.Color, fallback.Color);
        changed |= NormalizeColor(ref brush.Fill, fallback.Fill);
        changed |= ClampFloat(ref brush.Thickness, 0f, 10f, fallback.Thickness);

        return changed;
    }

    private static bool NormalizeColor(ref Vector4 value, Vector4 fallback)
    {
        if (!IsFinite(value))
        {
            value = fallback;
            return true;
        }

        var normalized = new Vector4(
            Math.Clamp(value.X, 0f, 1f),
            Math.Clamp(value.Y, 0f, 1f),
            Math.Clamp(value.Z, 0f, 1f),
            Math.Clamp(value.W, 0f, 1f)
        );

        if (normalized != value)
        {
            value = normalized;
            return true;
        }

        return false;
    }

    private static bool ClampFloat(ref float value, float min, float max, float fallback)
    {
        if (!float.IsFinite(value))
        {
            value = fallback;
            return true;
        }

        var clamped = Math.Clamp(value, min, max);
        if (Math.Abs(clamped - value) > float.Epsilon)
        {
            value = clamped;
            return true;
        }

        return false;
    }

    private static bool IsFinite(Vector4 value)
    {
        return float.IsFinite(value.X)
            && float.IsFinite(value.Y)
            && float.IsFinite(value.Z)
            && float.IsFinite(value.W);
    }
}
