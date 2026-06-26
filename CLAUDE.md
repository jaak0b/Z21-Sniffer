# CLAUDE.md

Z21 Feedback Sniffer — an Avalonia (Windows-only) debugging tool that connects to a Roco/Fleischmann **Z21** command station over UDP, listens to R-Bus feedback traffic, and draws every feedback contact (Rückmelder) as a row on a live logic-analyzer timeline. ON periods render as bars over a time axis so intermittent ghost-occupancy is easy to spot: which sensor, when, and for how long.

## Hard Rules

1. **No static methods or properties.** Exceptions: Avalonia `AvaloniaProperty.Register` and framework metadata; and the single `LocalizationService.Instance` singleton, which XAML binds to via `{x:Static}` (the established pattern carried over from Collectary). (The Z21 library's own `Z21BroadcastFlags` constants are external API — referencing them is fine; do not add new statics of our own.)
2. **Localization is resx-only.** All translatable strings live in `Strings.en.resx` / `Strings.de.resx`. Reference via the injected `LocalizationService` in C#; in XAML bind through the localization source. Both language files must have every key — German and English are both first-class.
3. **TDD mandatory, test-first, no exceptions.** For EVERY behavior change incl. bug fixes: commit the test before the production code. Order is non-negotiable: (a) write the test, (b) run it and PASTE the failing output, (c) only then touch production code, (d) re-run to green. A red run you can quote is the gate — no red proof = the fix does not start. If you catch yourself having edited production code first, revert it and restart from (a).
4. **Three test layers per change.** Every feature and bug fix needs unit (`Core.Tests`) + integration (`Infrastructure.Tests`) + headless (`UI.Tests`). "It's only a small change" is not an exemption. Untestable-by-design code (pure XAML, `Render`-only control drawing, generated code) is the only exception, and you must say so explicitly and cover its logic via the pure `TimelineLayout`/ViewModels instead.
5. **Verification gate — scale to the blast radius.** A change is NOT done until verified with real command output quoted (not assumed). What you run locally scales with the change:
   - **Small, localized change** (a few files in one project, no cross-project/DI/file-format surface): run only the **directly relevant fixtures** (`dotnet test … --filter`) and quote totals. Mutation not required. State that you classified it small and which fixtures ran.
   - **Big or multi-project change** (more than one project, or a public API / settings-file / sync format / DI wiring change): run the **local gate** below, output quoted.

   Local gate:
   1. **Changed-area fixtures** — `dotnet test … --filter`, pass/fail totals pasted. A failure there blocks everything. The full suite (`.\build.ps1 --target Test`) and coverage (`.\build.ps1 --target Coverage`, ≥95% line) are the heavier gates — run them before opening a PR, not on every inner-loop edit.
   2. **Diff-scoped mutation, survivors handled.** `.\build.ps1 --target Mutate` diffs against `HEAD` and mutates only your uncommitted changes. Run it **before you commit**. Running full Stryker over the whole codebase is forbidden — far too slow. Stop the running Desktop app first (`Get-Process Z21Sniffer.UI.Desktop | Stop-Process -Force`) — a live instance locks the DLL and fails Stryker's build. Quote the score; kill new survivors with tests or justify each.
   3. **Manual UI verification (for UI changes).** Ask the owner to run the app with exact repro steps (see "Verifying UI Fixes"). Tests do not replace this.

   If a local gate cannot be completed (e.g. a pre-existing failure you did not introduce), STOP and surface it with evidence — do not quietly proceed as if it passed.
6. **No test touches real hardware or the developer's filesystem.** Never require a live Z21 to run tests — wrap the station behind `IFeedbackSource` and fake it (`A.Fake<IFeedbackControl>()` / a scripted `SimulatedFeedbackSource`). File I/O tests use `Path.GetTempPath()` temp dirs, disposed in teardown. The injected `IClock` is faked so the recorder's timestamps are deterministic.
7. **No empty catch blocks.** Log via Serilog and, for owner-initiated operations, surface via the dialog service.
8. **No trademarked words in files.**
9. **NuGet packages: official Microsoft or highly-regarded community only.** No niche/unmaintained single-author packages. Prefer built-in BCL APIs over third-party dependencies. (The `Z21` / `Z21.Autofac` packages are the project's own and are the reason this app exists.)
10. **Every new feature is documented.** Add/update the relevant `docs-src/**` page in the same change. Write conversationally, not in terse machine-speak.
11. **No code comments.** Code self-explains via names and structure, in everything we author (C#, XAML, JSON, `.csproj`). Banned: *what*-narration, divider banners, commented-out code (git is the history), default XML doc-comments. Only allowed: a short non-obvious **why** the code can't express (external-bug workaround, Avalonia/Z21-protocol gotcha). Tempted to write *what*? Rename until the comment is redundant, then delete it. Markdown docs are exempt.
12. **No positional tuple access — code must be refactor-safe.** Never read a tuple by element position (`.Item1`) and never destructure one positionally (`var (a, b) = …`). Every multi-value return is a named `record` / `record struct` read by name, so reordering a member is a compile error, not a silent value swap. A private named-element `ValueTuple` is tolerated only when never destructured positionally — when in doubt, declare a record.
13. **Self-review-and-fix before handoff — multi-file changes only.** Before presenting a change that touches more than one file for commit approval, run a medium-effort `/code-review` scoped to the change and **fix every finding it surfaces** (each fix following the TDD and verification rules) before the change is done. The review's own verify step discards false positives, so any finding that reaches the list is real. Single-file changes are exempt, mirroring the verification buckets in rule #5.
14. **No direct commits to `master` — every change ships as a pull request.** Committing or pushing straight to `master` is FORBIDDEN. Every change goes on its own branch and lands through a PR opened with `gh`. **The PR title must read as a release note** — a single user-facing sentence. Owner review gates everything: never create the branch's commit, push, or open the PR until the owner has explicitly approved the change in chat — present the diff summary and ask, and proceed only after a clear "yes". Once approved: no AI attribution anywhere that touches git or GitHub (no `Co-Authored-By`, no "Generated with" line, no AI author/committer identity). **Commit messages are a short, single sentence** — one line, no body; if a change feels too big for one sentence, split it into smaller commits.
15. **Release notes come from labeled PRs — label every user-facing PR.** Once a release pipeline exists, notes are generated from PR titles via `.github/release.yml`: a PR appears only if it carries `feature` (→ Features) or `fix`/`bug` (→ Fixes). Label any user-facing change accordingly (pairs with rule #14 — the PR title is the release-note sentence); leave CI/chore/docs-only PRs unlabeled.
16. **Every mutation survivor in a file you touch MUST be killed — "it's pre-existing" is NEVER, EVER a valid excuse.** When a mutation run reports a survivor in any file your change touches, you kill it with a test. Full stop. It does not matter that the surviving mutant lives in a method you didn't edit, that it predates your change, that another round introduced it, or that nothing committed it yet — if it survives and it's in your blast radius, you write the test that kills it before the change is done. The ONLY tolerated non-kill is a provably **equivalent mutant** (one that cannot change observable behaviour for any input), and you must spell out *why* it is equivalent in the handoff — "pre-existing", "not mine", "low value", "cosmetic", or "out of scope" are all explicitly forbidden justifications. A surviving non-equivalent mutant means the change is not done.
17. **True polymorphism — generic code knows ONLY `IInterval` and `IIntervalSource`.** Any code that is not specific to one interval type — the `TimelineViewModel`, `BarChartRenderer`/`BarGeometry`, the `IIntervalSourceRegistry`, the generic `LegendRowViewModel`, `WorkspacePersistence`, etc. — MUST operate solely through `IInterval` and `IIntervalSource` (plus generic ports like `IKeyValueStore`). It may NEVER name a concrete interval/source type (`FeedbackSensorInterval`, `FeedbackSensorSource`, `ConnectionSource`, `SensorKey`, …), NEVER `is`/`switch`/cast on type, and NEVER hold type-specific state. All type-specific behaviour lives only in: the concrete source/interval itself, keyed strategies resolved via Autofac `IIndex<Type, …>`, `ViewLocator`/`DataTemplate`-matched views, and classes that are *by definition* type-specific (the feedback ingest, the MCP sniffer API, the summary calculator). Type-specific values (alias/label, order) persist on the source itself (e.g. via an injected `IKeyValueStore`), never bubbled up into generic code or `AppSettings`. This is non-negotiable and compounds rule #12 (no positional tuples is separate; this is the no-type-knowledge rule). Pattern-matching a *nullable* (`x is { } v`) is fine — that is null-checking, not type dispatch.

## Definition of Done — run this before calling any change "finished"

A change is complete **only** when every box is genuinely ticked, with real command output quoted. If you cannot tick a box, the work is not done — say so and stop.

- [ ] **Tests written first** (rule #3) — red output quoted before the production code existed.
- [ ] **All three layers present** (rule #4) — unit + integration + headless, or an explicit note on why a layer doesn't apply.
- [ ] **Tests run, scaled to the change** (rule #5) — changed-area fixtures green (`dotnet test … --filter`), totals quoted, classification stated.
- [ ] **Mutation run scoped to local changes, EVERY survivor in touched files killed** (rules #5 + #16) — *big changes only*; `.\build.ps1 --target Mutate` before committing; Desktop app stopped first; score quoted; every survivor in a touched file killed with a test — the only exception is a provably equivalent mutant, justified in writing. "Pre-existing"/"not mine"/"cosmetic" are forbidden excuses.
- [ ] **Manual UI verification requested** (rule #5) — for any UI change, exact repro steps handed to the owner.
- [ ] **Docs updated** (rule #10).
- [ ] **Localization complete** (rule #2) — every new key in both `Strings.en.resx` and `Strings.de.resx`.
- [ ] **No code comments added** (rule #11) — re-read the diff; only genuine non-obvious *why* notes remain.
- [ ] **Self-review run and every finding fixed** (rule #13) — multi-file change: medium `/code-review` on the diff; every finding fixed. Single-file change: state the exemption.
- [ ] **Owner review obtained** (rule #14) — diff summary presented and explicitly approved before any `git commit`/`push`.
- [ ] **PR labeled for release notes** (rule #15) — user-facing change carries `feature` or `fix`/`bug`.

## Build & Run

```powershell
try { Get-Process -Name "Z21Sniffer.UI.Desktop" | Stop-Process -Force } catch {}
dotnet build "src\Z21Sniffer.UI.Desktop\Z21Sniffer.UI.Desktop.csproj"
.\src\Z21Sniffer.UI.Desktop\bin\Debug\net8.0-windows\Z21Sniffer.UI.Desktop.exe

dotnet test "tests\Z21Sniffer.Core.Tests\Z21Sniffer.Core.Tests.csproj" --filter "FullyQualifiedName~MethodName"
.\build.ps1 --target Mutate    # mutation — scoped to your uncommitted changes since HEAD (full runs forbidden)
.\build.ps1 --target Test      # full suite — run before opening a PR
.\build.ps1 --target Coverage  # ≥95% line-coverage gate — run before opening a PR
.\build.ps1 --target RunDesktop
```

> **Data/log location depends on build config.**
> - **DEBUG** (what you run/debug locally): everything lives next to the build output — `src\Z21Sniffer.UI.Desktop\bin\Debug\net8.0-windows\z21sniffer-data\` → `settings.json`, exported sessions, and `logs\`. This isolates each git worktree. **When diagnosing a local run, read the log here.**
> - **RELEASE:** `%APPDATA%\Z21Sniffer\` → `settings.json`, exported sessions, `logs\`.

No database, no migrations — settings and saved sessions are plain JSON files.

## Project Structure

| Project | TFM | Role |
|---|---|---|
| `Z21Sniffer.Core` | net8.0 | Domain models, ports, use cases, the `FeedbackRecorder` (rising/falling-edge → intervals). No Avalonia, no Z21 dependency. |
| `Z21Sniffer.Infrastructure` | net8.0 | Adapters: `Z21FeedbackSource` (wraps `IZ21CommandStation`/`IFeedbackControl`), `SimulatedFeedbackSource`, JSON session/settings stores, Serilog setup. Depends on `Z21`, `Z21.Autofac`. |
| `Z21Sniffer.Presentation` | net8.0 | ViewModels (CommunityToolkit.Mvvm), `LocalizationService`, pure `TimelineLayout` geometry. No XAML, no Avalonia. Mutation-tested. |
| `Z21Sniffer.UI.Desktop` | net8.0-windows | Avalonia app: XAML views, the custom `FeedbackTimelineControl`, Autofac `UiModule`, `ViewLocator`, theming, entry point. |
| `*.Tests` ×3 | net8.0(-windows) | Unit (Core), Integration (Infrastructure), Headless (UI). |

## Key Patterns

**DI:** Autofac modules. The Z21 station is registered with `builder.AddZ21(t => t.RemoteEndPoint = …)` from `Z21.Autofac`; resolve `IZ21CommandStation`. Serilog is wired with `RegisterSerilog` (`Serilog.Extensions.Autofac.DependencyInjection`) so the Z21 library's `Microsoft.Extensions.Logging` calls flow into Serilog.

**Navigation:** callback-based — child VMs receive `Action`/`Func` at construction; `MainWindowViewModel` drives content. `ViewLocator` maps `XxxViewModel → XxxView` by convention.

**Localization:** injected `LocalizationService` in C#; bound through the localization source in XAML. `Apply(code)` switches language at runtime.

**Recorder:** `FeedbackRecorder` holds the current ON/OFF state per `SensorKey`; each decoded feedback frame opens an interval on a rising edge and closes it on a falling edge, timestamped via the injected `IClock`. Pure, deterministic, the unit-test heart.

**Timeline geometry:** `TimelineLayout` is pure (viewport + intervals → drawable rects + axis ticks), living in Presentation so it is unit- and mutation-testable. The `FeedbackTimelineControl` only consumes its output inside `Render`.

## Z21 Library Specifics (confirmed in the package source)

- `IZ21CommandStation` aggregates every capability: `ICommandStation` (`IsConnected`, `ConnectionChanged`, `ConnectAsync`, `DisconnectAsync`), `ITrackPowerControl` (`TrackPowerOnAsync/OffAsync`, `TrackPowerChanged`), `IFeedbackControl`, plus a raw `Commands` factory and `SendCommandsAsync(params IZ21Command[])`.
- **Feedback:** `IFeedbackControl.FeedbackChanged` is `EventHandler<FeedbackData>`; `FeedbackData(byte GroupIndex, IReadOnlyList<byte> States)` — one status byte per module, one bit per input. **Decode:** module address = `GroupIndex * 10 + stateIndex + 1`; contact = bit `0..7`; occupied = bit set. `RequestFeedbackAsync(groupIndex)` seeds initial state.
- **Endpoint:** `UdpTransportOptions.RemoteEndPoint` is a settable property on a registered singleton (default `192.168.0.111:21105`). To change IP at runtime: mutate it (disconnect first if connected), then `ConnectAsync()`. No child scope needed.
- **Broadcasts:** after connect, send `SetBroadcastFlags` with `Z21BroadcastFlags.RmBusDataChangedMessages` (`0x02`) so feedback is pushed; add `SystemStateDataChangedMessages`/`DriveAndSwitchingMessages` for the raw-traffic log. Built via the `Commands` factory and `SendCommandsAsync`.

## Avalonia 12 Gotchas

- **Dynamic `MenuItem` submenus:** build in code-behind (`CollectionChanged` → hand-built `List<MenuItem>`). XAML `ItemsSource` binding does not render submenus.
- **`Button.Flyout` content declared in XAML never receives input** (`PlatformImpl is null` in the log). Build flyout content in code-behind (`new Flyout()` + content controls).
- **`IsVisible` on a null sub-path** evaluates `true` when the object is null — always add `FallbackValue=False`.
- **Never replace an `ObservableCollection` instance** — mutate in place (`Clear()` + `Add()`).
- **Compiled bindings:** `AvaloniaUseCompiledBindingsByDefault=true`. Every `DataTemplate` needs `x:DataType`.

## Testing

**Conventions:** fixture = `<ClassUnderTest>Test`, method = `MethodName_State_Expected`. One fixture per production class, one file per fixture. NUnit + the constraint model (`Assert.That(x, Is.EqualTo(y))`). FakeItEasy for port interfaces (`A.Fake<IFeedbackSource>()`).

**Core tests:** fake the ports; drive `FeedbackRecorder` with a `FakeClock`.

**Infrastructure tests:** fake `IZ21CommandStation`/`IFeedbackControl`; raise `FeedbackChanged` and assert decoded output. File stores round-trip in `Path.GetTempPath()` temp dirs, disposed in teardown.

**UI tests:** `Avalonia.Headless.NUnit` for ViewModel flows and a render smoke test of the timeline control. Pure `Render` pixels and XAML are the untestable-by-design exception.

## Verifying UI Fixes

Ask the owner to run the app with exact repro steps (connect to the Z21, or use the simulated feedback source, and watch the named sensor row). Do not automate. Tests are required *in addition* to manual verification, never instead.
