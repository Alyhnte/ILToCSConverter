using ILToCSConverter.Core.Signing;

namespace ILToCSConverter.Tests;

public sealed class StrongNameKeyTests
{
    [Fact]
    public void Create_and_load_roundtrip_keeps_token()
    {
        string folder = Path.Combine(Path.GetTempPath(), "iltocs-snk-" + Guid.NewGuid().ToString("N"));
        string path = Path.Combine(folder, "signing.snk");
        try
        {
            var created = StrongNameKey.Create(path);
            var loaded = StrongNameKey.Load(path);

            Assert.True(File.Exists(path));
            Assert.Equal(16, created.Token.Length);
            Assert.Equal(created.Token, loaded.Token);
            Assert.Equal(created.PublicBlob, loaded.PublicBlob);
        }
        finally
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void Token_is_last_8_bytes_of_sha1_reversed()
    {
        byte[] blob = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();
        string token = StrongNameKey.ComputeToken(blob);
        byte[] hash = System.Security.Cryptography.SHA1.HashData(blob);
        var expected = new byte[8];
        for (int i = 0; i < 8; i++)
            expected[i] = hash[hash.Length - 1 - i];
        Assert.Equal(Convert.ToHexString(expected).ToLowerInvariant(), token);
    }
}

public sealed class StrongNameAlignerTests
{
    [Fact]
    public void Aligns_publickey_and_matching_token_only()
    {
        string folder = Path.Combine(Path.GetTempPath(), "iltocs-align-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var oldKey = StrongNameKey.Create(Path.Combine(folder, "old.snk"));
            var newKey = StrongNameKey.Create(Path.Combine(folder, "new.snk"));
            string ilPath = Path.Combine(folder, "sample.il");
            const string otherToken = "11 22 33 44 55 66 77 88";
            string oldTokenSpaced = Space(oldKey.Token);

            File.WriteAllText(ilPath,
                ".assembly Foo" + Environment.NewLine +
                "{" + Environment.NewLine +
                "  .hash algorithm 0x00008004" + Environment.NewLine +
                "  .ver 1:0:0:0" + Environment.NewLine +
                "  .publickey = (" + oldKey.FormattedPublicKeyHex + ")" + Environment.NewLine +
                "}" + Environment.NewLine +
                ".assembly extern LibMine" + Environment.NewLine +
                "{" + Environment.NewLine +
                "  .publickeytoken = (" + oldTokenSpaced + ")" + Environment.NewLine +
                "}" + Environment.NewLine +
                ".assembly extern VendorLib" + Environment.NewLine +
                "{" + Environment.NewLine +
                "  .publickeytoken = (" + otherToken + ")" + Environment.NewLine +
                "}" + Environment.NewLine);

            var result = StrongNameAligner.AlignFile(ilPath, newKey);
            string aligned = File.ReadAllText(ilPath);

            Assert.True(result.HasPublicKey);
            Assert.Equal(oldKey.Token, result.OldToken);
            Assert.Equal(newKey.Token, result.NewToken);
            Assert.Contains(newKey.FormattedPublicKeyHex, aligned);
            Assert.DoesNotContain(oldKey.FormattedPublicKeyHex, aligned);
            Assert.Contains(".publickeytoken = (" + newKey.FormattedTokenHex + ")", aligned);
            Assert.Contains(".publickeytoken = (" + otherToken + ")", aligned);
            Assert.Equal(1, result.UpdatedTokenCount);
        }
        finally
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void Inserts_publickey_when_assembly_has_none()
    {
        string folder = Path.Combine(Path.GetTempPath(), "iltocs-insert-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var key = StrongNameKey.Create(Path.Combine(folder, "mine.snk"));
            string ilPath = Path.Combine(folder, "unsigned.il");
            File.WriteAllText(ilPath,
                ".assembly Foo" + Environment.NewLine +
                "{" + Environment.NewLine +
                "  .ver 1:0:0:0" + Environment.NewLine +
                "}" + Environment.NewLine);

            var result = StrongNameAligner.AlignFile(ilPath, key);
            string aligned = File.ReadAllText(ilPath);

            Assert.True(result.HasPublicKey);
            Assert.Null(result.OldToken);
            Assert.Contains(".publickey = (", aligned);
            Assert.Contains(key.FormattedPublicKeyHex, aligned);
        }
        finally
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);
        }
    }

    private static string Space(string hex)
    {
        string clean = StrongNameKey.NormalizeHex(hex);
        var parts = new List<string>();
        for (int i = 0; i < clean.Length; i += 2)
            parts.Add(clean.Substring(i, 2).ToUpperInvariant());
        return string.Join(" ", parts);
    }
}
