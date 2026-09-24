# Prototype73: useful mechanism, not an overall pace win

449 build checks pass.73 retains71's open-space score and disables72's integrated tactical estimate. Its optional local speed envelope starts alternate-path speed from equipment capability, applies existing local curvature/backwards braking and steering limits, and retains guide speed at the120m boundary. Guide commands remain exact. The equipment-speed cache is owned per follower and invalidates on power/gearing changes; warm planning allocation is zero. Live/prospective follower parity is checked. Disabled73 exactly reproduces71's recorded motion, events, track/context samples and results.

| Same24-car field |71 behaviour |Local path speed |
|---|---:|---:|
| Combined time |2209.831879s |2235.059052s |
| Preview time/place |90.483315s /7 |87.159126s /4 |
| Preview CP1 rank |22 |22 |
| Preview sustained passes |12 |14 |
| Possible held-behind proxy |3.3998s |2.3000s |
| Impact participant records |10 |6 |
| Peak relative impact |7.627289m/s |6.842210m/s |
| Unresolved context samples |781 |700 |
| Brake sample duty |6.9341% |7.1604% |
| Body departures /physical recovery |0 /0 |0 /0 |
| Whole-AI mean per snapshot, hidden |3.602482ms |3.181553ms |

Both complete24/24 and pass12/12 field checks plus13/13 checkpoint deadlines. There are no runtime errors. However, the predeclared requirement that both preview and aggregate improve fails:car0 gains3.324188s while the field loses25.227173s. The lower measured AI cost accompanies changed driving/workload, not an exact-parity optimisation or rendered-performance acceptance. The80-second preview target is not met. Do not promote, broaden acceptance testing or replace the shortcut based on this run.

The user then proposed a dynamic local racing line per car, using the global reference as guidance and choosing a useful future rejoin point. This aligns with evaluating trajectory shape, speed and exit together rather than further isolated tuning. The implementation/performance proposal is recorded in `docs/37-dynamic-local-racing-trajectories.md`. No such planner has been implemented yet. Preserve71 as the user-liked behavioural reference, with its no-room and frame-time limitations visible.

Evidence: `path-speed-73/comparison.json`, raw off/on reports and analyzers, disabled `motion-parity.json`, `prototype-73-manifest.json`, source archive and predeclared expectations. All new experiments remain opt-in. Desktop55 and adventure checkpoint20 are unchanged.
