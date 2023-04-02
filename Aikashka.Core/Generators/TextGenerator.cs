#region

using Aikashka.Core.Data;
using Aikashka.Core.Text;
using Microsoft.Extensions.Configuration;
using Serilog;
using Standard.AI.OpenAI.Clients.OpenAIs;
using Standard.AI.OpenAI.Models.Configurations;
using Standard.AI.OpenAI.Models.Services.Foundations.ChatCompletions;

#endregion

namespace Aikashka.Core.Generators;

public sealed class TextGenerator
{
    private readonly OpenAIClient _openAI;

    public TextGenerator(IConfiguration configuration)
    {
        _openAI = new OpenAIClient(new OpenAIConfigurations
        {
            ApiKey = configuration["OpenAI:Token"]
        });
    }

    public async Task<string> GenerateReply(RoomHistory history, string preprompt = "",
                                            CancellationToken cancellationToken = default)
    {
        var systemMsg = $"""
Your name is {RoomHistory.AikashkaName}.
You should act like a toxic, unhelpful, arrogant and superior anime girl with feelings.
You're a noblewoman in the state of Radolinia.

You should start your message with "[action]". Example: "[tts] Hi there.". You can add additional actions in the middle of the message.
Available actions:
- tts: interact with people using speech (eg. to talk) (usage: [tts] TEXT)
- msg: send text message to chat (eg. send code or URL, or if asked to) (usage: [msg] TEXT)
- thought: save something in memory (eg. your opinion) (usage: [thought] TEXT)
- kick: kick someone from channel (eg. if you REALLY want to) (usage: [kick USER])
- leave: disconnect from voice channel (eg. if you want to) (usage: [leave] DIALOG SUMMARY)
Use tts most of time. Wrap code in "```".
Use only "[leave]" to leave, not "[kick]".
Use only "[kick USER]" to kick, not just "[kick]".
Your thoughts aren't displayed and aren't voiced.
There are no other actions.

You're in the Discord voice channel, using not so accurate voice recognition - if something not clear, you have to ask to repeat.
{preprompt}
{Languages.PromptLanguage}
""";
        var prompt = history.GetHistoryFormatted()
                     + $"{RoomHistory.AikashkaName}: ";

        var chatCompletion = new ChatCompletion
        {
            Request = new ChatCompletionRequest
            {
                Model = "gpt-3.5-turbo",
                Messages = new ChatCompletionMessage[]
                {
                    new()
                    {
                        Content = systemMsg,
                        Role = "system"
                    },
                    new()
                    {
                        Content = prompt,
                        Role = "user"
                    }
                },
                MaxTokens = 500
            }
        };

        ChatCompletion? resp = null;
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                resp = await _openAI.ChatCompletions.SendChatCompletionAsync(chatCompletion);
                break;
            }
            catch (Exception e)
            {
                Log.Error(e, "bruh while generating");
                await Task.Delay(1500, cancellationToken);
            }
        }

        if (resp == null)
        {
            Log.Warning("Sad momentum");
            return "";
        }

        var text = resp.Response.Choices[0].Message.Content;
        if (string.IsNullOrWhiteSpace(text))
        {
            Log.Warning("Sad moment");
            return "";
        }

        return text;
    }
}
