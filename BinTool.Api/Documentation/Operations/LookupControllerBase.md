## Search

By default only live rows are returned. Pass `includeDeleted=true` to also
receive soft-deleted rows - the only way to find one before restoring it.

## Create

Names are case-insensitively unique across live and soft-deleted rows. Naming
a value that exists on a soft-deleted row revives that row in place with the
supplied values and the response comes back as `Restored` - the row keeps
its id and its audit history.

