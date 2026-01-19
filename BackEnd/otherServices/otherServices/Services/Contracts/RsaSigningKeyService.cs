using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using otherServices.Repositories;

namespace otherServices.Services.Contracts
{
    public class RsaSigningKeyService : ISigningKeyService
    {
        private readonly IUnitOfWork _uow;
        private readonly IEncryptionService _enc;

        // 2048 كفاية لمشروع GP + سريع
        private const int RsaKeySize = 2048;

        public RsaSigningKeyService(IUnitOfWork uow, IEncryptionService enc)
        {
            _uow = uow;
            _enc = enc;
        }

        public async Task EnsureUserKeysAsync(long userId)
        {
            var user = await _uow.Users.GetByIdAsync(userId);
            if (user == null) throw new Exception("User not found");

            // لو موجودين خلاص
            if (!string.IsNullOrWhiteSpace(user.PublicSignKey) &&
                !string.IsNullOrWhiteSpace(user.PrivateSignKeyEncrypted))
                return;

            // Generate RSA keypair
            using var rsa = RSA.Create(RsaKeySize);

            // Export keys as bytes, then Base64
            var publicKeyBytes = rsa.ExportSubjectPublicKeyInfo();          // public
            var privateKeyBytes = rsa.ExportPkcs8PrivateKey();              // private

            var publicKeyB64 = Convert.ToBase64String(publicKeyBytes);
            var privateKeyB64 = Convert.ToBase64String(privateKeyBytes);

            user.PublicSignKey = publicKeyB64;
            user.PrivateSignKeyEncrypted = _enc.Encrypt(privateKeyB64);

            _uow.Users.Update(user);
            await _uow.CompleteAsync();
        }

        public async Task<string> SignAsync(long userId, string message)
        {
            await EnsureUserKeysAsync(userId);

            var user = await _uow.Users.GetByIdAsync(userId);
            if (user == null) throw new Exception("User not found");

            if (string.IsNullOrWhiteSpace(user.PrivateSignKeyEncrypted))
                throw new Exception("Private signing key is missing");

            var privateKeyB64 = _enc.Decrypt(user.PrivateSignKeyEncrypted);
            var privateKeyBytes = Convert.FromBase64String(privateKeyB64);

            using var rsa = RSA.Create();
            rsa.ImportPkcs8PrivateKey(privateKeyBytes, out _);

            var data = Encoding.UTF8.GetBytes(message);

            var sig = rsa.SignData(
                data,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pss);

            return Convert.ToBase64String(sig);
        }

        public async Task<bool> VerifyAsync(long userId, string message, string signatureBase64)
        {
            var user = await _uow.Users.GetByIdAsync(userId);
            if (user == null) throw new Exception("User not found");

            if (string.IsNullOrWhiteSpace(user.PublicSignKey))
                return false;

            var publicKeyBytes = Convert.FromBase64String(user.PublicSignKey);

            using var rsa = RSA.Create();
            rsa.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);

            var data = Encoding.UTF8.GetBytes(message);
            var sig = Convert.FromBase64String(signatureBase64);

            return rsa.VerifyData(
                data,
                sig,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pss);
        }
    }
}
