using System;
using System.Collections.Generic;
using Steamworks;
using Logger = Rocket.Core.Logging.Logger;
using ShimmyMySherbet.DiscordWebhooks;
using System.Drawing;
using ShimmyMySherbet.DiscordWebhooks.Models;

namespace nexusUT
{
    public static class DiscordHelper
    {
        public static async void SendJailLog(string officerName, string targetName, CSteamID targetId, int time, uint bail, string reason, string url)
        {
            try
            {
                WebhookMessage message = new WebhookMessage()
                    .WithUsername("Jail Logs")
                    .WithAvatar("https://imgur.com/NktP6Sn.png")
                    .PassEmbed()
                        .WithTitle("⚖️ Player Jailed")
                        .WithDescription($"**{targetName}** has been jailed by **{officerName}**.")
                        .WithColor(new EmbedColor(0, 0, 255))
                        .WithTimestamp(DateTime.Now)
                        .WithField("Reason", reason, false)
                        .WithField("Duration", $"{time} seconds", true)
                        .WithField("Bail Amount", $"${bail}", true)
                        .WithFooter($"Player ID: {targetId}")
                        .Finalize();

                await DiscordWebhookService.PostMessageAsync(url, message);
            }
            catch (Exception ex)
            {
                Logger.LogError($"[PoliceUT] Webhook failed to send: {ex.Message}");
            }
        }
        public static async void SendFineLog(string officerName, string targetName, CSteamID targetId, uint amount, string reason, string url)
        {
            try
            {
                WebhookMessage message = new WebhookMessage()
                    .WithUsername("Fine Logs")
                    .WithAvatar("https://imgur.com/NktP6Sn.png")
                    .PassEmbed()
                        .WithTitle("💸 Player Fined")
                        .WithDescription($"**{targetName}** has been fined by **{officerName}**.")
                        .WithColor(new EmbedColor(0, 0, 255))
                        .WithTimestamp(DateTime.Now)
                        .WithField("Amount", $"${amount}", true)
                        .WithField("Reason", reason, true)
                        .WithFooter($"Player ID: {targetId}")
                        .Finalize();

                await DiscordWebhookService.PostMessageAsync(url, message);
            }
            catch (Exception ex)
            {
                Logger.LogError($"[PoliceUT] Webhook failed to send: {ex.Message}");
            }
        }
        public static async void SendArrestLog(string issuerName, string targetName, CSteamID targetId, string reason, uint fineAmount, string url)
        {
            try
            {
                WebhookMessage message = new WebhookMessage()
                    .WithUsername("Arrest Logs")
                    .WithAvatar("https://imgur.com/NktP6Sn.png")
                    .PassEmbed()
                        .WithTitle("👮 Player Arrested")
                        .WithDescription($"**{issuerName}** has arrested **{targetName}**.")
                        .WithColor(new EmbedColor(0, 0, 255))
                        .WithTimestamp(DateTime.Now)
                        .WithField("Reason / Charges", reason, false)
                        .WithField("Associated Fine", $"${fineAmount}", true)
                        .WithFooter($"Target Player ID: {targetId}")
                        .Finalize();

                await DiscordWebhookService.PostMessageAsync(url, message);
            }
            catch (Exception ex)
            {
                Logger.LogError($"[PoliceUT] Arrest Log Webhook failed to send: {ex.Message}");
            }
        }
    }
}