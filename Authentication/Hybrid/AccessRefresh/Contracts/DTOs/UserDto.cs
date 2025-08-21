namespace AccessRefresh.Contracts.DTOs;

public record UserDto(
    int Id,
    string Username,
    string Role
);

public record UserTempDto(
    string Email,
    string Username,
    string PasswordHash,
    string ConfirmationToken
);