## Search

Expired rules are hidden by default; pass `includeExpired=true` to keep them in
the listing (visually distinguished by their `Expired` status). Soft-deleted
rules only appear when `includeDeleted=true`.

## GetById

Soft-deleted and expired rules are returned too. The response carries the rule's
key (scheme, product type, funding type, region - any of them null for a
wildcard), its rates, its currency and its validity window, which is everything
the editor needs to open it without a second call.

## Create

A null scheme, product or region is a wildcard. The validity window must not overlap
an existing rule that shares the same key - the save is refused with 409 and the
conflicting rule is named.

## Update

The rule is replaced wholesale, so send every field. The key may be changed, and
the same overlap check applies against the new key rather than the old one: a
rule moved onto a key that is already covered for those dates is refused with
`409 Conflict`, naming the rule it would have collided with.

A soft-deleted rule cannot be edited - restore it first.

## Delete

The delete is soft. The rule stops being considered when a card is priced and
drops out of the default listing, but the record stays and
`POST /api/CommissionRules/{id}/restore` brings it back.

Refused with `409 Conflict` while the rule is the configured default: a default
that silently disappeared would change what an unmatched card costs without
anyone asking for it. Point the default at another rule, or clear it, first.

## Restore

The rule returns with the values it had when it was deleted and is considered
again from that moment, subject to its own validity dates - so restoring an
expired rule brings back a rule that still matches nothing.

Being restored does not make a rule the default again. If it was the default
before, set it again explicitly.

## SetDefault

The fallback applied when no rule matches a classified card. There is at most
one, so this replaces whatever was default before rather than adding to a list -
no request is needed to unseat the previous holder.

The rule must be live and active: a soft-deleted or inactive rule is refused with
`400`, because a default that matches nothing is the same as having no default
while looking like it has one.

## ClearDefault

Leaves the system with no fallback, so a card that matches no rule is priced at
no fee rather than at a default. Succeeds when there was no default configured -
the request states the end state, not a transition, so a client does not have to
check first.
