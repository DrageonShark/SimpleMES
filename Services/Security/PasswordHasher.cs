using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SimpleMES.Services.Security
{
    public static class PasswordHasher
    {
        private const int SaltSize = 16; //128位盐值长度
        private const int KeySize = 32; //256位派生密钥长度，最终生成的密码哈希长度
        private const int Iteration = 100_00; //密钥派生算法重复计算哈希的迭代次数
        private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256; //指定密钥派生底层使用的算法

        public static (string Hash, string Salt) HashPassword(string password)
        {
            if (password == null)
            {
                throw new ArgumentNullException(nameof(password));
            }

            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iteration, Algorithm, KeySize);

            return (Convert.ToBase64String(hash),Convert.ToBase64String(salt));
        }

        public static bool VerifyPassword(string password, string hashBase64, string saltBase64)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hashBase64) || string.IsNullOrWhiteSpace(saltBase64))
            {
                return false;
            }

            var salt = Convert.FromBase64String(saltBase64);
            var hashToCompare = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iteration, Algorithm, KeySize);
            var expectedHash = Convert.FromBase64String(hashBase64);
            return CryptographicOperations.FixedTimeEquals(hashToCompare, expectedHash);

        }
    }
}
