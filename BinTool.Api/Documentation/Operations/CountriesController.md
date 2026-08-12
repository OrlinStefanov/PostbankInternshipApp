## Search

Ordered by ISO code. Live rows only by default; `includeDeleted=true` also
returns soft-deleted ones, which is the only way to find a country before
restoring it.

Each row carries its region, because a country's region is what commission rules
match on - a client showing countries without it shows half the story.

## GetById

Soft-deleted countries are returned too, so a client can look at one before
deciding whether to restore it.

## Create

Sample request:

    POST /api/Countries
    { "isoCode": "us", "name": "United States", "regionId": 3 }

`isoCode` is ISO 3166-1 alpha-2 and is uppercased on the way in, so `us` and `US`
are the same country and only one of them can exist. The code must be free across
live and soft-deleted rows; a code already held by a soft-deleted row revives that
row in place with the supplied values and the response comes back as `Restored`.

`regionId` must name an existing region. Naming one that does not exist is
rejected rather than creating it.

## Update

Every field is replaced, so send the region as well as the name even when only
the name is changing. The ISO code may be changed as long as no other country
owns the new one.

Moving a country to another region changes which commission rules apply to cards
issued there, from the next classification onwards. Rules already written are not
rewritten - they match on region, and the country's region is simply different now.

A soft-deleted country cannot be edited; restore it first.

## Delete

The delete is soft, and is refused with `409 Conflict` while any live BIN range
still names this country as its issuing country. That is what stops a range from
being left pointing at a country that no longer exists.

Reassign or delete those ranges first - `GET /api/BinRanges?countryCode=XX` finds
them.

The ISO code stays reserved while the country is deleted, so adding it again
revives this row rather than creating a second one.

## Restore

The country returns with the values and region it had when it was deleted, and is
selectable again immediately. Unconditional: nothing about live data can block
bringing a deleted country back.
