<!-- SPDX-License-Identifier: Apache-2.0 -->
<!-- Copyright (c) OpenEdge Labs -->
# Code signing policy

wEdge release files are signed so you can check they come from this project's
release workflow and were not changed since.

| What | How | Verify with |
|---|---|---|
| Every release file | Sigstore keyless signature (`<file>.sigstore.json`), made by the release workflow's GitHub identity | `cosign verify-blob` (see [SECURITY.md](../../SECURITY.md#verifying-downloads)) |
| Every release file | GitHub build provenance attestation | `gh attestation verify <file> --repo OpenEdge-Labs/wedge` |
| `.deb` and `.rpm` packages | GPG, key `wedge-release.asc` (fingerprint `B84A920DB6B511E646B124FD983EF3CDD108E023`) | `rpm -K` for `.rpm`; `gpg --verify` of the `_gpgorigin` member for `.deb` |
| Container image `ghcr.io/openedge-labs/wedge` | Sigstore keyless signature and build provenance in the registry | `cosign verify`, `gh attestation verify oci://...` |
| Windows `wedge.exe` and the MSI | Authenticode, once the project is approved by SignPath Foundation (planned) | Windows file properties, `Get-AuthenticodeSignature` |

## Windows signing (planned)

Free code signing provided by [SignPath.io](https://about.signpath.io/),
certificate by [SignPath Foundation](https://signpath.org/), once the project
is accepted. Until then the Windows files are unsigned for Authenticode
(Windows SmartScreen shows "unknown publisher"); check them with
`SHA256SUMS` and the Sigstore signature instead.

How it will work:

- Only the release workflow (`.github/workflows/release.yml`), running on
  GitHub-hosted runners from a version tag on `main`, submits signing
  requests. The signing key never leaves SignPath's hardware security module.
- One request signs `wedge.exe` inside the MSI and inside the portable zip,
  then the MSI itself (`packaging/windows/signpath-artifact-configuration.xml`).
- Every signing request needs manual approval by an approver.
- The release workflow tests the installer before signing it.

## Team roles

| Role | Who |
|---|---|
| Committers and reviewers | members of the [OpenEdge Labs](https://github.com/OpenEdge-Labs) organization with write access to this repository |
| Approvers (release signing) | OpenEdge Labs organization owners |

All changes reach `main` through commits by these members; `main` is
protected against force-pushes and deletion, and every release is built only
by the release workflow.

## Privacy

wEdge does not send any information to OpenEdge Labs or to anyone else: it
has no telemetry, no update check and no network access other than the
listeners and upstream servers you configure.
