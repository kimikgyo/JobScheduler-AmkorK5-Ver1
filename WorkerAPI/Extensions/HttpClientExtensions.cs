using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

internal static class HttpClientExtensions
{
    public static async Task<T> GetFromJsonAsync<T>(this HttpClient httpClient, string uri, JsonSerializerSettings settings = null, CancellationToken cancellationToken = default)
    {
        ThrowIfInvalidParams(httpClient, uri);

        var response = await httpClient.GetAsync(uri, cancellationToken);

        response.WriteRequestToConsole();
        response.EnsureSuccessStatusCode();

        using (var streamReader = new StreamReader(await response.Content.ReadAsStreamAsync()))
        using (var jsonReader = new JsonTextReader(streamReader))
        {
            return JsonSerializer.Create(settings).Deserialize<T>(jsonReader);
        }
    }

    public static async Task<HttpResponseMessage> PostAsJsonAsync<T>(this HttpClient httpClient, string uri, T value, JsonSerializerSettings settings = null, CancellationToken cancellationToken = default)
    {
        ThrowIfInvalidParams(httpClient, uri);

        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var jsonContent = new StringContent(JsonConvert.SerializeObject(value, settings), Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync(uri, jsonContent, cancellationToken);

        response.WriteRequestToConsole();
        response.EnsureSuccessStatusCode();

        return response;
    }

    public static async Task<HttpResponseMessage> PutAsJsonAsync<T>(this HttpClient httpClient, string uri, T value, JsonSerializerSettings settings = null, CancellationToken cancellationToken = default)
    {
        ThrowIfInvalidParams(httpClient, uri);

        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var jsonContent = new StringContent(JsonConvert.SerializeObject(value, settings), Encoding.UTF8, "application/json");

        var response = await httpClient.PutAsync(uri, jsonContent, cancellationToken);

        response.WriteRequestToConsole();
        response.EnsureSuccessStatusCode();

        return response;
    }

    public static async Task<HttpResponseMessage> DeleteFromJsonAsync(this HttpClient httpClient, string uri, CancellationToken cancellationToken = default)
    {
        ThrowIfInvalidParams(httpClient, uri);

        var response = await httpClient.DeleteAsync(uri, cancellationToken);

        response.WriteRequestToConsole();
        response.EnsureSuccessStatusCode();

        return response;
    }

    private static void ThrowIfInvalidParams(HttpClient httpClient, string uri)
    {
        if (httpClient == null)
        {
            throw new ArgumentNullException(nameof(httpClient));
        }

        if (string.IsNullOrWhiteSpace(uri))
        {
            throw new ArgumentException("Can't be null or empty", nameof(uri));
        }
    }
}