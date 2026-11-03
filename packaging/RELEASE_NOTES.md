<!-- SPDX-License-Identifier: Apache-2.0 -->
<!-- Copyright (c) OpenEdge Labs -->
**wEdge** (`wedge`) 0.2.0 brings a new Windows installer, an admin page with
administrator logins, and the container image on GitHub Packages. The
project now lives at <https://github.com/OpenEdge-Labs/wedge>.

## New Windows installer

- Asks for the install folder, the **web application folder**, the
  **configuration folder**, the **web port** (optionally reachable from other
  computers, with a Windows Firewall rule), the **admin port** and an
  **administrator account**.
- Finds an older wEdge and offers to **upgrade** it (keeping its folders,
  configuration, content and accounts) or to **uninstall it and install
  fresh**, optionally deleting its configuration and the content it created.
- Adds **Start menu and install-folder shortcuts**: start, stop, restart and
  status of the service, edit the configuration (checked, then reloaded), the
  admin page, the web, configuration and log folders, the user guide, and
  uninstall.
- Locks the install and configuration folders to Administrators and SYSTEM.
  Every choice is also a property for silent installs; see the
  [Windows install guide](https://github.com/OpenEdge-Labs/wedge/blob/main/docs/install/windows.md).
- New installs keep everything under the install folder by default
  (`conf\wedge.toml`, `www`, `logs`). Upgrades from 0.1 keep using
  `C:\ProgramData\wedge`.
- Checks the choices on each dialog page before installing anything: ports
  that are invalid, equal or already in use, and an account name or password
  wEdge would refuse (at least 12 characters), are reported right there.
- Installs to a folder on a second drive (such as `D:\wedge`), and setup
  steps record wEdge's own error message in the installer log.

## Admin page and accounts

- `[server.admin] users = "file:admin-users.txt"` turns on administrator
  accounts: the admin listener then shows a **status page** (version,
  uptime, connections, listeners, virtual hosts, upstream health) with a
  **Reload configuration** button, and every admin request, `/metrics`
  included, needs a login.
- Only Argon2id password hashes are stored; five failed logins lock the
  address out for five minutes; the Reload button refuses cross-site
  requests; the page runs no scripts and cannot be framed.
- See the [admin page guide](https://github.com/OpenEdge-Labs/wedge/blob/main/docs/guide/admin.md).

## New commands

- `wedge init` writes a starter configuration and web folder.
- `wedge admin set-password` creates an account or changes a password
  (asked without echo).
- Windows: `wedge service start | stop | restart | status | reload`, and
  `wedge service install --log-dir`.

## Changed

- **Windows service check.** `wedge service install`, and the service at
  every start, now check the folder permissions of `wedge.exe` and every
  folder above it instead of requiring Program Files: any folder only
  administrators can change is accepted.
- **Container image on GitHub Packages:** `ghcr.io/openedge-labs/wedge:0.2.0`
  (amd64 and arm64), signed with Sigstore and with build provenance.
- **Minimum Rust version for building from source: 1.88** (was 1.80), for
  the fixed `rpassword` (GHSA-2p6r-x3vv-xqm2, low severity).
- Copyright holder: OpenEdge Labs.

## Downloads

| File | For |
|---|---|
| `wedge-0.2.0-x86_64.msi` | Windows installer |
| `wedge-0.2.0-x86_64-pc-windows-msvc.zip` | Windows, portable |
| `wedge_0.2.0-1_{amd64,arm64}.deb` | Debian, Ubuntu, Kali |
| `wedge-0.2.0-1.{x86_64,aarch64}.rpm` | Fedora, RHEL and compatible |
| `wedge-0.2.0-{x86_64,aarch64}-unknown-linux-musl.tar.gz` | any Linux (static) |
| `wedge-0.2.0-{x86_64,aarch64}-unknown-linux-gnu.tar.gz` | Linux with glibc 2.17+ |
| `wedge-0.2.0-{aarch64,x86_64}-apple-darwin.tar.gz` | macOS |
| `ghcr.io/openedge-labs/wedge:0.2.0` | container image (also as `wedge-0.2.0-oci-{amd64,arm64}.tar`) |
| `wedge-0.2.0.cdx.json` | software bill of materials (CycloneDX 1.5) |

Install guides: <https://github.com/OpenEdge-Labs/wedge/blob/main/docs/install/README.md>.
Every package and archive contains the user guide (Markdown and HTML).

## Verifying

```sh
sha256sum --ignore-missing -c SHA256SUMS
gh attestation verify <file> --repo OpenEdge-Labs/wedge
cosign verify-blob <file> --bundle <file>.sigstore.json \
  --certificate-identity-regexp '^https://github.com/OpenEdge-Labs/wedge/' \
  --certificate-oidc-issuer https://token.actions.githubusercontent.com
cosign verify ghcr.io/openedge-labs/wedge:0.2.0 \
  --certificate-identity-regexp '^https://github.com/OpenEdge-Labs/wedge/' \
  --certificate-oidc-issuer https://token.actions.githubusercontent.com
```

Signatures use the newer Sigstore bundle format: verify with cosign 3, or add
`--new-bundle-format` with cosign 2.4 or later. The .deb and .rpm packages
are GPG-signed with the key in `wedge-release.asc` (fingerprint
`B84A 920D B6B5 11E6 46B1 24FD 983E F3CD D108 E023`).

## Known limitations

- The Windows executable and installer are not yet Authenticode-signed
  (SignPath Foundation, pending), so Windows shows an "unknown publisher"
  prompt. macOS binaries are not notarized.
- The admin page uses HTTP Basic authentication: keep the admin listener on
  loopback (the default) or a trusted management network.
- No TLS to upstreams, WebSocket tunnelling or HTTP/2 to upstreams yet.
- No per-client connection limit.
- The installer is tested silently on a GitHub-hosted Windows runner (fresh
  install, second drive, upgrade from 0.1.1, reinstall with data deletion)
  and was installed and uninstalled through its dialogs on one Windows 11
  desktop; it has not been tested on Windows 10 or on a clean VM. The macOS
  install guide is untested.
