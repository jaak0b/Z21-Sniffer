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

Locomotives get their own kind of row. Instead of a plain on/off bar, a loco bar carries a **line graph of the speed** over time, drawn right inside it. While a loco is moving you get one bar; the moment its speed drops to zero the bar ends (a falling edge) and the next time it pulls away a fresh bar begins.

- Each bar is **labelled with the loco's identity** — its address with a `Loco` tag, e.g. `Loco 27`, or `Express · Loco 27` once you've named it — so you can tell whose trace you're looking at. Speed and direction aren't repeated there as bare numbers; the graph already shows both.
- **Direction** is shown by which way the graph is oriented. Driving forward, the line sits low at a standstill and climbs toward the top as the loco speeds up. In reverse the whole graph flips — zero is at the top and full speed is at the bottom — so a glance tells you which way the loco was heading.
- Every recorded reading is marked with a small **circle right on the line** where the data point sits, so you can tell where an actual sample was taken from where the line is just holding a value between readings.
- **Hover the line** to read the exact speed at that instant — labelled, e.g. `Speed 67` — along with the direction and the time.
- The speed is scaled against the decoder's own range (14, 28, or 128 speed steps), so a bar that reaches the top means full throttle for that loco.
- Loco bars are **taller** than sensor bars to give the graph room, and they grow as you zoom in and shrink back to the normal bar height as you zoom out.
- Like sensors, each loco is keyed by its address and can be given a friendly **alias** in the legend (type a name into its entry); the name is remembered between runs.
- When a loco bar runs off the **left edge** of the view — because you've scrolled or zoomed into the middle of a long run — the graph doesn't crowd every earlier reading against the edge. Instead the line simply enters at the speed that was in effect at that moment (the last reading before the edge) and carries on, so the start of an off-screen run stays clean rather than smearing into a vertical streak.

## Scrolling and zooming through history

The whole recording is kept, so you can look as far back as the session goes:

- **Scrollbar** under the timeline — drag it to move back and forth through history.
- **Mouse wheel** over the timeline — zoom in and out around the cursor.
- **Drag** the timeline left/right to pan.

Scrolling or zooming pauses the live follow so the view holds still while you study it; drag the scrollbar back to the right-hand edge to snap back to following the present moment. Nothing is lost while you look around — the recording keeps running underneath.

## The legend

The panel on the left is the legend — one entry per row, lined up with its bar:

- Each entry carries a small **type icon** so you can tell at a glance what kind of row it is: linked nodes for the command-station connection, a track contact for a feedback sensor, and a locomotive for a loco. **Hover an entry** for a tooltip that spells out the exact source behind it — which module and contact a sensor decodes to, a loco's address, or that it's the command-station connection.
- **Rename** a sensor by typing a friendly name straight into its entry (press Enter or click away to keep it). The name is remembered between runs.
- **Reorder** sensors by dragging an entry by its `≡` handle — a ghost follows your cursor and the rows (and their bars) rearrange when you drop. The order is remembered between runs.
- **Remove** a sensor with the **✕** that appears when you hover its entry; you'll be asked to confirm. It disappears from both the legend and the timeline.

With a lot of sensors the legend and bars scroll together, only drawing what's on screen so it stays smooth.

## Hunting a flaky sensor

Set the **Highlight under** threshold in the top toolbar. Any ON-block shorter than that many seconds is drawn with a warning fill and a bright outline — and the moment you change the number, every block re-evaluates. Highlighting is on out of the box; dial the threshold down until only the suspicious blips light up, and you've found your ghost. Set it to **0** to turn highlighting off entirely.

## Light and dark

The **Dark** toggle in the top toolbar switches between a light and a dark (Graphite) theme; the bars, gridlines, axis and chrome all recolour live, and your choice is remembered.
