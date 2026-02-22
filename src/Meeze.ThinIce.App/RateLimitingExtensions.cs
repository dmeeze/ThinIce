using Microsoft.AspNetCore.RateLimiting;

namespace Meeze.ThinIce.App;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddThinIceRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddRateLimiter(limiter =>
        {
            var throttling = new ThrottlingOptions();
            var section = configuration.GetSection("Throttling");
            if (int.TryParse(section["ConfigPermitsPerMinute"], out var configPermits))
                throttling.ConfigPermitsPerMinute = configPermits;
            if (int.TryParse(section["AuthenticatedPermitsPerMinute"], out var authPermits))
                throttling.AuthenticatedPermitsPerMinute = authPermits;

            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            limiter.AddSlidingWindowLimiter("config", options =>
            {
                options.PermitLimit = throttling.ConfigPermitsPerMinute;
                options.Window = TimeSpan.FromMinutes(1);
                options.SegmentsPerWindow = 6;
                options.QueueLimit = 0;
            });

            limiter.AddSlidingWindowLimiter("authenticated", options =>
            {
                options.PermitLimit = throttling.AuthenticatedPermitsPerMinute;
                options.Window = TimeSpan.FromMinutes(1);
                options.SegmentsPerWindow = 6;
                options.QueueLimit = 0;
            });
        });

        return services;
    }
}
