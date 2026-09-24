# Prototype70: light drafting

Drafting is implemented behind `--light-drafting`. Both human and autonomous cars receive a small aerodynamic drag reduction from the same pre-controller snapshot. It changes neither engine power nor tyre grip and never tells a leader to yield. Owned prospective traffic inputs keep nominal, continuation and progress forecasts aware of the effect. The immediate safety/coast envelopes account conservatively for reduced resistance. Full model, approximations and acceptance requirements: `docs/35-light-drafting.md`.

430 build checks pass. The first mechanical assertion mistakenly expected a lasting speed gain after the club car had already reached its34m/s gear ceiling; two failed-attempt logs are retained. The test now measures acceleration before that ceiling and explicitly verifies gearing remains enforced. No production force tuning was changed to satisfy that assertion. The isolated constant-wake test takes1.2083s without tow and1.1083s with tow to accelerate26â€“32m/s.

The two-car Rigidbody experiment independently measures1.25834s without tow and1.15834s with tow over the same speed interval, with identical club loadouts and full-throttle inputs. After one second, speed is30.9577 vs31.3144m/s. The chasing car gains0.545m by the last pre-three-second sample. The leader receives no assistance; there is no contact during the scripted commands. The pull-out fixture loses its measured wake by two seconds. These are bounded physical experiments, not lap benchmarks or proof of exact future traffic prediction.

Disabled70 reproduces enabled69 motion, track/context samples, checkpoints, incidents and race results exactly. The existing controlled suite remains151/152, solely the old blocked-road completion-delay failure. All ten controlled runs have zero contact, sampled departure, physical recovery and runtime errors. New-option metadata is separately checked for all ten because the old analyzer does not check drafting.

| Field |69-equivalent off | Drafting on |
|---|---:|---:|
| Original combined car-seconds |2214.639038 |2223.453552 |
| Original preview time / place |91.733009s /9 |89.216957s /6 |
| Original boundary samples |4 |0 |
| Original impact participant records |16 |16 |
| Original peak relative impact |9.155983m/s |12.034184m/s |
| Original unresolved samples |758 |838 |
| Original braking sample percentage |7.0545% |7.4845% |
| Changed-grid combined car-seconds |2189.761780 |2186.279297 |
| Changed-grid preview time |80.794014s |82.618568s |
| Changed-grid boundary samples |5 |4 |
| Changed-grid impact participant records |10 |10 |

Both versions finish24/24 in both arrangements with no physical recovery. The changed grid moves the five club rivals to different positions while keeping six club/eighteen street entrants; a seed change alone is not an independent challenge for this deterministic controller. The original drafting field passes12/12 existing field checks and13/13 checkpoint deadlines. The changed field retains sampled road-edge excursions, so do not describe it as full physical acceptance.

The original field gets8.814514 combined seconds slower (+0.398%), while the preview improves2.516052s. The changed grid improves3.482483 combined seconds, but the preview slows1.824554s. This supports a functioning small situational tow, not universally faster racecraft. Peak impact severity and extra unresolved/braking samples remain concerns. Existing severity/performance requirements are not relaxed by the narrower drafting screen.

Original hidden mean whole-AI cost rises3.328965 to3.489497ms per snapshot (+4.82%); warm driver/snapshot/worker allocation remains zero. This is accelerated diagnostic timing, not rendering acceptance. Fixed1080p normal-speed on/off runs are being captured separately; no performance acceptance is claimed until their results are recorded.

The build/source manifest freezes the executable and C# inputs. The launcher initially omitted the new physical fixture names; it failed before launching those runs. The updated validation tools are archived separately with hashes, preserving the original build archive. Desktop55 is unchanged.69 remains the user-approved race-feel reference pending viewing of this candidate. Next milestone is individual personalities; keep drafting optional while its field trade-offs are assessed rather than repeatedly tuning strength against one lap.

Evidence: `drafting-70/analysis.json`, raw off/on and physical fixture folders, controlled `regressions/analysis.json` and `new-option-validation.json`, build checks and failed-attempt logs, `prototype-70-manifest.json`, frozen source70. Rendered and changed-grid supplementary analyses are added below when complete.

## Completed validation and viewing

The changed field passes13/13 checkpoint deadlines in both modes but11/12 absolute field checks because both modes have road-edge samples. Maximum body offset improves8.109461m to8.014671m against an8m half-width. It passes the predeclared relative drafting screen; that does not waive the absolute containment failure. Its peak impact rises2.160862 to2.634509m/s and braking duty6.9880% to7.3016%.

The normal-speed pair stays1920x1080 throughout. Off/on frame p95 is27.3094/28.4851ms, p99 is52.5404/55.0351ms; whole-AI mean per snapshot is3.348539/3.502171ms (+4.59%). Both retain zero warm driver/snapshot/worker allocation and zero dropped frame records. Both fail the original frame/AI tail budgets. Drafting adds measured work and is not a performance improvement.

The no-drafting rendered race exactly matches its hidden counterpart. Drafting matches all track/context samples, checkpoints, incident records and results, but raw motion-array parity reports a failure; `terminal-tail-supplement.json` confirms that the first2424 motion records match exactly and the hidden run alone has24 extra records at101.005745s, after the last finish at100.755806s. Keep that raw failure and its supplement rather than silently trimming the export.

The user missed the first drafting viewing. A separate `replay-on` was launched at normal speed/windowed1080p on request implied by that feedback, with Triangle rear view retained. Its viewing judgement remains pending. This replay is not the fixed-resolution comparison used above. The executable is an available opt-in feature candidate; no desktop shortcut or default configuration was changed. Move next to individual driver traits using69's approved behaviour as the reference, keeping the draft option independently switchable and its contact/braking/performance costs visible.


User viewing update: the drafting replay felt good and the user explicitly asked to keep it. Treat70 with drafting as the new race-feel reference, retaining its recorded performance/contact limitations. Before personalities, the user requests a bounded experiment in committing to usable open space ahead.71 tests forward-progress priority in candidate selection; physical limits remain authoritative. Desktop55 remains unchanged.
