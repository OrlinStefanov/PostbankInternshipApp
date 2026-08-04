namespace BinTool.UI.Services;

public static class AuthClaims
{
    /// <summary>
    /// Carries the API's bearer token inside the UI's own authentication cookie.
    /// <para>
    /// The cookie is encrypted by Data Protection and marked HttpOnly, so the token is
    /// never readable from JavaScript - unlike local storage. Keeping it as a claim also
    /// makes it reachable from inside a Blazor circuit, where the original HttpContext
    /// is long gone.
    /// </para>
    /// </summary>
    public const string AccessToken = "bintool:access_token";
}
