#!/bin/sh
# Every recorded engine's scorecard (docs/55): builds the sound harness once, then scores the banks six at a time.
# Usage (Git Bash, project root): sh tools/engine-compare-all.sh [bank ...]
PY="/c/Program Files/Blender Foundation/Blender 5.2/5.2/python/bin/python.exe"
BANKS=${*:-"skyline citroen-c2 golf escort s2000 piazza fabia rs4 viper niro sprint"}
powershell -NoProfile -ExecutionPolicy Bypass -File tools/sound-harness.ps1 -Filter none > /dev/null 2>&1
mkdir -p review/tmp/engine-compare
n=0
for b in $BANKS; do
  "$PY" tools/engine-compare.py $b --prebuilt > review/tmp/engine-compare/$b.txt 2>&1 &
  n=$((n+1)); if [ $n -ge 6 ]; then wait; n=0; fi
done
wait
grep -h "ENGINE_COMPARE\|overall\|Error" review/tmp/engine-compare/*.txt
