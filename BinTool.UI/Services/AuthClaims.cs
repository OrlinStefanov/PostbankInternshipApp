namespace BinTool.UI.Services;

public static class AuthClaims
{
    // The cookie is encrypted by Data Protection and marked HttpOnly, so the token is never
    // readable from JavaScript - unlike local storage - and stays reachable from inside a Blazor
    // circuit, where the original HttpContext is long gone.
    public const string AccessToken = "bintool:access_token";
}
