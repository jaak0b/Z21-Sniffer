# Reading the timeline

The timeline is the heart of the app. It works like a logic analyzer: time runs left to right, and every feedback contact that has reported activity gets its own row.

## Rows and bars

Each row is one R-Bus feedback contact. A filled bar means the contact was reporting *occupied* for that span; a gap means it was clear. A train sitting on a section is one long bar; a **ghost** is a short, stray bar where nothing should have triggered.

Hover any bar for a tooltip showing the sensor's name, its address, and how long it was on — e.g. `Station track 2 (M3.5) · on 0.04 s`. **Zoom in** far enough and that same text is drawn right inside the bar.

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
