using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

namespace AirlogGenerator.Services
{
    public static class SecretHelper
    {
        public static async Task<string> GetSecretAsync(string secretId, string? region = null)
        {
            if (string.IsNullOrWhiteSpace(secretId))
                throw new ArgumentException("SecretId must be provided.", nameof(secretId));

            using var client = string.IsNullOrWhiteSpace(region)
                ? new AmazonSecretsManagerClient()
                : new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(region));

            var response = await client.GetSecretValueAsync(new GetSecretValueRequest
            {
                SecretId = secretId
            });

            return response.SecretString;
        }
    }
}