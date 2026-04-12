# SmartSupervisorBot

## Overview
SmartSupervisorBot is a Telegram bot that utilizes OpenAI's API to offer real-time text correction and translation into German. 
This bot aims to enhance communication by providing grammatical corrections and facilitating language translation directly within Telegram chats.

## Features
- **Text Correction**: Corrects German grammatical errors in messages.
- **Language Translation**: Translates text into German if detected language is not German.
- **Interactive**: Responds interactively in Telegram chats and can send reactions to corrected messages.

## Technologies
- **.NET 10**
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


---
# System Sequence Diagram: End-to-End Message Processing
<img width="7034" height="4583" alt="SmartSupervisorBot_sequenceDiagram_mermaid" src="https://github.com/user-attachments/assets/ca24536d-8dda-4f68-a331-2930521bf52c" />


 # Detailed System Workflow and Code Map

### **Phase 1: Group Configuration Phase**

* **Step 1: Admin Adds Bot to Group**
    The Telegram bot is added to a new chat group. The application detects this event and automatically registers the group into the Redis database with default settings (Inactive and zero credits).
    * *Code Reference:* `BotService.cs` (`HandleNewChatMember` method) and `RedisGroupAccess.cs` (`AddGroupAsync` method).

* **Step 2: Admin Configures Group**
    The Administrator uses the Web Dashboard to set the target language, add purchased credits, and change the group's status to Active.
    * *Code Reference:* `Program.cs` (API Endpoints: `/groups/{groupId}/language`, `/groups/{groupId}/credit`, and `/groups/{groupId}/active`).

### **Phase 2: Live Processing Phase**

* **Step 3: User Sends Message**
    A user types a text message in the Telegram group that requires correction or translation (triggering the bot by ending the text with `..` or `。。`).
    * *Code Reference:* Initiated externally by the user on Telegram.

* **Step 4: Telegram Forwards Message**
    The Telegram Platform catches the message and forwards the data (Text and Group ID) to your C# Bot Engine.
    * *Code Reference:* `BotService.cs` (`HandleUpdateAsync` method routes it to the `ProcessTextMessage` method).

* **Step 5: Engine Queries Database**
    The application queries the Redis database to check the specific settings and activation status for that Group ID.
    * *Code Reference:* `BotService.cs` (`IsValidGroup` method) and `OpenAiChatProcessingService.cs` (`ValidateGroupStatusAsync` method).

* **Step 6: Database Returns Data**
    The Redis database successfully returns the group's saved details: Target Language, Active Status, and Credit information.
    * *Code Reference:* `RedisGroupAccess.cs` (`GetGroupInfoAsync` and `IsActivatedGroup` methods).

* **Step 7: Ignore Inactive Groups**
    If the group is new, currently inactive, or the text does not contain the trigger characters, the Bot Engine silently ignores the message and stops processing.
    * *Code Reference:* `BotService.cs` (`ProcessTextMessage` method—handles early returns and exits).

* **Step 8: Build AI Prompt**
    If the group is active, the engine constructs a specific "Prompt." It combines the user's text, the target language, and the exact rules required by the AI model.
    * *Code Reference:* `BotService.cs` (`BuildTextProcessingRequestAsync` method) and `OpenAiChatProcessingService.cs` (`BuildChatCompletionRequest` method).

* **Step 9: Send Request to AI**
    The engine sends this formatted prompt over the internet to the OpenAI API.
    * *Code Reference:* `OpenAiChatProcessingService.cs` (`SendChatCompletionRequestAsync` method).

* **Step 10: AI Returns Result**
    The OpenAI API replies with the grammatically corrected or translated text, along with a "Token Usage" receipt detailing how much data was processed.
    * *Code Reference:* `OpenAiChatProcessingService.cs` (`ProcessTextAsync` method maps the response).

* **Step 11: Calculate Costs**
    The engine calculates the exact financial cost of the translation based on the specific AI model used and the returned Token Usage.
    * *Code Reference:* `OpenAiCostCalculator.cs` (`CalculateCost` method) invoked by `OpenAiChatProcessingService.cs` (`UpdateGroupCreditsAsync` method).

* **Step 12: Deduct Credits**
    The application adds this calculated financial cost to the group's `CreditUsed` tally and updates the database.
    * *Code Reference:* `OpenAiChatProcessingService.cs` (`UpdateGroupCreditsAsync` method) calling `RedisGroupAccess.cs` (`UpdateGroupInfoAsync` method).

* **Step 13: Deactivate if Depleted**
    If the group's used credit reaches or exceeds their purchased credit during this transaction, the engine immediately updates the database to switch the group to Inactive (`IsActive = False`).
    * *Code Reference:* `OpenAiChatProcessingService.cs` (`UpdateGroupCreditsAsync` method) calling `RedisGroupAccess.cs` (`SetToggleGroupActive` method).

* **Step 14: Send Warning Message**
    In the event of depleted credits, the bot sends a warning message to the Telegram chat, notifying the users to contact the administrator.
    * *Code Reference:* `OpenAiChatProcessingService.cs` (`UpdateGroupCreditsAsync` method returns the warning string) which is dispatched by `BotService.cs` (`ProcessTextMessage` method).

* **Step 15: Send Processed Message**
    If the group has sufficient credit, the bot sends the final, corrected, and translated text back to the Telegram chat.
    * *Code Reference:* `BotService.cs` (`SendProcessedTextAsync` method).

* **Step 16: Telegram Displays Message**
    The Telegram Platform receives the bot's transmission and displays the final response bubble in the group chat for all members to read.
    * *Code Reference:* Handled externally by the Telegram UI.

