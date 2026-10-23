# Contributing to QuietCare

Thanks for helping improve a small, understandable Windows utility. Focused changes with clear behavior and evidence are easiest to review.

## Start here

Read the [architecture](docs/ARCHITECTURE.md), [security model](docs/SECURITY-MODEL.md), [feature boundaries](docs/FEATURES.md), and [build guide](docs/BUILDING.md). Open an issue before a large feature or architectural change so its scope can be discussed.

## Development loop

1. Fork/clone the repository and create a branch for one focused change.
2. Build with `build.ps1` on Windows x64.
3. Run checks that exercise the behavior you changed; core changes generally need `test.ps1`, UI changes need `test-ui.ps1` plus native visual review, and installer changes need `package.ps1` then `test-installer.ps1`.
4. Run `scripts/check-repository.ps1` and `git diff --check`.
5. Describe the problem, resulting behavior, evidence and remaining limits in your pull request.

Do not upload settings, recovery files, recordings, credentials, private logs, real startup command lines or build/test fixture folders. Use synthetic data when demonstrating a bug. Binaries belong in releases, not source commits.

## Engineering expectations

- Preserve the current user's active work. Keep process/file/registry changes narrow, explainable and recoverable where possible.
- Keep the compact monitor inexpensive. Measure resource changes under stated conditions.
- Do not call a lower working set a speed improvement without evidence.
- Distinguish pure/mocked policy tests, WPF layout checks, CI compilation and native hardware review.
- Keep Windows launchers fixed/allowlisted. Do not execute inventory-provided commands or download code at runtime.
- Make changes compatible with the Windows-bundled Framework compiler unless the build system is deliberately migrated.
- Avoid unrelated mass formatting; the preview source has compact legacy formatting that can be improved in focused follow-ups.

## Licensing and conduct

Submit only material you have the right to contribute. Contributions are offered under the repository's [MIT license](LICENSE). Credit external references and review their licenses before proposing code reuse. Treat people respectfully, discuss behavior and evidence, and keep private information out of public issues. Vulnerabilities should follow [SECURITY.md](SECURITY.md).
