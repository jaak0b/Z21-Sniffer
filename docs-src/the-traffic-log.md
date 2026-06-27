# The traffic log

The timeline shows you *that* a sensor flickered. The **traffic log** — its own tab next to the timeline — shows you everything else the command station said around that moment, in plain words. When a ghost occupancy lines up with a short circuit or a voltage dip, this is where you see it.

Switch to the **Datenverkehr / Traffic log** tab at the top of the workspace. New lines stream in at the bottom as they arrive.

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
- **Follow** — while enabled, the view sticks to the newest line. Scroll up to study history and it stops jumping around; the toggle puts it back.
- **Copy** — select one or more lines and **Copy** puts them on the clipboard (or copies the whole filtered view if nothing is selected).
- **Export** — writes the current, *filtered* log to a text file — handy for attaching to a forum post or keeping next to a saved session.
- **Clear** — empties the log without touching the timeline.

Everything respects the current language and theme: switch to German and the type badges and messages follow; switch to dark mode and the colours recolour with it.
