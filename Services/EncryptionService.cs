namespace TMSBilling.Services
{
    using System.Security.Cryptography;
    using System.Text;

    public class EncryptionService : IEncryptionService
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;

        public EncryptionService(IConfiguration configuration)
        {
            var keyString = configuration["EncryptionKey"]
                ?? throw new InvalidOperationException("EncryptionKey tidak ditemukan di appsettings.json");

            // Pastikan key tepat 32 bytes (AES-256)
            _key = Encoding.UTF8.GetBytes(keyString.PadRight(32).Substring(0, 32));

            // IV fixed 16 bytes dari key
            _iv = Encoding.UTF8.GetBytes(keyString.PadRight(16).Substring(0, 16));
        }

        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return string.Empty;

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;

            using var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            return Convert.ToBase64String(encryptedBytes);
        }

        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return string.Empty;

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;

            using var decryptor = aes.CreateDecryptor();
            var cipherBytes = Convert.FromBase64String(cipherText);
            var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

            return Encoding.UTF8.GetString(plainBytes);
        }
    }
}
