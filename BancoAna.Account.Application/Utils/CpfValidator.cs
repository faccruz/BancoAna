namespace BancoAna.Account.Application.Utils
{
    public class CpfValidator
    {
        public static bool IsValid(string? cpf)
        {
            if (string.IsNullOrWhiteSpace(cpf)) return false;

            var onlyDigits = new string(cpf.Where(char.IsDigit).ToArray());
            if (onlyDigits.Length != 11) return false;

            // Reject sequences like 00000000000
            var invalids = new string[]
            {
            "00000000000","11111111111","22222222222","33333333333","44444444444",
            "55555555555","66666666666","77777777777","88888888888","99999999999"
            };
            if (invalids.Contains(onlyDigits)) return false;

            int[] numbers = onlyDigits.Select(c => c - '0').ToArray();

            // first digit
            int sum = 0;
            for (int i = 0; i < 9; i++) sum += numbers[i] * (10 - i);
            int remainder = sum % 11;
            int digit = (remainder < 2) ? 0 : 11 - remainder;
            if (numbers[9] != digit) return false;

            // second digit
            sum = 0;
            for (int i = 0; i < 10; i++) sum += numbers[i] * (11 - i);
            remainder = sum % 11;
            digit = (remainder < 2) ? 0 : 11 - remainder;
            if (numbers[10] != digit) return false;

            return true;
        }
    }
}
