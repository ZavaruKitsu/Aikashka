#region

using System.Collections.Concurrent;

#endregion

namespace Aikashka.Core;

public sealed class RuntimeStorage
{
    public ConcurrentDictionary<ulong, RoomContext> Rooms { get; set; } = new();
}
