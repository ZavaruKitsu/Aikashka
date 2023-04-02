#region

using Discord.WebSocket;

#endregion

namespace Aikashka.Core.Utilities;

public static class NameService
{
    private static readonly Dictionary<ulong, string> IdToNames;
    private static readonly Dictionary<string, ulong> NameToIds;

    static NameService()
    {
        IdToNames = File
                    .ReadAllText("./Data/YourOwn/name_mappings.txt")
                    .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Split(':', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                    .ToDictionary(x => ulong.Parse(x[0]), x => x[1]);

        NameToIds = IdToNames.ToDictionary(x => x.Value, x => x.Key);
    }

    public static string GetMemberName(SocketGuildUser member)
    {
        return GetMemberName(member.Id) ?? member.Username;
    }

    public static string? GetMemberName(ulong id)
    {
        var exists = IdToNames.TryGetValue(id, out var name);
        if (exists)
        {
            return name!;
        }

        return null;
    }

    public static ulong? GetMemberId(string name)
    {
        _ = NameToIds.TryGetValue(name, out var id);
        return id;
    }
}
