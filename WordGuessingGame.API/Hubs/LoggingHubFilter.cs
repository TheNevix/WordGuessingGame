using Microsoft.AspNetCore.SignalR;

namespace WordGuessingGame.API.Hubs
{
    // Logs any exception thrown by a hub method so failures surface in the logs/Grafana
    // instead of being quietly converted into a client-side error.
    public class LoggingHubFilter : IHubFilter
    {
        private readonly ILogger<LoggingHubFilter> _logger;

        public LoggingHubFilter(ILogger<LoggingHubFilter> logger)
        {
            _logger = logger;
        }

        public async ValueTask<object?> InvokeMethodAsync(
            HubInvocationContext invocationContext,
            Func<HubInvocationContext, ValueTask<object?>> next)
        {
            try
            {
                return await next(invocationContext);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[HUB] Error in {Method} (connId={ConnId})",
                    invocationContext.HubMethodName,
                    invocationContext.Context.ConnectionId);
                throw;
            }
        }
    }
}
