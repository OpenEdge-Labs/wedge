<!-- SPDX-License-Identifier: Apache-2.0 -->
<!-- Copyright (c) OpenEdge Labs -->
# Admin page and accounts

The admin listener is a second, private port. With administrator accounts
configured it serves:

| Address | What |
|---|---|
| `GET /` | the status page: version, uptime, connections, listeners, virtual hosts with request counts, upstream health, and a **Reload configuration** button |
| `GET /metrics` | Prometheus metrics (see [Operations](operations.md#metrics)) |
| `POST /-/reload` | reload the configuration (the button, `wedge reload`, scripts) |

The Windows installer sets this up for you when you give an administrator
password: the page is at `http://127.0.0.1:9090/` (or the port you chose),
and the Start menu has a **wEdge admin page** shortcut.

## Turning it on

```toml
[server]
listen = ["127.0.0.1:8080"]

[server.admin]
listen = "127.0.0.1:9090"        # keep it on loopback
users = "file:admin-users.txt"   # relative to this file
```

Then create the account. The password is asked twice, without echo:

```sh
sudo wedge admin set-password --config /etc/wedge/wedge.toml --user admin
```

On Windows, run it from an elevated PowerShell:

```powershell
wedge admin set-password --config "C:\Program Files\wedge\conf\wedge.toml" --user admin
```

Run the same command again to change a password or add another account
(`--user ops`). A running wedge picks the change up on its next reload. To
remove an account, delete its line from the users file and reload.

`wedge init --admin-port 9090` writes a configuration with this section
already in it (see [Getting started](getting-started.md)).

## How the accounts are protected

- **No passwords on disk.** The users file holds one line per account,
  `name:$argon2id$...`: an Argon2id hash (19 MiB, 2 passes, the OWASP
  recommendation) with a random salt. wedge refuses to start if a line is
  not such a hash, and the error never shows the line's content.
- **The users file is private.** `wedge admin set-password` writes it so that
  only administrators (root on Linux, Administrators and SYSTEM on Windows)
  and the wedge service can read it. On Linux, give the service group read
  access: `sudo chown root:wedge admin-users.txt`.
- **Password rules.** 12 to 1024 characters, no control characters. User
  names: 1 to 64 letters, digits, `.`, `_`, `-` or `@`.
- **Lockout.** Five failed logins from one address within 15 minutes lock
  that address out for 5 minutes (answer `429`, with `Retry-After`). Each
  failure is logged with the address and user name, never the password.
- **Same timing for unknown users.** A wrong user name takes as long as a
  wrong password, so response times do not reveal which accounts exist.
- **Loopback only by default.** Clients outside `allow` are disconnected
  before any HTTP is read, and requests whose `Host` is not an IP address or
  `localhost` get `421` (this blocks DNS rebinding).
- **Cross-site requests are refused.** The Reload button only works from the
  admin page itself: a browser `POST` whose `Origin` or `Sec-Fetch-Site`
  shows another site gets `403`, so a web page you visit cannot use your
  saved login.
- **The page loads nothing from elsewhere.** It has no scripts, is sent with
  `Content-Security-Policy: default-src 'none'; style-src 'self'; ...`,
  cannot be framed, is never cached, and escapes every value it shows.

## Signing in and out

The browser asks for the user name and password (HTTP Basic authentication).
Over loopback the password does not leave the computer. If you put the admin
listener on a network address, use it only on a trusted management network:
Basic authentication over plain HTTP is readable by anyone on the path.

Browsers remember the login until they close; close the browser to sign out.

## Metrics and scripts with a login

With `users` set, `/metrics` needs a login too. Prometheus:

```yaml
scrape_configs:
  - job_name: wedge
    basic_auth:
      username: metrics
      password_file: /etc/prometheus/wedge-password
    static_configs:
      - targets: ["127.0.0.1:9090"]
```

Create a separate `metrics` account for it. `wedge reload` keeps using the
bearer `token`, which works with or without accounts:

```toml
[server]
listen = ["127.0.0.1:8080"]

[server.admin]
listen = "127.0.0.1:9090"
users = "file:admin-users.txt"
token = "file:admin-token"
```
