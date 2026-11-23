# Security policy

## Supported versions

WingetUI has not published a stable release. Security fixes currently apply to the latest maintained development branches. A merged change is not a published release.

## Reporting a vulnerability

Do not open a public issue containing credentials, personal data, exploit instructions, or other sensitive evidence. Use GitHub's private vulnerability reporting feature for this repository when it is available. If private reporting is unavailable, contact the repository owner through their GitHub profile without including sensitive details in the initial message.

Do not invent or use an unverified security email address.

Include the affected commit or version, Windows and App Installer versions, impact, reproduction conditions, and the smallest sanitized evidence needed to investigate.

## Security boundaries

- WingetUI does not accept or store account credentials.
- Package discovery and installation are performed through the Windows App Installer WinGet COM API.
- The application runs as the current user by default. Administrator restart requires an explicit Windows elevation confirmation.
- An elevated session requests silent installer mode, but individual installers and Windows policy can still display prompts.
- Package identity, source, installed version, and target version are revalidated immediately before execution.
- Activity history is stored locally and can reveal package names, identifiers, versions, timestamps, and operation outcomes.
- The 0.1.0 preview executable is not code-signed. Verify its SHA-256 checksum against the GitHub release asset before running it.

See [configuration and data](docs/configuration.md) for storage details and [troubleshooting](docs/troubleshooting.md) for safe diagnostics.
