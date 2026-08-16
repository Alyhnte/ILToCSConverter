using ILToCSConverter.Core.Updates;

namespace ILToCSConverter.Tests;

public sealed class VersionComparerTests
{
    [Theory]
    [InlineData("1.3.0", "1.2.0", true)]
    [InlineData("v1.2.1", "1.2.0", true)]
    [InlineData("1.2.0", "1.2.0", false)]
    [InlineData("v1.1.9", "1.2.0", false)]
    public void Compares_release_tags_to_installed_version(string remote, string current, bool newer)
    {
        Assert.Equal(newer, VersionComparer.IsNewer(remote, current));
    }
}
