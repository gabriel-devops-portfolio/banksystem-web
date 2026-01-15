namespace BankSystem.Common.Database
{
    using System;
    using System.Data.SqlClient;
    using System.Threading.Tasks;
    using Amazon;
    using Amazon.RDS;
    using Amazon.RDS.Util;
    using Amazon.Runtime;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Helper class to generate RDS IAM authentication tokens and build connection strings
    /// with fallback to username/password authentication for break-glass scenarios.
    /// </summary>
    public class RdsIamAuthenticationHelper
    {
        private readonly ILogger<RdsIamAuthenticationHelper> logger;
        private readonly string rdsEndpoint;
        private readonly int rdsPort;
        private readonly string dbUser;
        private readonly string region;
        private readonly bool useIamAuth;
        private readonly string fallbackPassword;

        public RdsIamAuthenticationHelper(
            ILogger<RdsIamAuthenticationHelper> logger,
            string rdsEndpoint,
            int rdsPort,
            string dbUser,
            string region,
            bool useIamAuth,
            string fallbackPassword = null)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.rdsEndpoint = rdsEndpoint ?? throw new ArgumentNullException(nameof(rdsEndpoint));
            this.rdsPort = rdsPort;
            this.dbUser = dbUser ?? throw new ArgumentNullException(nameof(dbUser));
            this.region = region ?? throw new ArgumentNullException(nameof(region));
            this.useIamAuth = useIamAuth;
            this.fallbackPassword = fallbackPassword;
        }

        /// <summary>
        /// Generates an RDS IAM authentication token valid for 15 minutes.
        /// </summary>
        public async Task<string> GenerateAuthTokenAsync()
        {
            try
            {
                // Use IAM role credentials from the pod (IRSA - IAM Roles for Service Accounts)
                var credentials = FallbackCredentialsFactory.GetCredentials();

                this.logger.LogInformation(
                    "Generating RDS IAM authentication token for {DbUser}@{RdsEndpoint}:{Port} in region {Region}",
                    this.dbUser, this.rdsEndpoint, this.rdsPort, this.region);

                var token = await Task.Run(() =>
                    RDSAuthTokenGenerator.GenerateAuthToken(
                        credentials,
                        RegionEndpoint.GetBySystemName(this.region),
                        this.rdsEndpoint,
                        this.rdsPort,
                        this.dbUser));

                this.logger.LogInformation("Successfully generated RDS IAM authentication token");
                return token;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to generate RDS IAM authentication token");
                throw;
            }
        }

        /// <summary>
        /// Builds a connection string using IAM authentication if enabled,
        /// otherwise falls back to username/password.
        /// </summary>
        public async Task<string> BuildConnectionStringAsync(string database, string baseConnectionString)
        {
            if (!this.useIamAuth)
            {
                this.logger.LogInformation("IAM authentication disabled, using username/password authentication");

                if (string.IsNullOrEmpty(this.fallbackPassword))
                {
                    this.logger.LogWarning("No fallback password configured");
                    return baseConnectionString;
                }

                var builder = new SqlConnectionStringBuilder(baseConnectionString)
                {
                    UserID = this.dbUser,
                    Password = this.fallbackPassword
                };

                return builder.ConnectionString;
            }

            try
            {
                var token = await this.GenerateAuthTokenAsync();

                var builder = new SqlConnectionStringBuilder(baseConnectionString)
                {
                    UserID = this.dbUser,
                    Password = token,
                    // Important: Tokens are valid for 15 minutes
                    ConnectTimeout = 30
                };

                this.logger.LogInformation("Built connection string with IAM authentication");
                return builder.ConnectionString;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to build connection string with IAM authentication, attempting fallback");

                // Fallback to username/password
                if (!string.IsNullOrEmpty(this.fallbackPassword))
                {
                    this.logger.LogWarning("Falling back to username/password authentication");

                    var builder = new SqlConnectionStringBuilder(baseConnectionString)
                    {
                        UserID = this.dbUser,
                        Password = this.fallbackPassword
                    };

                    return builder.ConnectionString;
                }

                this.logger.LogError("No fallback password available, cannot establish database connection");
                throw;
            }
        }
    }
}
