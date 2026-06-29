# Reading the timeline

The timeline is the heart of the app. It works like a logic analyzer: time runs left to right, and every feedback contact that has reported activity gets its own row.

## Starting and stopping a recording

The timeline only moves while you are recording. The **Start recording** button sits in the top toolbar, right next to Connect/Disconnect, and is always available — recording is a session-wide action, not tied to one tab. Press it and the clock starts, the axis begins to advance, and feedback is captured into rows. Press **Stop recording** and the clock freezes where it stands: every bar that was still on is closed off and marked as ended by the stop, so you can see exactly what was occupied at the moment you stopped.

Each **Start** begins a fresh recording — the previous rows and bars are cleared and the clock resets. Before you ever press Start, the axis stays still; it never ticks away on its own while you're just connected and watching.

Both **Connect** and **Start recording** ask the station for its current state — every feedback contact and the system/track-power state — so a section that was *already* occupied (or a power state that was already set) shows from the very first moment of the recording, instead of staying blank until it next changes. That means it doesn't matter whether you connect first and then start, or start first and then connect: whichever you do second seeds the current picture.

Disconnecting does **not** stop a recording. If the link to the station drops mid-run, the timeline keeps going and the gap is captured on the connection row (below) instead.

## Rows and bars

Each row is one R-Bus feedback contact. A filled bar means the contact was reporting *occupied* for that span; a gap means it was clear. A train sitting on a section is one long bar; a **ghost** is a short, stray bar where nothing should have triggered.

Hover any bar for a tooltip showing the sensor's name, its address, and how long it was on — e.g. `Station track 2 (M3.5) · on 0.04 s`. **Zoom in** far enough and that same text is drawn right inside the bar.

## The connection row

While you record, the app also tracks whether it was talking to the command station. A dedicated **Connection** row shows this as a coloured bar: green while connected, red across any stretch where the link was lost. Each segment is labelled with its state and how long it lasted (e.g. `Connected · 142 s`), just like a sensor bar. If a ghost lines up with a red patch, you know the station — not the layout — was the culprit. The row appears the moment you start recording and follows the connection for the rest of the run.

## The track power row

Right alongside the connection, a dedicated **Track power** row records what the command station was doing with the rails. It works just like the connection row — one continuous bar that changes colour whenever the state changes — but it has four states instead of two, each with its own colour:

- **blue — power on**: the track is live and trains can run.
- **grey — power off**: track voltage is switched off.
- **red — short circuit**: the station reported a short; pair this with a sensor blip and you can see exactly when a derailment or wiring fault cut in.
- **green — programming track**: the station is in CV programming mode.

Each segment is labelled with its state and how long it held (e.g. `Power on · 134 s`, `Short circuit · 0.4 s`). The row appears the moment you start recording and, like the connection row, its legend entry is fixed — you can't rename or remove it.

## Watching locomotive speed

Locomotives get their own kind of row. Instead of a plain on/off bar, a loco bar carries a **line graph of the speed** over time, drawn right inside it. While a loco is moving you get one bar — it keeps going across a change of direction — and the moment its speed drops to zero the bar ends (a falling edge); the next time the loco pulls away a fresh bar begins.

- Each bar is **labelled with the loco's identity** — its address with a `Loco` tag, e.g. `Loco 27`, or `Express · Loco 27` once you've named it — so you can tell whose trace you're looking at. Speed and direction aren't repeated there as bare numbers; the graph already shows both.
- **Direction** reads off a **zero line through the bar**: forward speed is drawn *above* it, reverse speed *below* it. A loco that only ever drives one way uses the whole bar for that direction; once it actually reverses within a run, the zero line sits in the middle with forward above and reverse below, and the reversal shows up as the trace crossing the line. A glance tells you which way the loco was heading, and how fast, at any moment.
- The line is **stepped, not smoothed**: each speed step is held flat until the decoder is commanded to a new one, then the trace jumps straight to it. That mirrors what the loco actually did — no invented in‑between speeds — so a reversal is a clean vertical step down through the zero line and back up the other side.
- Every recorded reading is marked with a small **circle right on the line** where the data point sits, so you can tell where an actual sample was taken from where the line is just holding a value between readings.
- **Hover anywhere across the row** to read the speed at that instant — labelled with its direction, e.g. `Speed 67 · forward`. You don't have to land precisely on the line, and you don't have to find a data point: the reading is the speed the loco was actually holding at the moment under your cursor (stepped, never invented). Keep the cursor still while recording and the value updates on its own as the trace scrolls underneath it.
- The speed is scaled against the decoder's own range (14, 28, or 128 speed steps), so a bar that reaches the top means full throttle for that loco.
- Loco bars are **taller** than sensor bars to give the graph room, and they grow as you zoom in and shrink back to the normal bar height as you zoom out.
- Like sensors, each loco is keyed by its address and can be given a friendly **alias** in the legend (type a name into its entry); the name is remembered between runs.
- When a loco bar runs off the **left edge** of the view — because you've scrolled or zoomed into the middle of a long run — the graph doesn't crowd every earlier reading against the edge. Instead the line simply enters at the speed that was in effect at that moment (the last reading before the edge) and carries on, so the start of an off-screen run stays clean rather than smearing into a vertical streak.

## Watching the system current

The command station reports how much current the booster is drawing, and that gets its own **System current** row — a line graph of the amperage over time, in milliamps. A spike that lines up with a sensor blip or a short on the track-power row is a strong clue to what tripped: an overloaded section, a partial short, a motor stalling.

- The line is **interpolated, not stepped**: current is a continuously varying analog measurement that the station happens to sample at intervals, so the trace runs straight between readings — the honest approximation of a value that really does glide between samples. (This is the opposite of the loco speed graph, which holds each step flat because speed only changes when the decoder is commanded.) Each actual reading is marked with a small circle.
- The graph is **scaled to your station's rated maximum current**, so a trace near the top of the row means the booster is near its limit. On connect the app reads the station's hardware type and looks up its rating — a black Z21 at 3 A, a white z21 / z21start at 2 A, a Z21 XL at 6 A, the Single/Dual boosters at 3 A.
- If you connect a station the app **doesn't recognise** (or one whose rating we don't publish, like SmartRail), it **never invents a ceiling**. Instead that segment scales to its own highest reading — an honest self-scaled graph rather than a made-up limit.
- **Swap stations mid-recording** — disconnect a z21start, reconnect a Z21 XL while still recording — and the row starts a **fresh segment** for the new device, each with its own name and scale, so the two are never blended on one ruler.
- The rating table lives in a plain **`hardware-current.json`** shipped next to the app. It's keyed by hardware id, with a friendly name per device. You can edit a milliamp value if you like — though note an app update ships a fresh copy, so every install always gets the latest table.
- **Hover anywhere across the row** to read the value at that instant. For a recognised station it shows the device, the current, and the rating — `Z21 XL: 900 / 6000 mA`; for an unrecognised one it shows just the current — `900 mA`. As with the loco graph you don't need to be on the line or on a data point — the value is interpolated to the cursor's exact moment and keeps updating live as the trace scrolls under a still cursor.

## Scrolling and zooming through history

The whole recording is kept, so you can look as far back as the session goes:

- **Scrollbar** under the timeline — drag it to move back and forth through history.
- **Mouse wheel** over the timeline — zoom in and out around the cursor.
- **Drag** the timeline left/right to pan.

Scrolling or zooming pauses the live follow so the view holds still while you study it; drag the scrollbar back to the right-hand edge to snap back to following the present moment. Nothing is lost while you look around — the recording keeps running underneath.

## The legend

The panel on the left is the legend — one entry per row, lined up with its bar:

- Each entry carries a small **type icon** so you can tell at a glance what kind of row it is: linked nodes for the command-station connection, a track contact for a feedback sensor, a locomotive for a loco, and a small waveform for the system-current row. **Hover an entry** for a tooltip that spells out the exact source behind it — which module and contact a sensor decodes to, a loco's address, or that it's the command-station connection.
- **Rename** a sensor by typing a friendly name straight into its entry (press Enter or click away to keep it). The name is remembered between runs.
- **Reorder** sensors by dragging an entry by its `≡` handle — a ghost follows your cursor and the rows (and their bars) rearrange when you drop. The order is remembered between runs, and it holds no matter *when* each source first becomes active: drag `M1.8` above `System current`, restart the app, and `M1.8` stays above it even though it only gets a row once its sensor reports again. A source you've never positioned joins next to the last row of its own kind — a new sensor lands below the other sensors rather than at the very bottom — so the list stays grouped by type as it fills in.
- **Remove** a sensor with the **✕** that appears when you hover its entry; you'll be asked to confirm. It disappears from both the legend and the timeline.

With a lot of sensors the legend and bars scroll together, only drawing what's on screen so it stays smooth.

## Showing and hiding rows

When a lot is happening — many locos running, many sections flickering — the timeline can get crowded. The **Filter** button — on the right of the timeline's tab row, where it sits beside the Timeline / Traffic log tabs and shows only on the Timeline tab — opens a tidy, two-level checklist of everything that has a row, so you can hide the noise and keep only what you're watching.

- The top level is the **type** of row — Sensor, Loco, Connection, Track power, System current — each with its type icon and a checkbox. A type with more than one source has a triangle you can click to expand it and reveal the **individual sources** beneath, listed by their label, each with its own checkbox.
- A type's checkbox is **three-state**: ticked when all of its rows are shown, empty when all are hidden, and a dash when only some are. Click it to show or hide the whole type at once.
- **Show all** / **Hide all** flip everything, the **filter** box narrows the list to rows whose name (or type) matches what you type, and each type shows a small **shown / total** count on its right.
- Hiding a row only removes it from view — **the source keeps recording in the background**. Un-hide it and its full history is right there, nothing lost. Visibility is a per-session view preference: it isn't saved, and every fresh **Start recording** brings all rows back.

## Hunting a flaky sensor

The two boxes in the top toolbar bound the highlight. The right box is the **upper** limit ("highlight under"): any block shorter than that many seconds keeps its normal colour but gains a bright red outline — so the label inside stays readable while the short blip still jumps out — and the moment you change the number, every block re-evaluates. The left box is an optional **lower** limit: set it and only blocks *longer* than that value are outlined, so just the band between the two values lights up — handy when you want, say, only the blips between 0.1 s and 0.5 s. The lower box can never be set higher than the upper one; nudging either past the other pulls it back into range. Highlighting works across every row that draws discrete blocks (feedback sensors, the connection, track power), not just sensors; the line-graph rows (loco speed, system current) opt out, since a short segment there isn't a ghost. Highlighting is on out of the box; dial the upper threshold down until only the suspicious blips light up, and you've found your ghost. Set the upper box to **0** to turn highlighting off entirely, or the lower box to **0** for no lower limit.

## Light and dark

The app starts in the dark (Graphite) theme. The **Dark** toggle in the top toolbar switches between the light and dark themes; the bars, gridlines, axis and chrome all recolour live, and your choice is remembered. Every colour comes from the theme palette, so the neutral rows — a loco's speed lane, the system-current band, a powered-off stretch of track power — keep a fill that stands clear of the background in both light and dark, never washing out to invisible.
