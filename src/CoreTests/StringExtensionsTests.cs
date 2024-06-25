using Andronix.Core.Extensions;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;

namespace CoreTests;

[TestClass]
public class StringExtensionsTests
{
    [TestMethod]
    [DataRow("2021-01-01T00:00:00Z", "2021-01-01T00:00:00Z")]
    [DataRow("tomorrow", "2021-01-02T00:00:00Z")]
    [DataRow("next week", "2021-01-03T00:00:00Z")]
    public void ToDateTime(string testValue, string expectedDateTime)
    {
        var fakeTimeProvider = new FakeTimeProvider(new DateTimeOffset(2021, 1, 1, 0, 0, 0, TimeSpan.Zero));

        var expected = DateTimeOffset.Parse(expectedDateTime);

        var actual = testValue.ToDateTimeOffset(fakeTimeProvider);

        actual.Should().Be(expected);
    }
}