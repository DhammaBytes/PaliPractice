namespace PaliPractice.Services.Practice;

/// <summary>
/// A replayable cadence indexed by completed answers, independent of queue
/// builds and calendar dates. Every 30 positions contain five new slots;
/// consecutive new slots are five to seven positions apart.
/// </summary>
internal static class NewFormSchedule
{
    const int BlockLength = 30;
    const int NewPerBlock = 5;

    public static (bool[] Slots, long ReviewsBefore) Build(long completed, int count, PracticeType type, DateTime seedDate,
        DateTime? firstPracticeUtc = null)
    {
        var slots = new bool[count];
        var block = completed / BlockLength;
        var offset = (int)(completed % BlockLength);
        var days = (int)((firstPracticeUtc ?? seedDate).Date - DateTime.UnixEpoch).TotalDays;
        var positions = Positions(block, type, days);
        var priorNew = block * NewPerBlock + positions.Count(p => p < offset);
        for (int i = 0; i < count; i++)
        {
            if (offset == BlockLength)
            {
                positions = Positions(++block, type, days);
                offset = 0;
            }
            slots[i] = Array.IndexOf(positions, offset++) >= 0;
        }
        return (slots, completed - priorNew);
    }

    static int[] Positions(long block, PracticeType type, int days)
    {
        // A balanced shuffled block keeps the mean admission rate unchanged
        // without requiring a saved RNG state or replaying all past answers.
        int[] gaps = [5, 6, 6, 6, 7];
        var seed = unchecked(((int)(block ^ (block >> 32)) * 397 ^ days) * 397 ^ (int)type);
        new Random(seed).Shuffle(gaps);
        var position = -1;
        for (int i = 0; i < gaps.Length; i++)
        {
            position += gaps[i];
            gaps[i] = position;
        }
        return gaps;
    }
}
