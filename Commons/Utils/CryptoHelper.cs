using System.Security.Cryptography;

namespace CloudShield.Commons.Utils
{
  public static class CryptoHelper
  {

    public static string RandomOtp(int digits =6)
    {
      var max = (int)Math.Pow(10, digits) - 1;
      return RandomNumberGenerator.GetInt32(0, max).ToString($"D{digits}");
    }
  }
}