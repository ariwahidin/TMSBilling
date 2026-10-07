using System.Security.Cryptography;
using System.Text;

namespace TMSBilling.Services.Reporting
{
    public interface IReportingTokenService
    {
        string CreateToken(string username);
    }

    public class ReportingTokenService : IReportingTokenService
    {
        private readonly IConfiguration _configuration;

        private const int TokenLifetimeMinutes = 5;

        public ReportingTokenService(
            IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string CreateToken(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                username = "unknown";
            }

            var secret = _configuration["Reporting:Secret"];

            if (string.IsNullOrWhiteSpace(secret))
            {
                throw new InvalidOperationException(
                    "Reporting:Secret is not configured."
                );
            }

            var issuedAt = DateTimeOffset.UtcNow;

            var expiresAt =
                issuedAt.AddMinutes(TokenLifetimeMinutes);

            /*
             * Payload:
             *
             * username
             * issued timestamp
             * expiration timestamp
             * purpose
             */

            var payload =
                $"{username}|{issuedAt.ToUnixTimeSeconds()}|{expiresAt.ToUnixTimeSeconds()}|REPORTING";

            using var hmac =
                new HMACSHA256(
                    Encoding.UTF8.GetBytes(secret)
                );

            var signatureBytes =
                hmac.ComputeHash(
                    Encoding.UTF8.GetBytes(payload)
                );

            var payloadBase64 =
                Base64UrlEncode(
                    Encoding.UTF8.GetBytes(payload)
                );

            var signatureBase64 =
                Base64UrlEncode(signatureBytes);

            return $"{payloadBase64}.{signatureBase64}";
        }

        private static string Base64UrlEncode(byte[] bytes)
        {
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }
    }
}