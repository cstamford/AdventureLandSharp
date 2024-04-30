namespace AdventureLandSharp.Core;

using System.Runtime.InteropServices;

public static class HighPrecisionSleep {
    [DllImport("ntdll.dll")]
    private static extern int NtDelayExecution(bool Alertable, ref long DelayInterval);

    [DllImport("ntdll.dll")]
    private static extern int ZwSetTimerResolution(uint RequestedResolution, bool Set, ref uint ActualResolution);

    private static bool _initialized = false;
    private static uint _actualResolution;

    public static void Sleep(float milliseconds) {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            if (!_initialized) {
                // Setting the system timer resolution to 1 microsecond
                uint requestedResolution = 1; // in 100-nanosecond units
                _ = ZwSetTimerResolution(requestedResolution, true, ref _actualResolution);
                _initialized = true;
            }

            // Convert milliseconds to 100-nanosecond units (1 ms = 10000 * 100ns)
            long interval = (long)(-milliseconds * 10000);
            _ = NtDelayExecution(false, ref interval);
        }
        else {
            Thread.Sleep((int)milliseconds);
        }
    }
}
