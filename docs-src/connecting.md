# Connecting to the Z21

The app talks to your command station over the network using the Z21 LAN protocol (UDP, port `21105`).

## Pointing it at your controller

The connection details live in **Settings** — click the **⚙** button at the far left of the top bar to open it. Under **Connection**, set the **Source** to **Z21**, then type your controller's IP address into the **Host** box and leave the **Port** at `21105` unless you've changed it. The factory default address is `192.168.0.111`, which is what the app starts with — if your controller is reachable at that address you can close Settings and connect straight away.

Not sure of the address? It's shown in the Z21 maintenance tool, or in your router's list of connected devices. The Z21 and the PC running this app need to be on the same network.

Back on the top bar there's a single connect button next to a small status dot. Press it while disconnected and it reads **Connect**; the app opens the UDP connection, switches on the broadcast flags that make the controller push R-Bus feedback changes, and asks for the current state of the first feedback groups so the timeline starts from a known baseline. The dot turns green and the button now reads **Disconnect** — press it again to close the link. While you're connected the Source, Host and Port in Settings are locked so you can't change them mid-session.

Your last-used host and port are remembered between runs.

## No hardware handy? Use the simulator

Open **Settings**, and under **Connection** set the **Source** to **Simulation**. The Host and Port boxes disappear — there's no controller to point at — and when you connect, the app feeds itself a synthetic stream of feedback instead of talking to a real Z21. Two trains run their own routes: you see occupancy move from one block to the next as they travel, and each train's speed and direction draw out on its loco row, including a stop and a reversal. One sensor off to the side flickers on its own — rarely and unevenly — so you can see what a real ghost occupancy looks like and practise hunting it down without setting up your layout. It's the quickest way to get a feel for the tool.

## Other settings

The Settings window also holds the things you set once and forget. Under **Appearance** there are two dropdowns: one picks the **theme** (Light or Dark) and the other the **language** (English or German). Below that sit the **MCP server** toggle and port covered in *Reading the feedback with Claude*.

Under **Train data** there's a **Capture train data** checkbox, off by default. Tick it and the app records locomotive speed and direction onto the timeline and into the traffic log. Leave it off and locos are skipped entirely — no loco rows, no loco log entries. The warning beneath it spells out the catch: the Z21 only sends loco updates to a *listening* client like this one when no other software is talking to the command station on the same PC at the same time. If you run this app alongside your throttle or layout software, loco capture won't see anything, so the checkbox stays off until you know this is the only program connected.

The whole app wears a compact "Classic" look — tight, lightly-rounded controls, flat cards, and tabs that mark the active page with a thin accent underline. It's designed to stay out of the way so the timeline and the traffic log are what your eye lands on. The dark/light toggle simply repaints that same look, so everything stays consistent whichever theme you run.
