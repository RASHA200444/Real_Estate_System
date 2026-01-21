using System.Security.Cryptography;
using System.Text;

namespace otherServices.Services
{
    public interface IEncryptionService
    {
        string Encrypt(string plainText);
        string Decrypt(string cipherText);
    }

    public class EncryptionService : IEncryptionService
    {
        // AES256 (32-byte key)
        private readonly byte[] _key = Convert.FromBase64String("kCts6hzl8WnA2c+HR5knUEgsJX8W1+O8pVZQnQYhZy8=");
        private readonly byte[] _iv = Convert.FromBase64String("w7ZkqXJg0KBnmbX5Iu5qjg==");

        public string Encrypt(string plainText)
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;

            var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            var bytes = Encoding.UTF8.GetBytes(plainText);
            var encrypted = encryptor.TransformFinalBlock(bytes, 0, bytes.Length);

            return Convert.ToBase64String(encrypted);
        }

        public string Decrypt(string cipherText)
        {
            var buffer = Convert.FromBase64String(cipherText);

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;

            var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            var decrypted = decryptor.TransformFinalBlock(buffer, 0, buffer.Length);

            return Encoding.UTF8.GetString(decrypted);
        }

        

    }
}
