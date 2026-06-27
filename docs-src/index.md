# Z21 Feedback Sniffer

A small Windows tool for hunting down flaky feedback on a model-railway layout.

If you have ever had a track section that reports "occupied" when nothing is sitting on it — a ghost occupancy — you know how maddening it is to catch. The trigger is usually brief and intermittent, so by the time you look at the controller it has already cleared. This app records every R-Bus feedback sensor (Rückmelder) coming out of your Roco/Fleischmann **Z21** command station and draws each contact as a row on a live timeline, with a bar for every span the sensor was on. A sensor that flickers shows up instantly as a cluster of stray little bars on its row, and you can read off exactly when it fired and for how long.

## What it does

- Connects to your Z21 over the network (UDP) — just point it at the controller's IP.
- Listens to all R-Bus feedback traffic and turns it into a logic-analyzer-style timeline.
- Lets you pan, zoom and pause the timeline while it streams.
- Keeps a rich, filterable traffic log — decoded sensor edges, station telemetry (current, voltage, temperature) and fault flags, loco and turnout messages — so nothing is hidden.
- Saves a recording to disk as JSON and loads it back later for further analysis.
- Lets you give friendly names to sensors so "Module 3 / Contact 5" can become "Station track 2".

## The pages

- **Connecting to the Z21** — finding your controller and getting feedback flowing.
- **Reading the timeline** — what the rows and bars mean, and how to spot a ghost.
- **The traffic log** — the decoded, filterable event stream and what each message type tells you.
- **Saving and importing a session** — keeping what you captured and reopening it later.
- **Reading the feedback with Claude** — switching on the built-in MCP server so an AI assistant can query your live recording.

There is also a built-in simulator, so you can try the whole thing out without any hardware connected.

## The window

The app wears its own title bar rather than the standard Windows one: the toolbar row at the very top — with the ⚙ settings, Connect, recording and Session controls — doubles as the title bar. Drag any empty part of it to move the window, and double-click it to maximize or restore. The minimize, maximize/restore and close buttons sit at the far right of that same row.
