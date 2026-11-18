using System.Text.RegularExpressions;

namespace ASPHotelM2631.Application.Authentication;

public static class IdentifierRules
{
    private static readonly Regex Email = new(@"^[^\s@]+@[^\s@]+\.[^\s@]+$");
    private static readonly Regex Phone = new(@"^(0|\+84)\d{9}$");

    public static string Normalize(string? raw)
    {
        var s = (raw ?? "").Trim();
        return s.Contains('@') ? s.ToLowerInvariant() : Regex.Replace(s, @"[\s.\-]", "");
    }

    public static bool IsValid(string id) => Email.IsMatch(id) || Phone.IsMatch(id);

    public static bool IsStrongPassword(string? pw) =>
        pw is { Length: >= 8 } && pw.Any(char.IsLetter) && pw.Any(char.IsDigit);
}