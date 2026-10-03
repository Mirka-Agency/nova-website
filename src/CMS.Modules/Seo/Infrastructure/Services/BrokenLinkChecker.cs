using CMS.Modules.Seo.Application.Interfaces;
using CMS.Modules.Seo.Application.Tools;

namespace CMS.Modules.Seo.Infrastructure.Services;

public sealed class BrokenLinkChecker : IBrokenLinkChecker
{
    public const string HttpClientName = "SeoBrokenLink";

    private readonly IHttpClientFactory _httpClientFactory;

    public BrokenLinkChecker(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IReadOnlyList<BrokenLinkCheckResult>> CheckAsync(
        IEnumerable<string> urls,
        CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);
        var distinct = urls
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .Select(u => u.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(30)
            .ToList();

        var results = new List<BrokenLinkCheckResult>(distinct.Count);
        foreach (var url in distinct)
        {
            results.Add(await CheckOneAsync(client, url, cancellationToken));
        }

        return results;
    }

    private static async Task<BrokenLinkCheckResult> CheckOneAsync(
        HttpClient client,
        string url,
        CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return new BrokenLinkCheckResult(url, null, false, "آدرس نامعتبر است.");
        }

        try
        {
            using var head = new HttpRequestMessage(HttpMethod.Head, uri);
            using var headResponse = await client.SendAsync(head, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (headResponse.IsSuccessStatusCode
                || (int)headResponse.StatusCode is >= 300 and < 400)
            {
                return new BrokenLinkCheckResult(url, (int)headResponse.StatusCode, true, null);
            }

            // Some hosts reject HEAD — fall back to GET.
            if ((int)headResponse.StatusCode is 405 or 403 or 501)
            {
                using var get = new HttpRequestMessage(HttpMethod.Get, uri);
                using var getResponse = await client.SendAsync(get, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                var ok = getResponse.IsSuccessStatusCode || (int)getResponse.StatusCode is >= 300 and < 400;
                return new BrokenLinkCheckResult(url, (int)getResponse.StatusCode, ok, ok ? null : "لینک در دسترس نیست.");
            }

            return new BrokenLinkCheckResult(url, (int)headResponse.StatusCode, false, "لینک در دسترس نیست.");
        }
        catch (TaskCanceledException)
        {
            return new BrokenLinkCheckResult(url, null, false, "زمان درخواست به پایان رسید.");
        }
        catch (HttpRequestException ex)
        {
            return new BrokenLinkCheckResult(url, null, false, ex.Message);
        }
        catch (Exception ex)
        {
            return new BrokenLinkCheckResult(url, null, false, ex.Message);
        }
    }
}
