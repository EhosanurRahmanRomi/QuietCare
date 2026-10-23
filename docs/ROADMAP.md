# Roadmap

The order below is a proposed engineering direction, not a delivery promise. Scope should be driven by measured usefulness and preservation of active work.

## Available in v0.2.0

- [x] Compact transparent CPU/GPU/RAM monitor with configurable cadence.
- [x] Integrated title-free main canvas and compact navigation.
- [x] Direct guarded background working-set trim with media/project exclusions.
- [x] Recoverable old-temp cleanup and current-user startup backup/restore.
- [x] Read-only file/app/system tools and explicit Windows integrations.
- [x] Local validation evidence and public source/build documentation.

## Next reliability work

- [ ] Broader GPU, multi-monitor and per-monitor DPI validation.
- [ ] Keyboard, screen-reader and high-contrast audit.
- [ ] Long-duration CPU/memory and battery measurement.
- [ ] Installation upgrade/migration and repeated lifecycle coverage.
- [ ] Independent security review and a signing/distribution plan.
- [ ] More readable modular UI/source organization as the project grows.

## Potential improvements

- [ ] Clearer per-action progress, cancellation and result history.
- [ ] User-managed application protection profiles and more compatibility evidence.
- [ ] Modern project/solution metadata and reproducible-build tooling.
- [ ] Additional read-only diagnostics with measured resource budgets.

Changes that kill applications, discard browser tabs, weaken security controls, or apply unexplained registry/network tweaks are outside the current design direction. Temperature sensors or a driver-based module require a separate design and security review.
