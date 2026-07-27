using Serilog;
using StatisticsAnalysisTool.Common.UserSettings;
using StatisticsAnalysisTool.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Threading.Tasks;

namespace StatisticsAnalysisTool.Notification;

/// <summary>
/// Sends optional alerts (loot, death, dungeon closed) to a user-configured Discord channel webhook.
/// Each call is gated by its own settings toggle and silently no-ops if no webhook URL is configured.
/// </summary>
public static class DiscordWebhookService
{
    private const int ColorLoot = 0x3498DB;
    private const int ColorDeath = 0xE74C3C;
    private const int ColorDungeonClosed = 0xF2B83B;

    public static async Task SendLootAsync(string lootedByName, Item item, int quantity)
    {
        if (!SettingsController.CurrentSettings.IsDiscordWebhookLootActive || item is null)
        {
            return;
        }

        await SendEmbedAsync("Loot", $"**{lootedByName}** looted {quantity}x **{item.LocalizedName}**", ColorLoot);
    }

    public static async Task SendDeathAsync(string characterName, string killedBy)
    {
        if (!SettingsController.CurrentSettings.IsDiscordWebhookDeathActive)
        {
            return;
        }

        await SendEmbedAsync("Death", $"**{characterName}** was killed by **{killedBy}**", ColorDeath);
    }

    public static async Task SendDungeonClosedAsync()
    {
        if (!SettingsController.CurrentSettings.IsDiscordWebhookDungeonClosedActive)
        {
            return;
        }

        await SendEmbedAsync("Dungeon closed", "The dungeon entrance has closed.", ColorDungeonClosed);
    }

    private static async Task SendEmbedAsync(string title, string description, int color)
    {
        var webhookUrl = SettingsController.CurrentSettings.DiscordWebhookUrl;
        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            return;
        }

        try
        {
            using var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(15)
            };

            var payload = new
            {
                embeds = new List<object>
                {
                    new
                    {
                        title,
                        description,
                        color,
                        timestamp = DateTime.UtcNow.ToString("o")
                    }
                }
            };

            var response = await client.PostAsJsonAsync(webhookUrl, payload);
            if (!response.IsSuccessStatusCode)
            {
                Log.Warning("Discord webhook call failed with status {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "{message}", MethodBase.GetCurrentMethod()?.DeclaringType);
        }
    }
}
