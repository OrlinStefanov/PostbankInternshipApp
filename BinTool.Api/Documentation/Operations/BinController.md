## Classify

Sample request:

    POST /api/bin/classify
    { "bin": "400001", "amount": 100 }

The lookup is a longest-prefix match. An 8-digit range is more specific than the
6-digit range it sits inside, so it wins; if no 8-digit range covers the BIN the
7-digit one is tried, then the 6-digit one. Only ranges that are valid today are
considered - a range that has expired, has not started yet, or has been deleted
is skipped, and a shorter range may then match in its place.

A full card number may be sent instead of a BIN. Only the leading 8 digits are
used: the rest is discarded before the lookup runs, is never stored or logged,
and the response echoes back only the truncated value.

When an `amount` is supplied and the BIN matches, the response also carries the
commission: the resolved rule (or the default, flagged as a fallback) and the fee.

No scheme ranges are hard-coded. Everything the response reports comes from the
BIN ranges and reference data in the database, so an import changes the answer.

