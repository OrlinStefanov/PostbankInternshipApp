## GetPermissions

The set is fixed in code, because each permission maps to real enforcement on an
endpoint. What is data-driven is which of these a role holds. Each entry carries a group,
so a client can lay the permissions out under headings.

## Create

Sample request:

    POST /api/Roles
    { "name": "Analyst", "description": "Reads and edits BIN ranges",
      "permissions": ["binranges.read", "binranges.write"] }

The name must be free, and every permission must be a catalog key. Members are assigned
separately, through the users endpoint.

## Update

The permissions are replaced wholesale, so send the complete set. The protected Admin
role is refused with a 409.

## Delete

Refused while any user still holds the role, and always for the protected Admin role.

