using Pictomancy;

namespace Avarice.Drawing;

internal static class PictomancyRenderer
{
    private static PctDrawList _drawList;
    private static DateTime _disabledUntilUtc;
    private static DateTime _lastFailureLogUtc;
    private static int _consecutiveFailures;

    public static bool IsDrawing => _drawList != null;
    public static bool InFailureBackoff => DateTime.UtcNow < _disabledUntilUtc;
    public static TimeSpan FailureBackoffRemaining => InFailureBackoff ? _disabledUntilUtc - DateTime.UtcNow : TimeSpan.Zero;
    public static int ConsecutiveFailures => _consecutiveFailures;

    public static void BeginFrame()
    {
        EndFrame();

        if (!P.config.UsePictomancyRenderer)
        {
            return;
        }

        if (InFailureBackoff)
        {
            return;
        }

        try
        {
            var hints = new PctDrawHints(
                autoDraw: true,
                maxAlpha: P.config.PictomancyMaxAlpha,
                clipNativeUI: P.config.PictomancyClipNativeUI
            );
            _drawList = PictoService.Draw(ImGui.GetWindowDrawList(), hints);
            _consecutiveFailures = 0;
            _disabledUntilUtc = DateTime.MinValue;
        }
        catch (Exception ex)
        {
            _consecutiveFailures++;
            var now = DateTime.UtcNow;
            var backoff = TimeSpan.FromSeconds(Math.Min(30, Math.Max(2, _consecutiveFailures * 2)));
            _disabledUntilUtc = now.Add(backoff);

            if (now - _lastFailureLogUtc >= TimeSpan.FromSeconds(10))
            {
                PluginLog.Error($"Failed to get Pictomancy draw list: {ex.Message}. Falling back to standard rendering for {backoff.TotalSeconds:0}s.");
                _lastFailureLogUtc = now;
            }

            _drawList = null;
        }
    }

    public static void EndFrame()
    {
        if (_drawList == null) return;
        _drawList.Dispose();
        _drawList = null;
    }

    public static void DrawCircle(Vector3 center, float radius, uint color, float thickness)
    {
        if (_drawList == null) return;
        _drawList.AddCircle(center, radius, color, thickness: thickness);
    }

    public static void DrawCircleFilled(Vector3 center, float radius, uint color)
    {
        if (_drawList == null) return;
        _drawList.AddCircleFilled(center, radius, color);
    }

    public static void DrawFan(Vector3 center, float innerRadius, float outerRadius, float startRads, float endRads, uint color, float thickness)
    {
        if (_drawList == null) return;
        _drawList.AddFan(center, innerRadius, outerRadius, startRads, endRads, color, thickness: thickness);
    }

    public static void DrawFanFilled(Vector3 center, float innerRadius, float outerRadius, float startRads, float endRads, uint color)
    {
        if (_drawList == null) return;
        _drawList.AddFanFilled(center, innerRadius, outerRadius, startRads, endRads, color);
    }

    public static void DrawDot(Vector3 position, float size, uint color)
    {
        if (_drawList == null) return;
        _drawList.AddDot(position, size, color);
    }

    public static void PathLineTo(Vector3 point)
    {
        _drawList?.PathLineTo(point);
    }

    public static void PathStroke(uint color, float thickness, bool closed = false)
    {
        if (_drawList == null) return;
        _drawList.PathStroke(color, closed ? PctStrokeFlags.Closed : PctStrokeFlags.None, thickness);
    }

    public static void PathArcTo(Vector3 center, float radius, float startAngle, float endAngle)
    {
        _drawList?.PathArcTo(center, radius, startAngle, endAngle);
    }
}
