using System;
using System.Net.Http;

internal static class HttpResponseMessageExtensions
{
    private static readonly object obj = new object();

    public static void WriteRequestToConsole(this HttpResponseMessage response)
    {
        if (response is null)
        {
            return;
        }

        lock (obj)
        {
            var request = response.RequestMessage;
            Console.Write($"[{DateTime.Now:HH:mm:ss}] ");
            Console.Write($"{request?.Method} ");
            Console.Write($"{request?.RequestUri} ");
            Console.WriteLine($"HTTP/{request?.Version}");

            Console.WriteLine($"           {(int)response.StatusCode} {response.ReasonPhrase}");
        }
    }
}