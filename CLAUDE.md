# CLAUDE.md

Guidance for Claude Code sessions working in this repository.

## What this library is

`WpfFastControls` is a WPF class library containing **one** control, `FixedWidthTextBlock`, designed to eliminate layout-pass overhead when displaying frequently-changing fixed-format text (FX prices, counters, timestamps, status codes).

Published as: **github.com/nilez/WpfFixedWidthTextBlock** (git remote `origin`).

## Origin story (context for why this exists)

This control was extracted from a larger WPF FX-pricing dashboard (`TilesPrism`, lives at `C:\_nilesh\_code\java-ract-example\TilesPrism` — not part of this repo). In that dashboard, 12 currency-pair tiles were updating ~10 times per second. Standard `TextBlock` calls `InvalidateMeasure()` on every `Text` change, which propagates up the visual tree causing a full layout pass 120 times/sec. CPU usage was ~17% just for render overhead. Replacing `TextBlock` with `FixedWidthTextBlock` dropped it to ~6%.

## The core idea

- `FixedWidthTextBlock` inherits from `FrameworkElement` (not `Control` — no template overhead)
- `Text` dependency property is registered with `FrameworkPropertyMetadataOptions.AffectsRender` only — **never** `AffectsMeasure`
- Text changes call `InvalidateVisual()` (repaint this element only) — layout tree is never touched
- `MeasureOverride` returns either caller-supplied `FixedWidth`/`FixedHeight` or the size measured on the first pass. After initial layout the element's desired size is frozen.
- `FormattedText` is rebuilt in `OnTextChanged` so `OnRender` is allocation-free per frame

Font properties (`FontSize`, `FontWeight`, `FontFamily`) correctly use `AffectsMeasure` because a font change genuinely alters desired size. `TextAlignment`, `Foreground`, and `Text` all use `AffectsRender` only.

## Repo layout

```
WpfFastControls/
├── WpfFastControls.slnx                           # solution — 3 projects
├── README.md                                      # user-facing docs
├── CLAUDE.md                                      # this file
├── src/
│   └── WpfFastControls/
│       ├── WpfFastControls.csproj                 # net9.0-windows, UseWPF, no deps
│       └── Controls/
│           └── FixedWidthTextBlock.cs             # namespace WpfFastControls.Controls
└── samples/
    ├── Sample.WithTextBlock/                      # control: standard TextBlock
    │   ├── Sample.WithTextBlock.csproj            # WinExe, no project ref
    │   ├── App.xaml(.cs)
    │   └── MainWindow.xaml(.cs)
    └── Sample.WithFixedWidthTextBlock/            # control: FixedWidthTextBlock
        ├── Sample.WithFixedWidthTextBlock.csproj  # WinExe, refs src/WpfFastControls
        ├── App.xaml(.cs)
        └── MainWindow.xaml(.cs)
```

Both samples are **intentionally identical** except for which text control they use, so the user can profile one, then the other, and attribute the delta to the control choice alone. Both build 12 FX-styled tiles in a `UniformGrid` and a `DispatcherTimer` updates them at 100 ms intervals (10 Hz).

## Commands

```bash
# From repo root
dotnet build WpfFastControls.slnx

# Run samples
dotnet run --project samples/Sample.WithTextBlock
dotnet run --project samples/Sample.WithFixedWidthTextBlock
```

Target framework is `net9.0-windows`. Only Microsoft.NET.Sdk is used — no external NuGet packages.

## Open items / likely next requests

1. **Profiler screenshots** — README has a placeholder "Performance Results" section. User plans to profile both samples with VS Diagnostic Tools CPU Usage (or JetBrains dotTrace) and paste the screenshots.
2. **NuGet packaging** — the `.csproj` already has `<Version>1.0.0</Version>` and `<Description>` set. To publish: add `<PackageId>`, `<Authors>`, `<RepositoryUrl>`, `<PackageLicenseExpression>MIT</PackageLicenseExpression>`, then `dotnet pack`.
3. **CI** — no GitHub Actions workflow yet.
4. **Possible future controls** — the original TilesPrism has `PriceElement` (value-typed price renderer) and `IsolatingUniformGrid` (blocks upward layout invalidation from a panel) that were deliberately **left out** of this library per user's instruction. Do not add them back unless explicitly asked.

## What NOT to do

- Don't edit files in `C:\_nilesh\_code\java-ract-example\TilesPrism` — that's a separate repo/project, untouched by design.
- Don't re-add `PriceElement.cs` or `IsolatingUniformGrid.cs`. The user scoped this library down to `FixedWidthTextBlock` only.
- Don't replace `FrameworkElement` with `Control` — the render-only path depends on not having a template.
- Don't add `AffectsMeasure` to the `Text` DP. That would defeat the entire point of the control.

## Style notes

- The single source file `FixedWidthTextBlock.cs` uses regional comment banners (`// ── Section ──`). Preserve that style when editing.
- `Nullable` and `ImplicitUsings` are enabled in every `.csproj`.
- The samples build their UI in code-behind (no MVVM, no XAML bindings) to keep the profiling signal noise-free — don't "improve" them to use bindings unless explicitly asked.
