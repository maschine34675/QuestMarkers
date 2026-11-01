using System;

namespace QuestMarkers
{
    [Flags]
    internal enum MarkerSet
    {
        None = 0,
        Quests = 1,
        Extracts = 2,
        Airdrops = 4,
        BTR = 8,
    }
}
