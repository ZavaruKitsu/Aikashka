#region

using Aikashka.Core.Data;
using Aikashka.Core.Utilities;
using Discord.WebSocket;

#endregion

namespace Aikashka.Core.Text;

public sealed class RoomHistory
{
    public static readonly string AikashkaName = Languages.AikashkaName;

    public static readonly List<string> AikashkaNames =
        Languages.AikashkaNames
                 .Split(";", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                 .ToList();

    private readonly List<(string member, string message)> _history = new();

    // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
    public string LastMessageFrom => _history.LastOrDefault().member ?? "";

    // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
    public string LastMessage => _history.LastOrDefault().message ?? "";

    public void AddMessage(SocketGuildUser member, string message)
    {
        var name = NameService.GetMemberName(member);
        _history.Add((name, message));
    }

    public void AddAikashkaMessage(string message)
    {
        _history.Add((AikashkaName, message));
    }

    public string GetHistoryFormatted()
    {
        var messages = new List<string>();

        var usersCount = 0;
        var aikashkaCount = 0;
        foreach (var (member, message) in _history.AsEnumerable().Reverse())
        {
            if (usersCount > 20 && aikashkaCount > 6)
            {
                break;
            }

            if (member == AikashkaName && aikashkaCount > 6)
            {
                continue;
            }

            if (member != AikashkaName && usersCount > 20)
            {
                continue;
            }

            if (member == AikashkaName)
            {
                ++aikashkaCount;
            }
            else
            {
                ++usersCount;
            }

            messages.Add($"{member}: {message}\n");
        }

        return messages.Aggregate("", (next, curr) => curr + next);
    }

    public void Clear(SocketGuildUser member)
    {
        _history.Clear();

        AddMessage(member, Languages.PromptHistoryCleared);
    }
}
