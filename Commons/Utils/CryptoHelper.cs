using System.Security.Cryptography;

namespace CloudShield.Commons.Utils
{
    public static class CryptoHelper
    {
        public static string RandomOtp(int digits = 6)
        {
            var max = (int)Math.Pow(10, digits) - 1;
            return RandomNumberGenerator.GetInt32(0, max).ToString($"D{digits}");
        }

        private const string _chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789@#";

        public static string Generate(int length = 12)
        {
            var bytes = RandomNumberGenerator.GetBytes(length);
            var chars = new char[length];
            for (int i = 0; i < length; i++)
                chars[i] = _chars[bytes[i] % _chars.Length];
            return new string(chars);
        }
    }
}
