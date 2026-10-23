# Performance measurements

The design goal is a useful small monitor with bounded sampling work. Faster polling costs CPU; it is a user choice rather than a claim of identical resource use at every setting.

## Local v0.3.0 observation

20 seconds per case after 5-second warmup, same i5-8350U / Intel UHD 620 / 16 GB / 8 logical CPU reference PC. Other workloads were active.

| Refresh | Total CPU | Working set |
| --- | ---: | ---: |
| 1000 ms | 0.185% | 97.3 MB |
| 250 ms | 0.722% | 98.6 MB |
| 100 ms | 1.749% | 100.5 MB |

Fully hidden: 0.0% observed CPU; polling disabled; main UI unconstructed in every case. Raw [0.3.0 JSON](evidence/v0.3.0/benchmark-results.json).

## Historical v0.2.0 observation

Reference machine: Windows build 26200, Intel i5-8350U, 8 logical CPUs, Intel UHD 620, 16 GB RAM. The production main-window/widget/telemetry code was compiled with a separate benchmark entry point, started in tray mode, warmed for five seconds, then measured for about 20 seconds. No forced garbage collection was used. Other workloads were active.

| Mode | Refresh | Total CPU | Working set | Main UI constructed | Polling enabled |
| --- | ---: | ---: | ---: | --- | --- |
| Compact monitor | 1,000 ms | 0.146% | 96.9 MB | No | Yes |
| Compact monitor | 250 ms | 0.703% | 97.7 MB | No | Yes |
| Compact monitor | 100 ms | 1.601% | 99.8 MB | No | Yes |
| Fully hidden | 250 ms preference | 0.000% observed | 52.8 MB | No | No |

CPU percentage = process CPU seconds ÷ elapsed seconds ÷ logical CPU count × 100. The hidden observation is a short sample with polling disabled, not a universal zero-CPU claim. Working set is resident process memory, not total allocation. Earlier measurements that constructed the main UI were excluded from compact-only comparisons; all final compact rows above kept it unconstructed.

Raw annotated data: [benchmark-report.json](evidence/v0.2.0/benchmark-report.json).

## Changes that reduce work

- Build the main shell only when opened; release it on close to tray.
- Collect CPU/GPU/RAM for compact mode, with disk/network detail on demand.
- Reuse the native GPU buffer and group engine identifiers without per-instance regex/list churn.
- Skip overlapping sensor reads and unchanged numeric labels.
- Limit secondary summaries to one update per second and network discovery to cached intervals.
- Stop sampling when fully hidden/minimized-only or suspended.
- Use static visual assets rather than continuous background animation.

The GPU reading follows Microsoft's [busiest-engine explanation](https://devblogs.microsoft.com/directx/gpus-in-the-task-manager/); unavailable counters show a dash. CPU is system busy time and may differ from Task Manager's clock-normalized utility. Windows/driver counter freshness can be slower than the requested UI interval.

## Limits and reproduction

These are short local observations, not a cross-device performance guarantee, battery study or soak test. Run `benchmark.ps1` on your system, keep other conditions comparable, and exclude runs where the main UI opens. Do not interpret a reduced working set or a smaller RAM percentage as evidence of improved application responsiveness.
