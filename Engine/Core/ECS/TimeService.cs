using System.Diagnostics;

namespace Engine.Core.Core.ECS;

public sealed class TimeService
{
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

    public float DeltaTime { get; set; }
    public double TotalTime { get; set; }
    private double _lastTime;
    
    public float GetDeltaTime()
    {
        var currentTime = _stopwatch.Elapsed.TotalSeconds;
        DeltaTime = (float)(currentTime - _lastTime);
        TotalTime = currentTime;
        _lastTime = currentTime;
        return DeltaTime;
    }
    public void Reset(){ DeltaTime =0; TotalTime =0; _lastTime =0;}
}
