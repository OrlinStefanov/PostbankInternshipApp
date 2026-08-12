## Search

Ordered by code. Live rows only by default; `includeDeleted=true` also returns
soft-deleted ones.

Every rate is `rateToEur` - the euro value of one unit - so euro is the fixed
point the arithmetic pivots through and sits at rate 1. A conversion between two
non-euro currencies goes through euro rather than through a direct pair, which is
why only one rate per currency is stored.

Reading takes `currencies.read`, not `referencedata.manage`: anyone pricing a
lookup has to pick the currency their amount is in, and that is not an admin's
privilege. The writes below take `referencedata.manage`.

## GetById

Soft-deleted currencies are returned too, so a client can look at one before
deciding whether to restore it.

## Create

Sample request:

    POST /api/Currencies
    { "code": "GBP", "name": "Pound sterling", "rateToEur": 1.17 }

`rateToEur` is the euro value of one unit, so a currency worth more than the euro
has a rate above 1. Getting the direction wrong is not detectable by the API - it
is a plausible number either way - so it is worth checking against a known amount
after adding one.

The code must be free across live and soft-deleted rows; a code already held by a
soft-deleted row revives that row with the supplied values and the response comes
back as `Restored`.

## Update

Both the name and the rate are replaced, so send them together.

A changed rate applies to fees calculated from that point on. Commission already
quoted is not recalculated - there is no as-of rate history, because the only
non-euro currency shipped so far is a fixed peg. That is worth revisiting before
adding a currency whose rate actually floats.

A soft-deleted currency cannot be edited; restore it first.

## Delete

The delete is soft, and is refused with `409 Conflict` while any live commission
rule is denominated in this currency - a rule pointing at a currency that no
longer exists could not be priced. Move those rules to another currency first.

The euro row is the base every rate is expressed against, so removing it would
leave every other rate meaningless.

## Restore

The currency returns with the code, name and rate it had when it was deleted, and
is selectable again immediately. The rate comes back as it was - check it before
using it if the currency was deleted a long time ago.
