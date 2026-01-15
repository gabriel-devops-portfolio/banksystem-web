namespace BankSystem.Web.Infrastructure.Extensions
{
    using System;
    using System.Threading.Tasks;
    using BankSystem.Common.Database;
    using Data;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    public static class DatabaseServiceExtensions
    {
        /// <summary>
        /// Adds BankSystemDbContext with RDS IAM authentication support and fallback to username/password.
        /// </summary>
        public static IServiceCollection AddBankSystemDatabase(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var rdsConfig = configuration.GetSection("RdsAuthentication").Get<RdsAuthenticationConfiguration>();

            // If RDS IAM configuration is not provided, use legacy connection string
            if (rdsConfig == null || string.IsNullOrEmpty(rdsConfig.RdsEndpoint))
            {
                var legacyConnectionString = configuration.GetConnectionString("DefaultConnection");
                services.AddDbContextPool<BankSystemDbContext>(options =>
                    options.UseSqlServer(legacyConnectionString));

                return services;
            }

            // Register RDS IAM helper
            services.AddSingleton<RdsIamAuthenticationHelper>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<RdsIamAuthenticationHelper>>();
                return new RdsIamAuthenticationHelper(
                    logger,
                    rdsConfig.RdsEndpoint,
                    rdsConfig.RdsPort,
                    rdsConfig.DbUser,
                    rdsConfig.AwsRegion,
                    rdsConfig.UseIamAuthentication,
                    rdsConfig.FallbackPassword);
            });

            // Add DbContext with dynamic connection string
            services.AddDbContextPool<BankSystemDbContext>((sp, options) =>
            {
                var helper = sp.GetRequiredService<RdsIamAuthenticationHelper>();
                var logger = sp.GetRequiredService<ILogger<BankSystemDbContext>>();

                try
                {
                    // Build base connection string
                    var baseConnectionString = $"Server={rdsConfig.RdsEndpoint},{rdsConfig.RdsPort};" +
                                              $"Database={rdsConfig.DatabaseName};" +
                                              $"{rdsConfig.AdditionalParameters ?? "MultipleActiveResultSets=true;Encrypt=true;TrustServerCertificate=false"}";

                    // Get connection string with IAM auth or fallback
                    var connectionString = helper.BuildConnectionStringAsync(
                        rdsConfig.DatabaseName,
                        baseConnectionString).GetAwaiter().GetResult();

                    options.UseSqlServer(connectionString);

                    logger.LogInformation("BankSystemDbContext configured successfully");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to configure BankSystemDbContext with RDS IAM authentication");

                    // Last resort: try legacy connection string
                    if (!string.IsNullOrEmpty(rdsConfig.LegacyConnectionString))
                    {
                        logger.LogWarning("Using legacy connection string as last resort");
                        options.UseSqlServer(rdsConfig.LegacyConnectionString);
                    }
                    else
                    {
                        throw;
                    }
                }
            });

            return services;
        }
    }
}
