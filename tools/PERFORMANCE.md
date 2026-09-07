# Responsiveness changes and regression checks

The pre-change source is backed up at `artifacts/backups/pre-performance-d1045ed.zip`.
`artifacts/backups/pre-performance-d1045ed.bundle` contains the complete Git history
and was verified with `git bundle verify`. Both capture commit
`d1045eddf07843cc0298d71032fc80ad167fb193`; the working tree was clean before editing.
Signing credentials, installed character saves and generated build outputs are not
part of the source ZIP. Existing art is reused without modification.

Changes:

- Remove the startup layer of hidden enemy/background image controls. Native image
  requests now happen when the corresponding screen needs an asset.
- Validate static data on the background initialization task.
- Build combat-area cards incrementally, cancel obsolete navigation and retain
  completed cards when an area is reopened.
- Reuse inventory slots on quantity updates and sorting; batch inventory events.
- Retain Collection Log cards across filtering and discoveries, and refresh pets
  when returning to the screen.
- Index base-item rarity, including equipment upgrades and common/unknown items.
- Cache collection completion counts, invalidating before discovery/restore events
  so shared drops, completion rewards and combat bonuses remain consistent.
- Update skilling progress by scaling the fill instead of requesting new layout.
  Calculate activity recommendations once per refresh and prevent overlapping
  action-completion animations.
- Update text shadows selectively for counters and animation properties.
- Run offline combat in cancellable 16,384-tick background batches, refresh the
  summary at most every 100 ms and avoid fixed delays between batches. Cache
  combat values between level-ups and coalesce reward notifications.

## Native regression run

`PerformanceSmokeTests.cs` is included only when `PerformanceSmokeTests=true`.
The diagnostic build starts its tests instead of the landing screen. It uses fresh
players, never calls `Game.Load`, and never writes the user's save. Do not distribute
a diagnostic build as the playable app.

Windows:

```powershell
dotnet build OSRSIdle/OSRSIdle.csproj -f net10.0-windows10.0.19041.0 -p:WindowsPackageType=None -p:PerformanceSmokeTests=true
```

Launch the resulting `OSRSIdle.exe`. Results are written alongside the executable
as `performance-smoke.txt` and displayed in the test window.

Android:

```powershell
dotnet build OSRSIdle/OSRSIdle.csproj -f net10.0-android -p:PerformanceSmokeTests=true -p:ApplicationId=com.osrsidle.perfsmoke -p:EmbedAssembliesIntoApk=true
```

Use a working local debug keystore for signing. Install the diagnostic APK on an
emulator/device. The separate application ID keeps it independent of the installed
game. Retrieve results with:

```powershell
adb shell run-as com.osrsidle.perfsmoke cat files/performance-smoke.txt
```

The run checks all static item rarities, shared collection discovery and restoration,
native inventory reuse/sorting/empty states, equipment combining, healing, all eight
skilling activities and cancellation, home/skills controls, collection filters,
incremental combat navigation, live combat, respawn state, offline combat, and JSON
save snapshots. Timing/allocation samples describe individual operations; they are
not claims of end-to-end frame-rate improvement on physical devices.

Omit `PerformanceSmokeTests` for normal playable builds. Only Windows and Android
are included in this validation workflow.

## Offline combat benchmark (2026-09-07)

This change was built and tested on Windows x64 only. No Android build, emulator
run or device test was performed. The same unpackaged Release diagnostic and
fresh-player scenario ran 100,000 auto-fight ticks against Chicken before and
after the optimization:

```powershell
dotnet build OSRSIdle/OSRSIdle.csproj -f net10.0-windows10.0.19041.0 -c Release -p:WindowsPackageType=None -p:PerformanceSmokeTests=true -p:AppxPackageSigningEnabled=false
```

| 100,000 offline combat ticks | Before | After | Change |
| --- | ---: | ---: | ---: |
| Core simulation time | 17.55 ms | 8.92 ms | 49.2% less time (1.97x throughput) |
| Managed allocations | 3,500,736 B | 17,296 B | 99.5% lower |

Both runs completed all 100,000 ticks without player death. The final regression
run also passed active cancellation, reward persistence, notification batching
and stat-cache refresh across level-ups. Reports are retained in
`artifacts/windows-offline-before.txt` and `artifacts/windows-offline-after.txt`.
These are single-run in-process measurements of `OfflineCombatSimulation.Advance`;
they exclude UI startup. The interactive path additionally removes the previous
fixed 16 ms delay after each simulation work batch.

## Verified results (2026-09-06)

All 50 assertions plus the completion marker passed on Windows x64 and the running
Android API 36 x86_64 emulator. Reports are retained in
`artifacts/windows-performance-smoke.txt` and `artifacts/android-performance-smoke.txt`.
The isolated Android diagnostic application was removed after testing.

| Operation (100 iterations, 40 inventory stacks) | Windows | Android emulator |
| --- | ---: | ---: |
| Retained refresh | 4.49 ms / 897,640 B | 87.73 ms / 1,279,040 B |
| Reconstruct slots, without native attachment/layout | 1,694.47 ms / 444,513,272 B | 6,292.87 ms / 482,985,072 B |

These are single-run in-process samples, including managed allocations, not a full
before/after application benchmark. The reconstruction comparison exercises the
same slot factory used by the old rebuilding approach and excludes attachment and
layout. Actual frame pacing on physical Android hardware remains unmeasured.

Build status: the normal Windows build passed with no warnings. The Android
diagnostic APK built and ran successfully, with three existing nullable warnings
in `FrameRateMonitor.cs`. Additional attempts to rebuild the normal Android app
stalled in local tooling before emitting build output, including an unsandboxed
attempt; those attempts were cancelled. A normal Android package is therefore not
part of the verified deliverables. Production signing settings were not changed.
