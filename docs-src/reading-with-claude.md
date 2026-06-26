# Reading the feedback with Claude (MCP server)

The app can expose its live recording to an AI assistant over the **Model Context Protocol**. Once you switch it on, Claude can ask the app what your sensors are doing — list them, pull their on-periods, and get per-sensor statistics — and help you hunt the ghost without you having to read the timeline out loud.

## Turning it on

The server is **off until you ask for it**. Open **Settings** (the **⚙** button at the far left of the top bar); under **MCP server** there's an **Enabled** checkbox with a port next to it (default `8731`). Nothing listens on the network until you tick the box — so Windows never pops up a firewall prompt unless you actually want the server running. Tick it and a green address appears (`http://127.0.0.1:8731`); untick it and the server stops. It only ever listens on `127.0.0.1`, so it's reachable from your PC only.

## Pointing Claude at it

There's a `.mcp.json` in the project root:

```json
{
  "mcpServers": {
    "z21-sniffer": { "type": "http", "url": "http://127.0.0.1:8731" }
  }
}
```

With that in place (and the checkbox ticked in Settings), Claude can call the tools below.

## What Claude can do

**Reading:**

- **get_status** — connected? track power? which host/port, and is the simulator in use?
- **list_sensors** — every contact seen, its friendly name, and whether it's occupied right now.
- **get_intervals** — the recorded on-periods, optionally for one sensor or just the last N seconds.
- **get_summaries** — the ghost-spotter: per sensor, how many times it fired and the total / shortest / longest on-time. A contact with a high count and a tiny *shortest on-time* is almost certainly your flickering sensor.
- **get_recent_events** — the latest raw command-station traffic.

**Control:**

- **connect** / **disconnect** — to the Z21 or the simulator.
- **clear_recording** — start a fresh capture.
- **rename_sensor** — give a contact a friendly name.
- **set_track_power** — switch track power on or off.

## The ghost-hunting workflow

Start a capture (real layout, or set the **Source** to **Simulation** in Settings to try it out), enable the **MCP server** in Settings, then ask Claude to look at `get_summaries`. The stray, ultra-short on-periods jump straight out of the numbers — that's the contact reporting a train that isn't there.
