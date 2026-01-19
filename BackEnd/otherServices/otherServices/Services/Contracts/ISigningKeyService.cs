using System.Threading.Tasks;

namespace otherServices.Services.Contracts
{
    public interface ISigningKeyService
    {
        /// <summary>
        /// Ensure user has RSA keys stored. If missing -> generate & persist.
        /// </summary>
        Task EnsureUserKeysAsync(long userId);

        /// <summary>
        /// Sign a message hash/string using user's private key (server-stored, encrypted).
        /// Returns Base64 signature.
        /// </summary>
        Task<string> SignAsync(long userId, string message);

        /// <summary>
        /// Verify signature using user's public key. Signature is Base64.
        /// </summary>
        Task<bool> VerifyAsync(long userId, string message, string signatureBase64);
    }
}
