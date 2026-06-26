# Saving and importing a session

Once you've captured something interesting, you can keep it and reopen it later.

Both live behind the **Session** button on the top bar — click it and a small menu drops down with **Save session** and **Import session**.

## Save session

**Save session** writes the whole recording — every sensor interval with its exact start and end times — to a `.json` file. This is the lossless format: it holds everything the app knows about the capture, so it's the right choice if you want to archive a tricky case or come back to it later. The file is written wherever you choose in the save dialog.

## Import session

**Import session** does the reverse: pick a `.json` file you saved earlier and the app loads it back onto the timeline, recreating every sensor row and every on-period exactly as it was recorded. It's the way to revisit a captured ghost — or to look at a recording a fellow modeller sent you — without needing the layout connected.
