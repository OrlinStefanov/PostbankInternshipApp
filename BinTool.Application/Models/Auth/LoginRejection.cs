namespace BinTool.Application.Models.Auth;

public class LoginRejection
{
    public LoginRejectionReason Reason { get; set; }

    // Only set when locked out.
    public DateTime? LockoutEndsUtc { get; set; }
}
