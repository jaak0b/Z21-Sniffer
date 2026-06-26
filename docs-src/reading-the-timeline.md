# Reading the timeline

The timeline is the heart of the app. It works like a logic analyzer: time runs left to right, and every feedback contact that has reported activity gets its own row.

## Starting and stopping a recording

The timeline only moves while you are recording. The **Start recording** button sits in the top toolbar, right next to Connect/Disconnect, and is always available — recording is a session-wide action, not tied to one tab. Press it and the clock starts, the axis begins to advance, and feedback is captured into rows. Press **Stop recording** and the clock freezes where it stands: every bar that was still on is closed off and marked as ended by the stop, so you can see exactly what was occupied at the moment you stopped.

Each **Start** begins a fresh recording — the previous rows and bars are cleared and the clock resets. Before you ever press Start, the axis stays still; it never ticks away on its own while you're just connected and watching.

Disconnecting does **not** stop a recording. If the link to the station drops mid-run, the timeline keeps going and the gap is captured on the connection row (below) instead.

## Rows and bars

Each row is one R-Bus feedback contact. A filled bar means the contact was reporting *occupied* for that span; a gap means it was clear. A train sitting on a section is one long bar; a **ghost** is a short, stray bar where nothing should have triggered.

Hover any bar for a tooltip showing the sensor's name, its address, and how long it was on — e.g. `Station track 2 (M3.5) · on 0.04 s`. **Zoom in** far enough and that same text is drawn right inside the bar.

## The connection row

While you record, the app also tracks whether it was talking to the command station. A dedicated **Connection** row shows this as a coloured bar: green while connected, red across any stretch where the link was lost. Each segment is labelled with its state and how long it lasted (e.g. `Connected · 142 s`), just like a sensor bar. If a ghost lines up with a red patch, you know the station — not the layout — was the culprit. The row appears the moment you start recording and follows the connection for the rest of the run.

## Scrolling and zooming through history

The whole recording is kept, so you can look as far back as the session goes:

- **Scrollbar** under the timeline — drag it to move back and forth through history.
- **Mouse wheel** over the timeline — zoom in and out around the cursor.
- **Drag** the timeline left/right to pan.
- **Live** button — snap back to following the present moment (scrolling or zooming pauses the live follow; **Live** resumes it). **Pause** freezes the view without losing any recording.

## The legend

The panel on the left is the legend — one entry per sensor, lined up with its row:

- **Rename** a sensor by typing a friendly name straight into its entry (press Enter or click away to keep it). The name is remembered between runs.
- **Reorder** sensors by dragging an entry by its `≡` handle — a ghost follows your cursor and the rows (and their bars) rearrange when you drop. The order is remembered between runs.
- **Remove** a sensor with the **✕** that appears when you hover its entry; you'll be asked to confirm. It disappears from both the legend and the timeline.

With a lot of sensors the legend and bars scroll together, only drawing what's on screen so it stays smooth.

## Hunting a flaky sensor

Set the **Highlight under** threshold in the top toolbar. Any ON-block shorter than that many seconds is drawn with a warning fill and a bright outline — and the moment you change the number, every block re-evaluates. Highlighting is on out of the box; dial the threshold down until only the suspicious blips light up, and you've found your ghost. Set it to **0** to turn highlighting off entirely.

## Light and dark

The **Dark** toggle in the top toolbar switches between a light and a dark (Graphite) theme; the bars, gridlines, axis and chrome all recolour live, and your choice is remembered.
