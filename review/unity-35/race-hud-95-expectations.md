# Build 95 expectations, before execution

Add a small north-up course map with physical entrant markers, three actual recent lap times and a transient final-lap notice. Preserve build 94's transparent direction, race timing and driving. Route mesh remains static; refresh only markers alongside 10 Hz HUD updates. Player marker differs by shape/size as well as colour. Course and markers share one aspect-preserving projection.

Verify build checks for lap-history duplication/reset/bounds and map orientation/aspect/bounds. Run 24-car Bayview and Cedar against 94 for exact stored behaviour parity. Run three laps at 1280×720 to observe genuine recent laps/final-lap notice and inspect small-window gameplay. Normal-speed 1080p HUD on/off measurements are separate from hidden visual/data runs and do not establish performance acceptance unless budgets pass. No editor or other project simulation during that pair.

Existing frame spikes, connected recovery stall, result-menu contrast/time display, physical controller and broad campaign work remain open. This is a bounded presentation slice; no new driving, economy or AI code is intended.
