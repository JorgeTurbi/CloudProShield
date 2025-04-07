using System.Security.Cryptography;


namespace Commons.Hash;

public static class PasswordHasher
{
    private static readonly int saltSize = 16; // Size of the salt in bytes
    private  static readonly int hashSize = 32; // Size of the hash in bytes
   private static readonly int iterations = 10000; // Number of iterations for the hash function

    public static string HashPassword(string password)
    {
     
        //todo : check if password is null or empty
        if (string.IsNullOrEmpty(password))
        {
            return null;
        //    throw new ArgumentException("Password cannot be null or empty", nameof(password));
        }

        // todo Generate a random salt
            byte[] salt = RandomNumberGenerator.GetBytes(saltSize);
            //todo Generate the hash using PBKDF2
            //todo : use SHA512 as the hash algorithm
            //todo : use 10000 iterations for the hash function
            //todo : use 32 bytes for the hash size
            //todo : use the salt and password to generate the hash
            //todo : use the Rfc2898DeriveBytes class to generate the hash
            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA512);

            //todo : save the salt and hash together in a secure way
            // For example, you can concatenate them and store them in a database
            byte[] hash = pbkdf2.GetBytes(hashSize);
            // todo: Combine the salt and hash into a single byte array
            //todo : use the salt and hash to generate the final hash

            byte[] hashBytes = new byte[saltSize + hashSize];
            Array.Copy(salt, 0, hashBytes, 0, saltSize);
            Array.Copy(hash, 0, hashBytes, saltSize, hashSize);
            //todo : convert the hash to a base64 string
            //todo : return the hash as a base64 string
            //todo : use the Convert.ToBase64String method to convert the hash to a base64 string
            return Convert.ToBase64String(hashBytes);
       
    }

    public static bool VerifyPassword(string hashedPassword, string password)
    {
            byte[] hashBytes = Convert.FromBase64String(hashedPassword);
            //todo : Extract the salt from the hash
            byte[] salt = new byte[saltSize];
            Array.Copy(hashBytes, 0, salt, 0, saltSize);
            //todo : Extract the hash from the hash
            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA512);
            byte[] hash = pbkdf2.GetBytes(hashSize);
            //todo : Compare the hash with the stored hash
            for (int i = 0; i < hashSize; i++)
            {
                if (hashBytes[i + saltSize] != hash[i])
                {
                    return false; // Password does not match
                }
            }

            return true; // Password matches

    }
}