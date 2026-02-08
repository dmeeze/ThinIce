namespace Meeze.ThinIce.App;

public sealed class ThrottlingOptions
{
    public int ConfigPermitsPerMinute { get; set; } = 100;
    public int AuthenticatedPermitsPerMinute { get; set; } = 10_000;
}
