using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

namespace RideWaitTime.Business;

public class Notifier : INotifier
{
    private readonly IConfiguration _config;
    private readonly HttpClient _slack;
    
    public Notifier(IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _config = config;
        _slack = httpClientFactory.CreateClient();
    }

    public async Task NotifyAsync(string message)
    {
        Console.WriteLine(message);
        var slackHookUrl = _config["Slack:HookUrl"];
        if (string.IsNullOrWhiteSpace(slackHookUrl))
        {
            throw new Exception("Slack:HookUrl is missing");
        }
        await _slack.PostAsJsonAsync(slackHookUrl, new SlackMessage(message));
    }

    private record SlackMessage(string Text);
}