<!-- SPDX-License-Identifier: Apache-2.0 -->
<!-- Copyright (c) OpenEdge Labs -->
# Running as a service

## systemd (Debian, Ubuntu, Kali, Fedora, RHEL)

The .deb and .rpm packages install `/usr/lib/systemd/system/wedge.service`
and a `wedge` system user. Enable and start it:

```sh
sudo systemctl enable --now wedge
systemctl status wedge
journalctl -u wedge -f            # follow the logs
sudo systemctl reload wedge       # apply configuration changes
```

The unit checks the configuration before starting (`ExecStartPre`), runs as
the `wedge` user with only the right to bind ports below 1024, and applies
systemd's sandboxing (read-only system, no home directories, private `/tmp`
and devices, restricted system calls, no core dumps). `systemd-analyze
security wedge` rates it 1.6 ("OK") on its 0–10 exposure scale.

To change environment variables (for example the log format) without
editing the packaged unit:

```sh
sudo systemctl edit wedge
# [Service]
# Environment=WEDGE_LOG_FORMAT=json
```

If a document root is outside `/srv`, `/var/www` or `/usr/share`, it is still
readable (the system is read-only, not hidden), but files must be readable by
the `wedge` user.

## OpenRC (Alpine)

The Linux tarballs include `openrc/wedge`; the
[Alpine guide](../install/alpine-3.20.md) installs it:

```sh
sudo rc-update add wedge default
sudo rc-service wedge start
sudo rc-service wedge reload
```

Logs go to `/var/log/wedge.log`.

## Windows service

The [Windows installer](../install/windows.md) registers and starts the
service and adds Start menu and install-folder shortcuts (**Start wEdge**,
**Stop wEdge**, **Restart wEdge**, **wEdge status**, **Edit wEdge
configuration**). They run `wedge-control.cmd`, which asks for administrator
rights when needed.

From an elevated PowerShell the same controls are:

```powershell
wedge service status        # works without elevation
wedge service start
wedge service stop
wedge service restart
wedge service reload        # reload the configuration
```

Without the installer (the portable zip), copy `wedge.exe` to a folder only
administrators can change, then:

```powershell
& 'C:\Program Files\wedge\wedge.exe' init --config 'C:\Program Files\wedge\conf\wedge.toml' --web-root 'C:\Program Files\wedge\www' --admin-port 9090
& 'C:\Program Files\wedge\wedge.exe' admin set-password --config 'C:\Program Files\wedge\conf\wedge.toml'
& 'C:\Program Files\wedge\wedge.exe' service install -c 'C:\Program Files\wedge\conf\wedge.toml' --log-dir 'C:\Program Files\wedge\logs'
& 'C:\Program Files\wedge\wedge.exe' service start
& 'C:\Program Files\wedge\wedge.exe' service uninstall    # to remove it
```

- The service runs as the virtual account `NT SERVICE\wedge`, not as
  LocalSystem. Install grants it read access to the configuration folder,
  the site folders and the admin users file, and write access to the log
  folder only (`--log-dir`; default `C:\ProgramData\wedge\logs`).
- It starts automatically at boot and restarts after a crash (three times,
  5 seconds apart).
- Install, and the service at every start, check that ordinary users cannot
  change `wedge.exe`, its folder or any folder above it: otherwise they could
  replace the binary (or plant a DLL beside it) and have it run as the
  service. Program Files passes; so does a folder the installer created,
  which it locks to Administrators and SYSTEM. A folder you made yourself
  under `C:\` usually fails, because Windows lets every signed-in user modify
  new folders there; lock it with
  `icacls <folder> /inheritance:r /grant:r *S-1-5-18:(OI)(CI)F *S-1-5-32-544:(OI)(CI)F *S-1-5-32-545:(OI)(CI)RX`.
  For a test machine only, `--allow-unprotected-path` skips the check.

## Containers

The OCI image contains only the static binary, a default configuration and a
test page, and runs as user 65532. From 0.2 it is published on GitHub
Packages for amd64 and arm64:

```sh
docker pull ghcr.io/openedge-labs/wedge:0.2.0
docker run -d --name wedge --read-only --cap-drop ALL \
  --security-opt no-new-privileges -p 8080:8080 \
  -v /srv/site:/srv/www:ro -v /etc/wedge:/etc/wedge:ro ghcr.io/openedge-labs/wedge:0.2.0
```

Check the image before running it (the signature and build provenance are
stored in the registry next to it):

```sh
cosign verify ghcr.io/openedge-labs/wedge:0.2.0 \
  --certificate-identity-regexp '^https://github.com/OpenEdge-Labs/wedge/' \
  --certificate-oidc-issuer https://token.actions.githubusercontent.com
gh attestation verify oci://ghcr.io/openedge-labs/wedge:0.2.0 --repo OpenEdge-Labs/wedge
```

Each release also has the image as a file (`wedge-<version>-oci-<arch>.tar`)
for machines without registry access: `docker load -i <file>`, then run
`wedge:<version>`.

Inside the container wedge listens on `0.0.0.0:8080`; choose the exposure
with `-p`. To bind port 80 inside the container instead, add
`--cap-add NET_BIND_SERVICE`. Reload with `docker kill -s HUP wedge`.
