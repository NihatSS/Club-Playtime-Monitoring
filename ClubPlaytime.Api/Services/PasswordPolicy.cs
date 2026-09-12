namespace ClubPlaytime.Api.Services;

/// <summary>
/// Central password policy for NEW passwords (registration, password changes,
/// admin-created users). Existing accounts are never forced to migrate — the
/// policy only runs when a password is being set.
/// </summary>
public static class PasswordPolicy
{
    public const int MinimumLength = 8;

    public const string RequirementsText =
        "Password must be at least 8 characters and include an uppercase letter, a lowercase letter, a number, and a symbol.";

    /// <summary>
    /// Validates a new password against the policy. Returns all failed rules so
    /// the UI can show a complete checklist instead of one error at a time.
    /// </summary>
    public static bool IsValid(string? password, out IReadOnlyList<string> errors)
    {
        var failures = new List<string>();

        if (string.IsNullOrEmpty(password) || password.Length < MinimumLength)
        {
            failures.Add($"at least {MinimumLength} characters");
        }

        if (string.IsNullOrEmpty(password) || !password.Any(char.IsUpper))
        {
            failures.Add("an uppercase letter");
        }

        if (string.IsNullOrEmpty(password) || !password.Any(char.IsLower))
        {
            failures.Add("a lowercase letter");
        }

        if (string.IsNullOrEmpty(password) || !password.Any(char.IsDigit))
        {
            failures.Add("a number");
        }

        if (string.IsNullOrEmpty(password) || password.All(char.IsLetterOrDigit))
        {
            failures.Add("a symbol");
        }

        errors = failures;
        return failures.Count == 0;
    }

    /// <summary>User-facing message listing every unmet requirement.</summary>
    public static string BuildErrorMessage(IReadOnlyList<string> errors)
    {
        return "Password must contain " + string.Join(", ", errors) + ".";
    }
}
