using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mullai.Channels.Core.Abstractions;
using Mullai.Channels.Core.Models;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace Mullai.Channels.Telegram;

public class TelegramChannelAdapter : IChannelAdapter, IHostedService
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<TelegramChannelAdapter> _logger;
    private readonly TelegramOptions _options;

    public string ChannelId => "Telegram";

    public event Func<ChannelMessage, Task>? OnMessageReceived;

    public TelegramChannelAdapter(
        ITelegramBotClient botClient,
        IOptions<TelegramOptions> options,
        ILogger<TelegramChannelAdapter> logger)
    {
        _botClient = botClient ?? throw new ArgumentNullException(nameof(botClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Telegram Long Polling service...");

        _botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandlePollingErrorAsync,
            receiverOptions: new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<global::Telegram.Bot.Types.Enums.UpdateType>() // Receive all update types
            },
            cancellationToken: cancellationToken
        );

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Telegram Long Polling service...");
        return Task.CompletedTask;
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        try
        {
            if (update?.Message == null || string.IsNullOrWhiteSpace(update.Message.Text))
            {
                return; // Ignore empty or non-text messages
            }

            var channelMessage = new ChannelMessage
            {
                ChannelId = ChannelId,
                UserId = update.Message.Chat.Id.ToString(),
                TextContent = update.Message.Text,
                RawData = update
            };

            if (OnMessageReceived != null)
            {
                _logger.LogInformation("Invoking OnMessageReceived for ChatId {ChatId}", update.Message.Chat.Id);
                await OnMessageReceived.Invoke(channelMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process incoming Telegram update");
        }
    }

    private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Telegram Polling Error occurred");
        return Task.CompletedTask;
    }

    public Task ProcessIncomingMessageAsync(object requestData)
    {
        throw new NotSupportedException("TelegramChannelAdapter uses Long Polling and does not support incoming webhooks.");
    }

    public async Task SendMessageAsync(ChannelMessage response)
    {
        try
        {
            if (!long.TryParse(response.UserId, out var chatId))
            {
                _logger.LogError("Invalid UserId for Telegram chat: {UserId}", response.UserId);
                return;
            }

            await _botClient.SendMessage(
                chatId: chatId,
                text: response.TextContent
            );

            _logger.LogInformation("Sent message to ChatId {ChatId}", chatId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message to Telegram ChatId {UserId}", response.UserId);
        }
    }
}
