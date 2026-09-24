# Capability smoke

27/27 valid physical runs; three repeats each. Recorded completion-time ranges are zero within this build/environment. This is not proof of cross-machine determinism or the full calibration gate.

| Course | Loadout | Time (s) | 0–30 mph (s) | Brake distance (m) | Force-model prediction (m) |
|---|---|---:|---:|---:|---:|
|straight|stock|40.400|2.698|—|—|
|straight|street|37.792|1.830|—|—|
|straight|club|28.342|1.283|—|—|
|brake|stock|1.583|—|15.335|15.335|
|brake|street|1.250|—|12.201|12.201|
|brake|club|0.950|—|9.239|9.239|
|uturn|stock|23.617|2.698|—|—|
|uturn|street|20.692|1.830|—|—|
|uturn|club|16.833|1.283|—|—|

Acceleration predictions differ by less than 1% at the measured 0–30 mph threshold. Brake entry is 20 m/s; prediction excludes turning. Held-out entry speeds, mixed equipment and combined cornering remain required before calling the capability envelope calibrated.
