# Contributing

Use Windows and the SDK pinned by `global.json`. Run the application build, isolated core fixtures and Boost helper suite before proposing changes. Use a new empty fixture directory for each run; never point tests at user data.

Keep system mutations explicit and narrowly scoped. Add meaningful regression coverage for file identities, cancellation, policy changes and failure reporting. Do not add generic elevated commands, remote bootstrap scripts, silent process termination, automatic registry repair or invented health/performance scores.

Distinguish fixture, rendered-control, live monitor, native interaction and hardware/OS coverage in validation reports. Screenshots must show this build and exclude private PC data. Preserve source notices and review dependency licenses before copying code.
