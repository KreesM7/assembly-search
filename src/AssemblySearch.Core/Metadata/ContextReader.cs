using System.Security.Cryptography;
namespace AssemblySearch.Core.Metadata;

public static class ContextReader
{
    public static string Read(SearchCandidate candidate)
    {
        using var stream = File.OpenRead(candidate.Assembly.Path);
        if (Convert.ToHexStringLower(SHA256.HashData(stream)) != candidate.Assembly.Sha256)
            throw new IOException("Assembly changed since search. Reload and search again.");
        return candidate.Context;
    }
}
