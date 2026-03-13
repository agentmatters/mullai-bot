# Telegram Bot Integration Guide

This guide explains how the `Mullai.Channels.Telegram` adapter integrates with the `Mullai.Channels.Core` architecture to enable communication between Telegram users and Mullai AI Agents using Long Polling.

## Architecture Overview

### 1. `Mullai.Channels.Core`
- **Abstractions:** The `IChannelAdapter` interface and the normalized `ChannelMessage` model form the core abstractions. This design allows any messaging platform (e.g., WhatsApp, Teams, Telegram) to implement these interfaces and communicate with Mullai AI Agents.
- **Orchestration:** The `ChannelManager` handles routing incoming messages from adapters to user-specific sessions via the `AgentFactory`. The agent's response is then routed back to the appropriate channel.

### 2. `Mullai.Channels.Api`
- This project serves as the host application for the Mullai agents and channels.
- Dependencies for `Mullai.Channels.Telegram` and `Mullai.Agents` are registered in its dependency injection container.
- It instantiates the `ChannelManager` on startup to ensure all events are correctly wired up.

### 3. `Mullai.Channels.Telegram`
- An isolated C# class library utilizing the `Telegram.Bot` NuGet package.
- The `TelegramChannelAdapter` runs as a Background Service (`IHostedService`) that continuously polls Telegram's servers for new messages.
- It receives raw update objects, transforms them into `ChannelMessage` instances, forwards them to the agent via `OnMessageReceived`, and invokes the Telegram API to send responses back.

## How to Test and Run the Telegram Chatbot

To see your agent live on Telegram running from your local machine, follow these steps:

### 1. Get a Telegram Bot Token
- Go to Telegram and message `@BotFather`. Use the command `/newbot` to create your own bot, and copy the `HTTP API Token`.

### 2. Configure App Settings
- Open `Mullai.Channels.Api/appsettings.json`.
- Under the `"Telegram"` block, paste your bot token into the `"BotToken"` field.

### 3. Run the API Application
Because the application uses **Long Polling** rather than webhooks, you **do not** need to expose your local port to the internet (e.g., you do not need Ngrok or port forwarding). The application will reach out to Telegram directly.

- Run the API application:
  ```bash
  cd Mullai.Channels.Api
  dotnet run
  ```

### 4. Chat with your Agent
- Open Telegram, search for your bot's username, click **Start**, and send it a message. It should immediately respond via the Mullai Assistant!
