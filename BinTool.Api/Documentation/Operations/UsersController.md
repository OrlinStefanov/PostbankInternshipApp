## SetRoles

Sample request:

    PUT /api/Users/{id}/roles
    { "roles": ["Analyst", "Viewer"] }

The set is a replacement, not a delta: a role not listed is removed. Every role named
must exist. The change reaches the user's own access when their token is next issued, not
mid-session.


## GetUsers

Every user with the roles they currently hold. Roles are the only thing this API
changes about a user - accounts themselves are not created or deleted here.
