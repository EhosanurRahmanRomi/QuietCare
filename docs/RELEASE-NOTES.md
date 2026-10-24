# QuietCare Next 0.5.0-preview.2

Removes the Process Monitor tab after a reported UI freeze and corrects RAM Boost result reporting.

- Completely removes the process page, synchronous two-second process polling, registrations and unused process-control backend.
- Keeps the tiny transparent CPU → GPU → RAM monitor.
- RAM Boost measures each trimmed process again after three seconds. An immediate drop that refills is no longer reported as a lasting reduction.
- Shows actual overall available-RAM changes in bytes and RAM-use percentages to one decimal place. Process working-set totals are labeled separately because shared pages can overlap.
- Displays live Boost progress/results on the home screen and in a temporary panel beside the floating monitor. Results remain accessible after restart.
- States clearly when no overall RAM gain remains, no idle process qualifies, or Windows declines a request.

54 core checks and 16 Boost checks passed, including refilled memory, small positive changes, negative overall changes, cancellation and allocated-data preservation in a dedicated 96 MB helper. The core count is four lower because tests for the removed process-control backend were removed with it.

This does not close user apps/tabs or free their allocations. Resident memory can return when used. No reduction is guaranteed. See [validation scope](VALIDATION.md) for tested and untested behavior.
