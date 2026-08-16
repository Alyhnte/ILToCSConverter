using ILToCSConverter.Core.Signing;

namespace ILToCSConverter.Tests;

public sealed class TokenMapParserTests
{
    [Fact]
    public void Parses_equals_and_colon_entries_case_insensitively()
    {
        var map = TokenMapParser.Parse("B77A5C561934E089=aabbccddeeff0011\n1122334455667788:99aa bb cc dd ee ff 00");

        Assert.NotNull(map);
        Assert.True(map.TryGetValue("b77a5c561934e089", out string? first));
        Assert.Equal("aabbccddeeff0011", first);
        Assert.True(map.TryGetValue("1122334455667788", out string? second));
        Assert.Equal("99aabbccddeeff00", second);
    }

    [Fact]
    public void Splits_commas_and_skips_comments()
    {
        var map = TokenMapParser.Parse("""
# yorum
1111111111111111=2222222222222222, 3333333333333333=4444444444444444
""");

        Assert.NotNull(map);
        Assert.Equal(2, map.Count);
        Assert.Equal("2222222222222222", map["1111111111111111"]);
        Assert.Equal("4444444444444444", map["3333333333333333"]);
    }

    [Fact]
    public void Empty_or_whitespace_returns_null()
    {
        Assert.Null(TokenMapParser.Parse((string?)null));
        Assert.Null(TokenMapParser.Parse("   "));
        Assert.Null(TokenMapParser.Parse("# sadece yorum"));
    }

    [Fact]
    public void Invalid_entry_throws()
    {
        Assert.Throws<FormatException>(() => TokenMapParser.Parse("not-a-map"));
        Assert.Throws<FormatException>(() => TokenMapParser.Parse("abc=12"));
    }
}

public sealed class StrongNameTokenMapAlignerTests
{
    [Fact]
    public void Align_applies_token_map_before_default_matching()
    {
        string folder = Path.Combine(Path.GetTempPath(), "iltocs-map-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var key = StrongNameKey.Create(Path.Combine(folder, "mine.snk"));
            string mapped = "aabbccddeeff0011";
            var tokenMap = TokenMapParser.Parse($"1122334455667788={mapped}");
            string il = """
.assembly Main
{
  .ver 1:0:0:0
}
.assembly extern Vendor
{
  .publickeytoken = ( 11 22 33 44 55 66 77 88 )
}
""";

            var result = StrongNameAligner.Align(il, key, tokenMap);

            Assert.Equal(1, result.UpdatedTokenCount);
            Assert.Contains($".publickeytoken = ({StrongNameKey.FormatHex(Convert.FromHexString(mapped), 8)})", result.UpdatedIl);
            Assert.DoesNotContain("11 22 33 44 55 66 77 88", result.UpdatedIl);
        }
        finally
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);
        }
    }
}
