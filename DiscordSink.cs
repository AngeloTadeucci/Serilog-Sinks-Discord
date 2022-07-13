using Discord;
using Discord.Webhook;
using Serilog.Core;
using Serilog.Events;
using System;

namespace Serilog.Sinks.Discord
{
    public class DiscordSink : ILogEventSink
    {
        private readonly IFormatProvider _formatProvider;
        private readonly ulong _webhookId;
        private readonly string _webhookToken;
        private readonly LogEventLevel _restrictedToMinimumLevel;

        public DiscordSink(
            IFormatProvider formatProvider,
            ulong webhookId,
            string webhookToken,
            LogEventLevel restrictedToMinimumLevel = LogEventLevel.Information)
        {
            _formatProvider = formatProvider;
            _webhookId = webhookId;
            _webhookToken = webhookToken;
            _restrictedToMinimumLevel = restrictedToMinimumLevel;
        }

        public void Emit(LogEvent logEvent)
        {
            SendMessage(logEvent);
        }

        private void SendMessage(LogEvent logEvent)
        {
            if (!ShouldLogMessage(_restrictedToMinimumLevel, logEvent.Level))
                return;

            EmbedBuilder embedBuilder = new();
            DiscordWebhookClient webHook = new(_webhookId, _webhookToken);

            try
            {
                if (logEvent.Exception != null)
                {
                    embedBuilder.Color = new Color(255, 0, 0);
                    embedBuilder.WithTitle(":o: Exception");
                    embedBuilder.AddField("Type:", $"```{logEvent.Exception.GetType().FullName}```");

                    string message = FormatMessage(logEvent.Exception.Message, 2000);
                    embedBuilder.AddField("Message:", message);

                    string stackTrace = FormatMessage(logEvent.Exception.StackTrace, 2000);
                    embedBuilder.AddField("StackTrace:", stackTrace);

                    webHook.SendMessageAsync(null, false, new[] { embedBuilder.Build() })
                        .GetAwaiter()
                        .GetResult();
                }
                else
                {
                    string message = logEvent.RenderMessage(_formatProvider);

                    message = FormatMessage(message, 480);

                    SpecifyEmbedLevel(logEvent.Level, embedBuilder);

                    embedBuilder.Description = message;

                    webHook.SendMessageAsync(
                        null, false, new[] { embedBuilder.Build() })
                        .GetAwaiter()
                        .GetResult();
                }
            }

            catch (Exception ex)
            {
                webHook.SendMessageAsync(
                    $"ooo snap, {ex.Message}", false)
                    .GetAwaiter()
                    .GetResult();
            }
        }
        private static void SpecifyEmbedLevel(LogEventLevel level, EmbedBuilder embedBuilder)
        {
            switch (level)
            {
                case LogEventLevel.Verbose:
                    embedBuilder.Title = ":loud_sound: Verbose";
                    embedBuilder.Color = Color.LightGrey;
                    break;
                case LogEventLevel.Debug:
                    embedBuilder.Title = ":mag: Debug";
                    embedBuilder.Color = Color.LightGrey;
                    break;
                case LogEventLevel.Information:
                    embedBuilder.Title = ":information_source: Information";
                    embedBuilder.Color = new Color(0, 186, 255);
                    break;
                case LogEventLevel.Warning:
                    embedBuilder.Title = ":warning: Warning";
                    embedBuilder.Color = new Color(255, 204, 0);
                    break;
                case LogEventLevel.Error:
                    embedBuilder.Title = ":x: Error";
                    embedBuilder.Color = new Color(255, 0, 0);
                    break;
                case LogEventLevel.Fatal:
                    embedBuilder.Title = ":skull_crossbones: Fatal";
                    embedBuilder.Color = Color.DarkRed;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(level), level, null);
            }
        }

        private static string FormatMessage(string message, int maxLenght)
        {
            if (message.Length > maxLenght)
                message = $"{message[..maxLenght]} ...";

            if (!string.IsNullOrWhiteSpace(message))
                message = $"```{message}```";

            return message;
        }

        private static bool ShouldLogMessage(
            LogEventLevel minimumLogEventLevel,
            LogEventLevel messageLogEventLevel) =>
                (int)messageLogEventLevel >= (int)minimumLogEventLevel;
    }
}
