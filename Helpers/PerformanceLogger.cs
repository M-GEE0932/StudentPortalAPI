using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace StudentPortalAPI.Helpers
{
    public class PerformanceLogger : IDisposable
    {
        private readonly ILogger _logger;
        private readonly string _operationName;
        private readonly Stopwatch _stopwatch;
        private readonly DateTime _startTime;

        public PerformanceLogger(ILogger logger, string operationName)
        {
            _logger = logger;
            _operationName = operationName;
            _stopwatch = Stopwatch.StartNew();
            _startTime = DateTime.UtcNow;
            _logger.LogInformation($"▶ START: {_operationName} at {_startTime:HH:mm:ss.fff}");
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            var elapsed = _stopwatch.Elapsed;
            _logger.LogInformation($"◀ END: {_operationName} - Duration: {elapsed.TotalMilliseconds:F2}ms ({elapsed.TotalSeconds:F2}s)");

            if (elapsed.TotalMilliseconds > 1000)
            {
                _logger.LogWarning($"⚠ SLOW OPERATION: {_operationName} took {elapsed.TotalMilliseconds:F2}ms");
            }
        }
    }
}