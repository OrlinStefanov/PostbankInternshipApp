## Login

Sample request:

    POST /api/auth/login
    { "userName": "admin", "password": "Admin@123" }

Send the returned token on every other call as
`Authorization: Bearer <accessToken>`. It carries the user's roles, which is
what the API authorizes against - a client hiding a button changes nothing here.

The token expires at `expiresAtUtc`. There is no refresh token: when it expires
the user logs in again.

Either the user name or the email address works as `userName`.

A wrong password, an unknown user and a deactivated account all return the same 401
with `Reason: InvalidCredentials`, so the response cannot be used to discover
which accounts exist.

**Five failed attempts lock the account for five minutes.** A locked-out account is
reported distinctly as `Reason: LockedOut` with `lockoutEndsUtc`, because a
lockout is the one refusal the caller can neither see nor fix by retrying. The
password is still checked first, so **the correct password releases the lockout and
signs in immediately** - it clears the failed-attempt count and the lock rather than
making the owner wait it out.

## Me

Useful for confirming a token is still valid and seeing which roles it grants.

