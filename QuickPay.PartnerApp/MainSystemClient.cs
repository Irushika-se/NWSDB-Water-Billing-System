using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace QuickPay.PartnerApp;

/// <summary>
/// The ONLY place that talks to the NWSDB main system. QuickPay holds no login credentials
/// of its own: every authenticated call is made with the logged-in QuickPay account's own
/// NWSDB JWT, obtained when it logged in (POST /api/auth/quickpay/login) and sent back to
/// QuickPay on every request afterwards (see Program.cs). NWSDB is the one and only place
/// that verifies that token and enforces which Customer ID(s) it is authorized for - this
/// class simply relays the request and the token, it never substitutes a token of its own.
/// </summary>
public class MainSystemClient
{
    private readonly HttpClient _http;
    private readonly MainSystemOptions _opt;

    public MainSystemClient(IOptions<MainSystemOptions> options)
    {
        _opt = options.Value;
        _http = new HttpClient
        {
            BaseAddress = new Uri(_opt.BaseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(20)
        };
    }

    public string BaseUrl => _opt.BaseUrl;

    // Unauthenticated call - used only for the Customer Login relay itself.
    public Task<MainResponse> SendAnonymousAsync(HttpMethod method, string path, object? body = null) =>
        SendRawAsync(method, path, body, token: null);

    // Authenticated call made with the CALLER's own JWT - never a token QuickPay holds itself.
    public Task<MainResponse> SendAsUserAsync(HttpMethod method, string path, string token, object? body = null) =>
        SendRawAsync(method, path, body, token);

    private async Task<MainResponse> SendRawAsync(HttpMethod method, string path, object? body, string? token)
    {
        using var request = new HttpRequestMessage(method, path);
        if (token != null) request.Headers.Authorization = new("Bearer", token);
        if (body != null) request.Content = JsonContent.Create(body);

        try
        {
            using var response = await _http.SendAsync(request);
            var text = await response.Content.ReadAsStringAsync();
            return MainResponse.From(response.StatusCode, text);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new MainSystemException(503,
                $"Cannot reach the NWSDB main system at {_opt.BaseUrl}. Is the NWSDB API running?");
        }
    }
}
