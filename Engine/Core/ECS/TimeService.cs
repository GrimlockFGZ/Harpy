using System.Diagnostics;

namespace Engine.Core.Core.ECS;

public sealed class TimeService
{
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

    public float DeltaTime { get; set; }
    public double TotalTime { get; set; }
    private double _lastTime;
    
    public void GetTimeElapsed()
    {
        var currentTime = _stopwatch.Elapsed.TotalSeconds;
        DeltaTime = (float)(currentTime - _lastTime);
        TotalTime = currentTime;
        _lastTime = currentTime;
    }
}