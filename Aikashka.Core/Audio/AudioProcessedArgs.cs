#region

using Discord.WebSocket;

#endregion

namespace Aikashka.Core.Audio;

public sealed class AudioProcessedArgs : EventArgs
{
    public AudioProcessedArgs(SocketGuildUser member, string text)
    {
        Member = member;
        Text = text;
    }

    public SocketGuildUser Member { get; }
    public string Text { get; }
}
