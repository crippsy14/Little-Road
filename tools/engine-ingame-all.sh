#!/bin/sh
# Score every game engine's in-game drive (docs/55), four processes at a time.
# Usage (Git Bash, project root): sh tools/engine-ingame-all.sh [--offline[=harness options]]
# With --offline the harness re-renders each engine's captured drive with the current sound code (seconds per engine)
# instead of scoring the capture; without it, the captures themselves (from tools/engine-ingame-capture.py).
PY="/c/Program Files/Blender Foundation/Blender 5.2/5.2/python/bin/python.exe"
MODE=$1
if ! powershell -NoProfile -ExecutionPolicy Bypass -File tools/sound-harness.ps1 -Filter none > review/tmp/harness-build.txt 2>&1 && grep -q "error" review/tmp/harness-build.txt; then :; else grep -i "error CS" review/tmp/harness-build.txt && exit 1; fi
mkdir -p review/tmp/engine-ingame
set -- "engine_stock engine_workshop engine_club engine_sport16 engine_valley" "engine_b_twincam engine_b_highcomp engine_b_v6 engine_a_touring engine_a_evo" "engine_a_works engine_s_v6 engine_s_v8 engine_s_v10" "engine_e_v6 engine_e_v8 engine_e_12h engine_e_economy"
n=0; export NOBUILD=1
for group in "$@"; do n=$((n+1)); "$PY" tools/engine-ingame.py $group $MODE > review/tmp/engine-ingame/part$n.txt 2>&1 & done
wait
cat review/tmp/engine-ingame/part*.txt | grep "in game" | sort -t'|' -k1,1
"$PY" -c "
import re,sys
rows=[l for p in range(1,5) for l in open(f'review/tmp/engine-ingame/part{p}.txt',encoding='utf-8',errors='replace') if 'in game' in l]
vals=[float(re.search(r'in game +([0-9.]+)',l).group(1)) for l in rows]
print(f'ENGINES {len(vals)}  mean {sum(vals)/max(1,len(vals)):.2f}  min {min(vals):.1f}  at or above 8: {sum(v>=8 for v in vals)}')"
