using Engine.Core;

public static class AllocationTracker
{
    private static long _baselineBytes;
    private static long _peakBytes;
    private static int _isTracking;

    /// <summary>
    /// Call this at application startup (e.g., in App.axaml.cs or Main) 
    /// after the initial startup JIT compilation settles.
    /// </summary>
    public static void StartTracking()
    {
        if (Interlocked.Exchange(ref _isTracking, 1) == 1) return;

        // Force a full GC to clear out startup jitter and get a clean baseline
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();

        _baselineBytes = GC.GetTotalAllocatedBytes(precise: true);
        _peakBytes = 0;
        
        Logger.LogInfo("[AllocationTracker] Tracking started. Baseline total allocated bytes: " + _baselineBytes);
    }

    /// <summary>
    /// Call this periodically (e.g., at the end of every frame or on a background timer) 
    /// to poll total allocations and track the overall lifecycle delta.
    /// </summary>
    /// <param name="category">An optional label to identify where this check is coming from.</param>
    public static void Checkpoint(string category)
    {
        if (_isTracking == 0) return;

        long currentTotal = GC.GetTotalAllocatedBytes(precise: true);
        long lifetimeAllocated = currentTotal - _baselineBytes;
        
        if (lifetimeAllocated > _peakBytes)
        {
            _peakBytes = lifetimeAllocated;
        }

         // Optional: Log if you want continuous monitoring
         //Logger.LogInfo($"[AllocationTracker] [{category}] Total lifetime managed allocations: {lifetimeAllocated:N0} bytes");
    }

    /// <summary>
    /// Call this when you want a complete summary report of total allocations 
    /// since StartTracking() was invoked.
    /// </summary>
    public static void ReportAndReset()
    {
        long currentTotal = GC.GetTotalAllocatedBytes(precise: true);
        long lifetimeAllocated = currentTotal - _baselineBytes;

        Logger.LogInfo("=== ALLOCATION TRACKER REPORT ===");
        Logger.LogInfo($"Total Allocated Since Startup: {lifetimeAllocated:N0} bytes ({lifetimeAllocated / 1024.0 / 1024.0:F2} MB)");
        Logger.LogInfo($"Peak Allocated Footprint: {_peakBytes:N0} bytes");
        Logger.LogInfo("==================================");

        _baselineBytes = currentTotal;
        _peakBytes = 0;
    }
}