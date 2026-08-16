using ILToCSConverter.Core.Signing;
using Xunit;

namespace ILToCSConverter.Tests;

public sealed class StrongNameKeyTests
{
    [Fact]
    public void Create_and_load_roundtrip_keeps_token_and_detects_private_key()
    {
        string folder = Path.Combine(Path.GetTempPath(), "iltocs-snk-" + Guid.NewGuid().ToString("N"));
        string path = Path.Combine(folder, "signing.snk");
        try
        {
            var created = StrongNameKey.Create(path);
            var loaded = StrongNameKey.Load(path);

            Assert.True(File.Exists(path));
            Assert.True(created.HasPrivateKey);
            Assert.True(loaded.HasPrivateKey);
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
        string snkOld = Path.Combine(folder, "old.snk");
        string snkNew = Path.Combine(folder, "new.snk");
        string ilPath = Path.Combine(folder, "sample.il");
        try
        {
            var oldKey = StrongNameKey.Create(snkOld);
            var newKey = StrongNameKey.Create(snkNew);
            string oldTokenSpaced = Space(oldKey.Token);
            const string otherToken = "11 22 33 44 55 66 77 88";

            File.WriteAllText(ilPath, $$"""
.assembly Foo
{
  .hash algorithm 0x00008004
  .ver 1:0:0:0
  .publickey = ({{oldKey.FormattedPublicKeyHex}})
}
.assembly extern LibMine
{
  .publickeytoken = ({{oldTokenSpaced}})
}
.assembly extern VendorLib
{
  .publickeytoken = ({{otherToken}})
}
""");

            var result = StrongNameAligner.AlignFile(ilPath, newKey);
            string aligned = File.ReadAllText(ilPath);

            Assert.True(result.HasPublicKey);
            Assert.Equal(oldKey.Token, result.OldToken);
            Assert.Equal(newKey.Token, result.NewToken);
            Assert.Contains(newKey.FormattedPublicKeyHex, aligned);
            Assert.DoesNotContain(oldKey.FormattedPublicKeyHex, aligned);
            Assert.Contains($".publickeytoken = ({newKey.FormattedTokenHex})", aligned);
            Assert.Contains($".publickeytoken = ({otherToken})", aligned);
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
        string snk = Path.Combine(folder, "mine.snk");
        string ilPath = Path.Combine(folder, "unsigned.il");
        try
        {
            var key = StrongNameKey.Create(snk);
            File.WriteAllText(ilPath, """
.assembly Foo
{
  .ver 1:0:0:0
}
""");

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

    [Fact]
    public void StripStrongName_removes_all_publickey_and_token_directives()
    {
        string il = """
.assembly Foo
{
  .publickey = ( 00 24 00 00 )
  .ver 1:0:0:0
}
.assembly extern Lib
{
  .publickeytoken = ( B7 7A 5C 56 19 34 E0 89 )
}
""";
        string stripped = StrongNameAligner.StripStrongName(il);

        Assert.DoesNotContain(".publickey", stripped);
        Assert.DoesNotContain(".publickeytoken", stripped);
        Assert.Contains(".assembly Foo", stripped);
        Assert.Contains(".assembly extern Lib", stripped);
    }

    [Fact]
    public void ReplaceAllExternTokens_forces_all_references_to_new_key()
    {
        string folder = Path.Combine(Path.GetTempPath(), "iltocs-force-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string snkPath = Path.Combine(folder, "test.snk");
        string ilPath = Path.Combine(folder, "force.il");
        try
        {
            var key = StrongNameKey.Create(snkPath);
            File.WriteAllText(ilPath, """
.assembly Main {}
.assembly extern Dep1 { .publickeytoken = ( 11 11 11 11 11 11 11 11 ) }
.assembly extern Dep2 { .publickeytoken = ( 22 22 22 22 22 22 22 22 ) }
""");

            var result = StrongNameAligner.AlignFile(ilPath, key, replaceAllExternTokens: true);
            string content = File.ReadAllText(ilPath);

            Assert.Equal(2, result.UpdatedTokenCount);
            Assert.DoesNotContain("11 11 11 11", content);
            Assert.DoesNotContain("22 22 22 22", content);
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
