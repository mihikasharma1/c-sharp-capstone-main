namespace UserService.DTOs;

public class ProfileResponseDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string MembershipStatus { get; set; } = string.Empty;
    public DateTime? MemberSince { get; set; }
    public int ActiveReservations { get; set; }
    public int BorrowingHistory { get; set; }
}

public class UserValidateResponseDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string MembershipStatus { get; set; } = string.Empty;
    public int ActiveReservationsCount { get; set; }
}