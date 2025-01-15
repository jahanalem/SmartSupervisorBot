# SmartSupervisorBot

## Overview
SmartSupervisorBot is a Telegram bot that utilizes OpenAI's API to offer real-time text correction and translation into German. 
This bot aims to enhance communication by providing grammatical corrections and facilitating language translation directly within Telegram chats.

## Features
- **Text Correction**: Corrects German grammatical errors in messages.
- **Language Translation**: Translates text into German if detected language is not German.
- **Interactive**: Responds interactively in Telegram chats and can send reactions to corrected messages.

## Technologies
- **.NET 9**
- **Telegram Bot API**
- **OpenAI API**

## Configuration
The bot requires configuration settings which include:
- `BotToken`: Your Telegram bot token.
- `OpenAiToken`: Your OpenAI API key.
- Configuration files: `appsettings.json` for production and `appsettings.Development.json` for development environments.

## Getting Started
To run SmartSupervisorBot:
1. Clone the repo:
git clone https://github.com/jahanalem/SmartSupervisorBot.git
2. Install dependencies:
dotnet restore
3. Start the bot:
dotnet run



