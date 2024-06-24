using Andronix.Core.Globalization;

namespace Andronix.Core.Extensions;

public static class StringExtensions
{
    public static DateTimeOffset ToDateTimeOffset(this string value, TimeProvider timeProvider)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentNullException(nameof(value));

        if (DateTime.TryParse(value, out var result))
            return result;

        value = value.Trim();
        if (value.Equals(Strings.Tomorrow, StringComparison.OrdinalIgnoreCase))
            return timeProvider.GetUtcNow().AddDays(1);

        throw new NotImplementedException();
    }
}
