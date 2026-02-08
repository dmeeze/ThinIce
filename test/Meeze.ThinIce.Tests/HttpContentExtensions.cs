using System.Net.Http.Json;

namespace Meeze.ThinIce.Tests;

internal static class HttpContentExtensions
{
    public static async Task<T> ReadJsonAsync<T>(this HttpContent content)
    {
        return await content.ReadFromJsonAsync<T>()
            ?? throw new AssertFailedException($"Expected non-null {typeof(T).Name} in response body");
    }
}
