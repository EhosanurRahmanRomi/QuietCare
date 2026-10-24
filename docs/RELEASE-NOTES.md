# QuietCare Next 0.5.0-preview.3

Adds one-click Smart Care and expands the RAM Boost idle-app policy. The removed Process Monitor remains absent; the tiny transparent CPU / GPU / RAM monitor stays available.

- Start the recommended automatic care workflow from Home or the header on every page, with live steps, reasons, actual outcomes and Stop.
- Uses live RAM/load/power, Defender completion time and successful drive-operation history. Rechecks conditions before memory, security and drive actions; avoids duplicate runs and unnecessary recent scans/optimization.
- Runs guarded old-junk cleanup, pressure-aware memory trimming, startup-change review, due Defender quick scan and due Windows drive optimization. UAC may be required for Windows operations.
- Expanded RAM Boost includes eligible inactive visible apps after a longer idle observation. Up to 24 accepted trims, or 32 under high memory pressure, with tighter CPU-idle checks. Active work, media, project tools, system/security processes, browser-controller/GPU/extension processes and exclusions remain protected.
- Keeps three-second follow-up measurements and reports zero/negative available-RAM changes honestly. The floating Boost action still operates directly without opening a selection page.
- Persists the last care report locally. A Stop request does not terminate a running Windows scanner/optimizer; it prevents subsequent actions. Startup refreshes are serialized against the background watch.

Smart Care is a local decision engine, not an external AI service. Personal files, duplicate removal, app uninstall and service/startup changes retain review in their tools. No RAM decrease is guaranteed. See [validation scope](VALIDATION.md) and [automatic-care rules](SMART-CARE.md).

Validation: 74 core checks, 27 Boost checks, all 17 production pages plus monitor renders with zero binding errors, and live assessment-adapter reads. One explicitly requested guarded Boost pass on the local PC observed +294.1 MiB available RAM after three seconds; this does not establish a fixed 1 GiB gain. No app was terminated or tab discarded.

Interface refinement: compact gradient Smart Care header, theme-aware emerald/blue/violet/amber tool surfaces, clearer text/button contrast and optional 180 ms page fades. Monitor settings now fit the work area with scrolling and explicit readable background/foreground. Dark/light compact layouts and the reduced-motion override are included in render validation. The resource harness now uses fresh monitor settings and rejects zero-sample runs; a remembered hidden-state sample was rejected instead of being published as a zero-CPU result.

The About and update-check screens now read the build version from assembly metadata rather than retaining a hardcoded previous-preview label.
