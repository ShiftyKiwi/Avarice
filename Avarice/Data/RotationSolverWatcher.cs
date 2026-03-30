using System.Diagnostics;
using Avarice.StaticData;
using ECommons.EzIpcManager;

namespace Avarice.Data;

internal class RotationSolverWatcher 
{
    private readonly EzIPCDisposalToken[] _ipcTokens;

    public RotationSolverWatcher() 
    {
        _ipcTokens = EzIPC.Init(this);
    }

    public bool Available { get; private set; }
    private readonly Stopwatch DataAge = new();
    private uint _nextGcdActionId;
    public uint NextGCDActionId 
    {
        get => DataAge.ElapsedMilliseconds < 5000 ? _nextGcdActionId : 0;
        private set 
        {
            Available = true;
            Svc.Log.Debug($"Next GCD Action: {value}");
            DataAge.Restart();
            _nextGcdActionId = value;
        }
    }

    [EzIPCEvent("RotationSolverReborn.ActionUpdater.NextActionChanged", false)]
    public void NextGCDActionChanged(uint action) 
    {
        NextGCDActionId = action;
    }

    public bool TryGetNextGCDActionId(out ActionID o) 
    {
        o = (ActionID) NextGCDActionId;
        return o != 0;
    }

    public void Dispose()
    {
        foreach (var token in _ipcTokens)
        {
            token.Dispose();
        }
    }
}
