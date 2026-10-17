# Security report handling

## Intake

- Public non-security bug reports belong in https://github.com/ServiceStack/Issues. The main ServiceStack repository has public issues disabled.
- Maintain the public bug-report template and private-security contact links in ServiceStack/Issues; keep vulnerability details out of public reports.

- Assign a primary maintainer and backup to monitor GitHub private reports and the account portal.
- Repository administrators and security managers should enable GitHub security-alert notifications and verify email delivery. Enabling private reporting alone does not configure each person's notifications.
- Acknowledge reports within the published three-business-day target and aim for an initial assessment within ten business days.
- Keep report contents and proof-of-concept details private during investigation.

## Assessment and remediation

- Reproduce the issue safely, record prerequisites, and determine affected packages and version ranges from evidence.
- Review severity and relevant configurations. Distinguish demonstrated impact from conditional impact.
- Add regression tests for both the vulnerability and authorized usage. Run relevant platform checks.
- Coordinate the fix and disclosure with the reporter and confirm their preferred credit.

## Release and publication

- Prepare a draft GitHub Security Advisory privately with affected packages, verified affected ranges, impact, mitigations, and fixed versions.
- Identify pre-release fixes separately from stable fixes. Do not invent an affected range when it is still under investigation.
- Link the advisory and upgrade guidance from release notes when disclosure is agreed.
- Publish the advisory at the coordinated disclosure time; request a CVE through the advisory workflow where appropriate.

## Reporting-channel maintenance

- Keep SECURITY.md and the canonical website policy consistent.
- Review the security.txt expiry before it is within 30 days of expiration and renew its date.
- Verify the reporting links and private-report notifications after configuration changes.
- Website CI checks source and generated artifacts; the scheduled website workflow checks the public endpoints.
