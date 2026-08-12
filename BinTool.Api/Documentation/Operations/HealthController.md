## Get

Anonymous, so a load balancer or container probe can call it without a token.

It answers whether the process is up and serving requests - nothing more. It does
not open a database connection, so a `200` here alongside failures elsewhere means
the API is running and its dependencies are not. That is deliberate: a probe that
fails when the database is briefly unreachable takes the API out of rotation for a
fault restarting it will not fix.

`timestamp` is UTC, and is useful for spotting a clock that has drifted between
hosts - the JWT lifetime is validated with no clock skew allowance, so a skewed
host rejects tokens that are still valid everywhere else.
