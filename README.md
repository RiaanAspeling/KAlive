# KAlive

A tiny Windows tray utility that keeps you "available" in apps like Microsoft Teams when your laptop has been idle, by injecting a harmless function keystroke after a configurable period of real inactivity.

It only acts when *you* are actually idle — synthetic input is distinguished from real input so the icon (and behavior) reflect the truth.

## Why

Some corporate policies force the screen to sleep after a short idle period, which in turn flips your Teams status to **Away** even if you're at your desk thinking. KAlive sends a random function key (F15–F24, all valid but virtually never mapped) when you've been idle past a threshold, then keeps it up on a randomized cadence until you return.

## Features

- **Two-phase keep-awake**: wait for an initial idle threshold (default 4 minutes) before doing anything, then re-fire every 60–120 seconds (randomized each time) until real input resumes.
- **Random F15–F24 keystroke** via `SendInput` — each fire picks a different key in the range so the cadence isn't mechanically detectable.
- **Smart state detection**: compares the OS's `LASTINPUTINFO` timestamp against the moment we sent our last synthetic key, so real user input is correctly distinguished from our own injections.
- **Tray icon** with three states:
  - **Green** — Watching (you're using the machine)
  - **Amber** — Keeping awake (we're filling in for you)
  - **Grey** — Paused
- **Schedule**: optional Wake / Sleep times (e.g. 06:00 resume, 16:00 pause). Boundary-triggered, so manual Pause/Resume inside the active window sticks. Cross-midnight windows supported.
- **Single instance** enforced via named `Mutex`.
- **Portable settings**: `settings.json` sits next to the exe.
- **Start with Windows** toggle (writes `HKCU\…\Run`).

## Requirements

- Windows 10/11
- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) (or build self-contained — see below)

## Build

```bash
dotnet build
```

Or produce a release exe with the included helper:

```bash
./publish.sh
```

That runs `dotnet publish -c Release -r win-x64 --self-contained false`, producing `bin/Release/net8.0-windows/win-x64/publish/KAlive.exe`. If the target machine doesn't have the .NET 8 desktop runtime installed, change `--self-contained false` to `true`.

## Install

Drop `KAlive.exe` into any folder you can write to (e.g. `%LOCALAPPDATA%\KAlive\` or your Desktop) and double-click. Enable **Start with Windows** from the tray menu if you want it to auto-launch.

## Configuration

Settings live in `settings.json` next to the exe. Edit via the tray (right-click → Settings…) or directly while the app is closed.

| Field | Default | Description |
|---|---|---|
| `Enabled` | `true` | Master on/off — same as the tray Pause toggle |
| `InitialIdleThresholdSeconds` | `240` | Real inactivity required before the first F-key fires |
| `InjectionIntervalMinSeconds` | `60` | Min gap between subsequent fires while keeping awake |
| `InjectionIntervalMaxSeconds` | `120` | Max gap; a fresh random interval in `[Min, Max]` is rolled before each fire |
| `ScheduleEnabled` | `false` | Master toggle for the time-of-day schedule |
| `WakeAt` | `"06:00"` | 24-hour `HH:mm`. Schedule resumes the app at this time |
| `SleepAt` | `"16:00"` | 24-hour `HH:mm`. Schedule pauses the app at this time |

A malformed JSON file falls through to defaults silently, so you can't brick the app with a typo.

## Caveats

- May violate your organisation's acceptable-use policy. Use at your own discretion.
- Synthetic input still counts as "input" everywhere — KAlive can't selectively defeat Teams' away detection while letting other inactivity timers run.

## How it works

`GetLastInputInfo` from `user32.dll` reports the system-wide tick count of the most recent real-or-synthetic input event. Polling it every 5 seconds is enough to know whether to act. After each `SendInput` call, KAlive stamps its own `GetTickCount()` — on subsequent polls, comparing that stamp to `LASTINPUTINFO.dwTime` reveals whether the most recent event was ours (within ~100 ms) or a real keystroke / mouse move that happened later.

Tray icons are drawn at runtime with `System.Drawing` and converted via `Icon.FromHandle` (with `DestroyIcon` cleanup to avoid GDI leaks), so no `.ico` files need to ship.
