# The traffic log

The timeline shows you *that* a sensor flickered. The **traffic log** — its own tab next to the timeline — shows you everything else the command station said around that moment, in plain words. When a ghost occupancy lines up with a short circuit or a voltage dip, this is where you see it.

Switch to the **Datenverkehr / Traffic log** tab at the top of the workspace. New lines stream in at the top as they arrive, so the most recent traffic is always right under the toolbar.

## It follows the recording

The traffic log is tied to the timeline recording, not to the connection. While you're **not** recording it stays empty — being connected isn't enough. Hit **Start recording** and the log begins capturing everything the station says; hit **Stop recording** and it freezes where it is. Starting a fresh recording clears the previous log, exactly as it clears the timeline, so each recording carries its own self-contained log. And because the two belong together, **saving a session writes the traffic log alongside the timeline**, and **importing a session brings the log back** — open an old session and its log is right there next to the bars. Importing is unavailable while a recording is in progress (stop recording first), so an imported session can never get mixed up with live traffic.

## What's in it

Every line has a timestamp (to the millisecond), a colour-coded **type badge**, and a message:

- **Sensor** — a decoded feedback edge, e.g. `Yard 3 (M1.1) → occupied` then later `→ clear`. This is the textual twin of a bar starting and ending on the timeline; if you gave the sensor a name, it shows the name and the address.
- **System** — the station's telemetry: track current, supply voltage and temperature, e.g. `320 mA · 15.0 V · 32 °C`. If the station reports a fault — **short circuit**, **emergency stop**, **track voltage off**, **power lost** or **over-temperature** — it is appended to the line and the whole row turns red. A short circuit at the same instant a section flickers is a strong hint about what's really going on.
- **Track power** — power switched on or off.
- **Loco** / **Turnout** — locomotive speed/direction changes and turnout switching, when the station broadcasts them. Loco lines only appear when **Capture train data** is switched on in Settings (off by default — see *Connecting to the Z21*); with it off, locos are skipped here just as they are on the timeline.
- **Connection** — connect and disconnect (marked *(simulated)* in demo mode).

## Making it useful

- **Filter by type** — the **Types ▾** dropdown lists every message type with a checkbox; tick the ones you want. It has **Select all** / **Deselect all** buttons too. Hunting a sensor? Deselect all, then tick just **Sensor** and **System**.
- **Search** — type into the search box to keep only lines whose text matches (case-insensitive). Search for a sensor's name to isolate just its activity.
- **Follow** — the view sticks to the newest line (at the top) on its own. Scroll down to study history and it stops jumping around; scroll back to the top and it resumes following.

To keep a log, save the session (the log travels with it) — there's no separate export.

Everything respects the current language and theme: switch to German and the type badges and messages follow; switch to dark mode and the colours recolour with it.
