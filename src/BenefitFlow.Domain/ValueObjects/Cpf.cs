using BenefitFlow.Domain.Common;

namespace BenefitFlow.Domain.ValueObjects;

public sealed record Cpf
{
    private Cpf()
    {
        Value = string.Empty;
    }

    private Cpf(string value)
    {
        Value = value;
    }

    public string Value { get; private set; }

    public static Result<Cpf> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<Cpf>(DomainErrors.InvalidCpf);
        }

        var trimmed = value.Trim();
        var isDigitsOnly = trimmed.Length == 11 &&
            trimmed.All(character => character is >= '0' and <= '9');
        var isFormatted = trimmed.Length == 14 &&
            trimmed[3] == '.' &&
            trimmed[7] == '.' &&
            trimmed[11] == '-' &&
            trimmed.Where((_, index) => index is not (3 or 7 or 11))
                .All(character => character is >= '0' and <= '9');

        if (!isDigitsOnly && !isFormatted)
        {
            return Result.Failure<Cpf>(DomainErrors.InvalidCpf);
        }

        var normalized = isDigitsOnly
            ? trimmed
            : new string(trimmed.Where(character => character is >= '0' and <= '9').ToArray());

        if (normalized.All(character => character == normalized[0]) ||
            !HasValidCheckDigits(normalized))
        {
            return Result.Failure<Cpf>(DomainErrors.InvalidCpf);
        }

        return Result.Success(new Cpf(normalized));
    }

    private static bool HasValidCheckDigits(string value)
    {
        var firstDigit = CalculateCheckDigit(value.AsSpan(0, 9), 10);
        var secondDigit = CalculateCheckDigit(value.AsSpan(0, 10), 11);
        return value[9] - '0' == firstDigit && value[10] - '0' == secondDigit;
    }

    private static int CalculateCheckDigit(ReadOnlySpan<char> digits, int initialWeight)
    {
        var sum = 0;
        for (var index = 0; index < digits.Length; index++)
        {
            sum += (digits[index] - '0') * (initialWeight - index);
        }

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }
}
