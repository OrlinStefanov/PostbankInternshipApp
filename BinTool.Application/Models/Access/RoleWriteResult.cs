namespace BinTool.Application.Models.Access;

public readonly record struct RoleWriteResult(bool Succeeded, string Error, RoleRecord? Role)
{
    public static RoleWriteResult Ok(RoleRecord role) => new(true, string.Empty, role);

    public static RoleWriteResult Refused(string error) => new(false, error, null);
}
