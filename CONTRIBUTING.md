# Contributing to wEdge

Thank you for helping improve wEdge. Bug reports, documentation fixes, tests and code are all welcome.

## Before you start

- **Security problems:** don't open a public issue or pull request. Report them privately as described in [SECURITY.md](SECURITY.md) (the repository's Security tab → *Report a vulnerability*).
- **Bugs:** open an [issue](https://github.com/OpenEdge-Labs/wedge/issues). Include the wedge version (`wedge --version`), your OS, the configuration that triggers the problem (with secrets removed) and the log lines around it.
- **Larger changes:** open an issue first to agree on the approach before you write a lot of code.

## Development setup

You need Rust (the minimum supported version is in `rust-version` in [Cargo.toml](Cargo.toml)) and Git. Windows also needs PowerShell 7 for the documentation checks.

```sh
git clone https://github.com/OpenEdge-Labs/wedge.git
cd wedge
cargo build
cargo test --all
```

## Checks your change must pass

CI runs these on every pull request, so it saves time to run them locally first:

```sh
sh scripts/check-spdx.sh                         # licence header on every source file
cargo fmt --all --check
cargo clippy --all-targets --locked -- -D warnings
cargo test --all --locked -q
pwsh scripts/check-doc-examples.ps1              # configuration examples in the docs stay valid
pwsh scripts/gen-config-reference.ps1            # then commit docs/guide/configuration-reference.md if it changed
```

The security workflow also runs `cargo audit`, `cargo deny check` and Semgrep. A new dependency must have a licence that [deny.toml](deny.toml) allows.

## Guidelines

- **Source file header:** every new `.rs`, `.toml`, `.sh`, `.yml` or `.yaml` file starts with:
  ```
  // SPDX-License-Identifier: Apache-2.0
  // Copyright (c) OpenEdge Labs
  ```
  Use the file type's comment syntax.
- **Tests:** add tests for new behaviour and for every bug you fix.
- **Secure defaults:** keep the defaults safe. Secrets are configured only by reference (`env:` or `file:`), never inline, and must never reach a log.
- **Documentation:** update the guide in [docs/guide/](docs/guide/) when you change behaviour or configuration.
- **Commits:** keep each commit to one logical change, and use [Conventional Commits](https://www.conventionalcommits.org/) messages (`feat:`, `fix:`, `docs:`, `test:`, `build:`, `ci:`, `chore:`).
- **Pull requests:** say what the change does, why, and how you tested it.

## Licence

wEdge is licensed under the [Apache License 2.0](LICENSE). Contributions are accepted under the same licence, as set out in section 5 of the licence.

## Community

Everyone taking part is expected to be respectful and constructive. Project news and other OpenEdge Labs projects are at <https://openelabs.com>.
