# Verification and standards
Status: proposed targets; no implementation has been verified yet.

## Standards alignment and evidence
| Reference | Use | Required evidence before release |
|---|---|---|
| ISO/IEC/IEEE 29148:2018 | Identified, testable requirements and traceability | NH IDs mapped to implementation/tests; reviewed changes |
| ISO/IEC/IEEE 42010:2022 | Stakeholders, concerns, architecture views and decisions | Updated design and ADRs reflecting implementation |
| OWASP ASVS 5.0, Level 2 target | Web/API security verification | Applicable control-by-control matrix with evidence/exclusions |
| RFC 9700 | Refresh rotation/replay principles; later OAuth integrations | Session race/reuse/revocation tests; no OAuth compliance claim for custom login |
| RFC 7519 and RFC 8725 | JWT format and validation practice | Issuer/audience/algorithm/key rotation/expiry negative tests |
| RFC 9457 | HTTP Problem Details | Contract snapshots and error examples |
| OpenAPI 3.x | Machine-readable versioned API contract | Valid generated schema, examples and contract checks |
| WCAG 2.2 AA | Accessible web experience, extends source 2.1 AA target | Automated checks plus manual keyboard/screen reader review |

IEEE/ISO documents are normative publications; this scaffold is standards-informed
and is not an independently assessed conforming or certified product. Full
conformance would require access to applicable normative text and a formal
review of required artifacts. Populate exact ASVS control IDs during implementation
from the stable standard, not invented mappings.

## Security verification checklist
- [ ] Cross-workspace access denied on every write, read projection, job and audit path.
- [ ] Dapper SELECT role cannot write, access secret columns, or bypass query scoping.
- [ ] Refresh rotation atomic; reuse revokes family; concurrent refresh fails safely.
- [ ] Logout/reset/revoke disables active session; JWT algorithms and claims validated.
- [ ] CSRF/Origin checks protect cookie endpoints; CORS rejects untrusted origins.
- [ ] API key shown once, hashed, scoped, rate limited, revocable; rotation tested.
- [ ] Admin/owner MFA and step-up required; recovery and password reset single use.
- [ ] Credential encryption detects tampering; nonce uniqueness and key rotation verified.
- [ ] Webhook/push SSRF tests cover private/reserved IPs, IPv6, rebinding and redirects.
- [ ] Logs/traces/errors/exports contain no raw credentials or unauthorized message bodies.
- [ ] CSP/XSS validation, safe template rendering and payload limits tested.
- [ ] Scanners run on code/dependencies/images; SBOM recorded; triage documented.

## Reliability and operational checklist
- [ ] Committed acceptance survives process failure with no job loss.
- [ ] Concurrent worker claims and expired leases cannot overwrite fresh completion.
- [ ] Retry/backoff, provider 429, terminal errors and manual replay verified.
- [ ] External uncertain outcomes and possible duplicates documented in UI/runbook.
- [ ] Quiet-hours DST fixtures cover ambiguous/nonexistent local times.
- [ ] Retention clears protected payload data and respects legal/operator configuration.
- [ ] Isolated restore recovers encryption keys, database and queue without live sends.
- [ ] Fresh install/upgrade/migration rollback plan and shutdown behavior exercised.
- [ ] Performance report names hardware, concurrency, dataset, percentiles and failures.
- [ ] Browser/viewport, theme, keyboard, focus, contrast and screen reader reviewed.

## Source references (checked 2026-10-01)
- .NET support policy: https://dotnet.microsoft.com/en-us/platform/support/policy
  Baseline is stable .NET 10 LTS; recheck patch/SDK compatibility when coding.
- Requirements standard: https://www.iso.org/standard/72089.html
- Architecture description: https://www.iso.org/standard/74393.html
- OWASP ASVS: https://github.com/OWASP/ASVS/releases
- OAuth security BCP: https://www.rfc-editor.org/rfc/rfc9700.html
- JWT: https://www.rfc-editor.org/rfc/rfc7519
- JWT security: https://www.rfc-editor.org/rfc/rfc8725
- Problem Details: https://www.rfc-editor.org/rfc/rfc9457
- Accessibility: https://www.w3.org/TR/WCAG22/

## Release evidence record
For each NH requirement/ASVS applicable control record: requirement/control ID,
implementation location, test ID/command, execution date/environment, outcome,
reviewer, known limitations. Keep unexecuted checks explicitly unverified.
