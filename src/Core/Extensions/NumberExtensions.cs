namespace Andronix.Core.Extensions;

public static class NumberExtensions
{
    private static readonly Dictionary<string, long> NumberTable = new(StringComparer.InvariantCultureIgnoreCase);

    static NumberExtensions()
    {
        NumberTable.Add("few", 3);

        NumberTable.Add("zero", 0);
        NumberTable.Add("one", 1);
        NumberTable.Add("two", 2);
        NumberTable.Add("three", 3);
        NumberTable.Add("four", 4);
        NumberTable.Add("five", 5);
        NumberTable.Add("six", 6);
        NumberTable.Add("seven", 7);
        NumberTable.Add("eight", 8);
        NumberTable.Add("nine", 9);
        NumberTable.Add("ten", 10);
        NumberTable.Add("eleven", 11);
        NumberTable.Add("twelve", 12);
        NumberTable.Add("thirteen", 13);
        NumberTable.Add("fourteen", 14);
        NumberTable.Add("fifteen", 15);
        NumberTable.Add("sixteen", 16);
        NumberTable.Add("seventeen", 17);
        NumberTable.Add("eighteen", 18);
        NumberTable.Add("nineteen", 19);
        NumberTable.Add("twenty", 20);
        NumberTable.Add("thirty", 30);
        NumberTable.Add("forty", 40);
        NumberTable.Add("fifty", 50);
        NumberTable.Add("sixty", 60);
        NumberTable.Add("seventy", 70);
        NumberTable.Add("eighty", 80);
        NumberTable.Add("ninety", 90);
        NumberTable.Add("hundred", 100);
        NumberTable.Add("thousand", 1000);
        NumberTable.Add("million", 1000000);
        NumberTable.Add("billion", 1000000000);
        NumberTable.Add("trillion", 1000000000000);
        NumberTable.Add("quadrillion", 1000000000000000);
        NumberTable.Add("quintillion", 1000000000000000000);
    }

    public static bool TryParseToLong(this string[] parts, int startIndex, int endIndex, out long total)
    {
        if (parts == null || parts.Length <= 0 || startIndex < 0 || endIndex < startIndex)
        {
            total = 0L;
            return false;
        }

        total = 0L;
        long acc = 0L;
        long partNumber;
        for (int i=startIndex; i<endIndex; i++)
        {
            if (!long.TryParse(parts[i], out partNumber))
            {
                if (!NumberTable.TryGetValue(parts[i], out partNumber))
                    return false;
            }

            if (partNumber >= 1000)
            {
                total += (acc * partNumber);
                acc = 0;
            }
            else if (partNumber >= 100)
            {
                acc *= partNumber;
            }
            else acc += partNumber;
        }

        total += acc;

        return true;
    }
}
