namespace Eskineria.Core.Logging.Configuration;

public sealed class EskineriaLoggingOptions
{
    public string? MinimumLevel { get; set; }
    public Dictionary<string, string> MinimumLevelOverrides { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public KafkaLoggingOptions Kafka { get; set; } = new();

    public string CorrelationIdHeaderName { get; set; } = "X-Correlation-Id";
    public bool EnableRequestResponseBodyLogging { get; set; } = true;
    public int MaxBodyLogSizeBytes { get; set; } = 100 * 1024;
    public int MaxHeaderValueLogLength { get; set; } = 100;
    public int MaxQueryValueLogLength { get; set; } = 200;
    public int MaxQueryStringLogLength { get; set; } = 2000;

    public string[] ExcludedPathPrefixes { get; set; } = new[]
    {
        "/scalar",
        "/openapi"
    };

    public string[] MaskedFields { get; set; } = new[]
    {
        "password", "confirmpassword", "token", "authorization", "secret", "apikey",
        "creditcard", "cardnumber", "cvv", "cvc", "ssn", "pin", "otp",
        "refreshtoken", "accesstoken", "privatekey", "sessionid",
        "bearer", "auth", "credentials", "securitycode", "accountnumber"
    };

    public string[] SensitiveHeaders { get; set; } = new[]
    {
        "Authorization", "X-API-Key", "Cookie", "Set-Cookie",
        "X-Auth-Token", "X-CSRF-Token"
    };
}

public sealed class KafkaLoggingOptions
{
    public bool Enabled { get; set; }
    public string BootstrapServers { get; set; } = string.Empty;
    public string Topic { get; set; } = "eskineria-logs";
    public string? ClientId { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string SecurityProtocol { get; set; } = "Plaintext";
    public string SaslMechanism { get; set; } = "Plain";
    public string? RestrictedToMinimumLevel { get; set; }
    public KafkaEnvironmentOptions Environment { get; set; } = new();
}

public sealed class KafkaEnvironmentOptions
{
    public bool Enabled { get; set; } = true;
    public bool UseDotEnvFile { get; set; } = true;
    public string DotEnvFilePath { get; set; } = ".env";
    public string BootstrapServersKey { get; set; } = "ESKINERIA_KAFKA_BOOTSTRAP_SERVERS";
    public string UsernameKey { get; set; } = "ESKINERIA_KAFKA_USERNAME";
    public string PasswordKey { get; set; } = "ESKINERIA_KAFKA_PASSWORD";
}
