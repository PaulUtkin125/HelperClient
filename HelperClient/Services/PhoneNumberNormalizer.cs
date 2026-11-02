using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace HelperClient.Services
{
    public enum PhoneValidationResult
    {
        Empty,
        TooShort,
        TooLong,
        InvalidFormat,
        Ok,
    }

    /// <summary>
    /// Валидация и нормализация мобильного контактного номера телефона для заявки.
    /// На сервер уходит каноничный формат +7XXXXXXXXXX (лимит поля — 255 символов).
    /// </summary>
    public static class PhoneNumberNormalizer
    {
        private static readonly Regex AllowedChars = new(@"^[0-9+()\-\s]+$", RegexOptions.Compiled);

        /// <summary>Привести ввод пользователя к формату +7 912 345-67-89 либо вернуть null.</summary>
        public static string? Normalize(string? input)
        {
            var validation = Validate(input);
            if (validation != PhoneValidationResult.Ok) return null;

            var digits = new string(input!.Where(char.IsDigit).ToArray());

            // 8 (800...) или 7 (7900...) -> приводим к 7.
            if (digits.Length == 11 && (digits[0] == '8' || digits[0] == '7'))
                digits = "7" + digits.Substring(1);

            if (digits.Length == 10)
                digits = "7" + digits;

            if (digits.Length != 11 || digits[0] != '7') return null;

            return $"+{digits[0]} {digits.Substring(1, 3)} {digits.Substring(4, 3)}-{digits.Substring(7, 2)}-{digits.Substring(9, 2)}";
        }

        public static PhoneValidationResult Validate(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return PhoneValidationResult.Empty;
            if (input.Trim().Length > 255) return PhoneValidationResult.TooLong;
            if (!AllowedChars.IsMatch(input.Trim())) return PhoneValidationResult.InvalidFormat;

            var digits = new string(input.Where(char.IsDigit).ToArray());
            if (digits.Length is < 10 or > 11) return PhoneValidationResult.TooShort;
            if (digits.Length == 11 && digits[0] is not ('7' or '8')) return PhoneValidationResult.InvalidFormat;

            return PhoneValidationResult.Ok;
        }

        public static string Describe(PhoneValidationResult result) => result switch
        {
            PhoneValidationResult.Empty => "Укажите контактный номер телефона",
            PhoneValidationResult.TooShort => "Номер должен содержать 10–11 цифр",
            PhoneValidationResult.TooLong => "Номер не должен превышать 255 символов",
            PhoneValidationResult.InvalidFormat => "Допустимы только цифры, «+», скобки, дефис и пробелы",
            _ => "Номер корректен",
        };
    }
}
