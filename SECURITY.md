# Security policy

Report a vulnerability through GitHub private vulnerability reporting when available. Do not open a public issue containing a secret, private key, certificate credential, production payload, invoice, NIP, or customer data.

The probe accepts untrusted local directories/ZIP files and therefore applies path traversal, size, executable-content, symlink, and private-key-marker protections. These controls reduce risk but do not make arbitrary files trustworthy.

Supported security updates cover the latest public probe release only.
