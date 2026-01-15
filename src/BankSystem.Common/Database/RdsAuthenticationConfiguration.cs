namespace BankSystem.Common.Database
{
    /// <summary>
    /// Configuration for RDS IAM authentication with fallback support.
    /// </summary>
    public class RdsAuthenticationConfiguration
    {
        /// <summary>
        /// RDS endpoint (e.g., mydb.xxxxxxxxxxxx.us-east-1.rds.amazonaws.com)
        /// </summary>
        public string RdsEndpoint { get; set; }

        /// <summary>
        /// RDS port (typically 1433 for SQL Server)
        /// </summary>
        public int RdsPort { get; set; } = 1433;

        /// <summary>
        /// Database name
        /// </summary>
        public string DatabaseName { get; set; }

        /// <summary>
        /// Database user (for both IAM and fallback authentication)
        /// </summary>
        public string DbUser { get; set; }

        /// <summary>
        /// AWS region (e.g., us-east-1)
        /// </summary>
        public string AwsRegion { get; set; }

        /// <summary>
        /// Enable IAM authentication. If false, uses fallback password.
        /// </summary>
        public bool UseIamAuthentication { get; set; } = true;

        /// <summary>
        /// Fallback password for break-glass scenarios when IAM authentication fails.
        /// Should be stored in Kubernetes secrets.
        /// </summary>
        public string FallbackPassword { get; set; }

        /// <summary>
        /// Additional connection string parameters (e.g., MultipleActiveResultSets=true;Encrypt=true)
        /// </summary>
        public string AdditionalParameters { get; set; }

        /// <summary>
        /// Legacy connection string (used if RDS IAM is not configured)
        /// </summary>
        public string LegacyConnectionString { get; set; }
    }
}
