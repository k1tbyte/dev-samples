using System.Text.Json.Serialization;
using AccessRefresh.Services.Domain;

namespace AccessRefresh.Contracts.BrokerMessages;

[KafkaMessage(KafkaTopic.Notifications,"ACCOUNT_VERIFICATION", "email")]
public class AccountVerificationMessage 
{
    public required string Email { get; init; }
    public required string Username { get; init; }
    public required string Link { get; init; }
}

[KafkaMessage(KafkaTopic.Notifications, "PASSWORD_RESET", "email")]
public sealed class PasswordResetMessage : AccountVerificationMessage;