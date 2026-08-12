## Search

By default only live rows are returned. Pass `includeDeleted=true` to also
receive soft-deleted rows - the only way to find one before restoring it.

## GetById

Soft-deleted rows are returned too, with `isDeleted: true`. A client deciding
whether to restore a row needs to look at it first, so this read does not hide
them the way the default listing does.

## Create

Names are case-insensitively unique across live and soft-deleted rows. Naming
a value that exists on a soft-deleted row revives that row in place with the
supplied values and the response comes back as `Restored` - the row keeps
its id and its audit history.

## Update

Both fields are replaced, so send the name as well as the description even when
only one of them is changing. The new name must be free on every other row,
deleted rows included.

A soft-deleted row cannot be edited: restore it first, so bringing a value back
into use is never a side effect of correcting its spelling.

## Delete

The delete is soft - the row keeps its id and its history, and
`GET ?includeDeleted=true` still finds it.

Refused with `409 Conflict` while live data still points at the row: BIN ranges
for a card scheme, product type or funding type, and live countries for a region.
That is what stops a delete from orphaning rows that name it. Reassign or remove
the dependents first, or leave the value in place - a value nobody selects any
more costs nothing.

The name stays reserved while the row is deleted. Adding it again revives this
row rather than creating a second one with the same name.

## Restore

The row returns with the values it had when it was deleted and is selectable
again immediately. Unconditional - nothing about a live row can block bringing
a deleted one back. Restoring a row that was never deleted is refused with
`409 Conflict` rather than silently succeeding.
