# Risk45: faster field, not accepted as the default

23 September 2026. Build45 passes311 checks, including exact removal of added gap candidates without altering original risk5 scores or hard side constraints. The comparison uses `--driver-risk 5 --no-gap-centres`; default risk0 still exactly reproduces43. No desktop shortcut or production build was replaced.

| Evidence | Risk0 | Risk5, soft preference only |
| --- | ---: | ---: |
| Finishers | 24/24 | 24/24 |
| Combined first-lap time | 2237.225189s | 2195.035492s |
| Car0 checkpoint1 | 24th | 19th |
| Car0 finish | 10th | 7th |
| Pair contact events | 5 | 22 |
| Maximum recorded relative impact speed | 4.9072m/s | 8.5559m/s |
| Sampled body departures | 0 | 0 |
| Assisted / physical recoveries | 0 / 0 | 0 / 0 |
| Active unresolved path samples | 138 | 193 |

All existing field and checkpoint-throughput gates pass. Combined time improves42.189697s, about1.89%. Contact frequency and severity exceed the declared review threshold. All22 contacts occur before either involved car finishes, so post-finish traffic does not explain them. Many are light early side contacts, but the later8.5559m/s event is not dismissed. Risk5 is a requested behavioural target, not an accepted personality calibration from this experiment.

## Controlled regression evidence

All10 controlled runs exit normally, finish, and have no contact, sampled road departure or recovery. The existing evaluator passes49/52 checks. Failures are retained:

- Queue: car0 passes the first slower car on the straight, but the second sustained pass is confirmed at12.3666s outside the required straight region. It finishes27.8580s versus26.9580s for the frozen reference.
- Narrow-road case: car0 finishes36.2589s versus29.7580s, exceeding the completion allowance. The independent no-room corridor contract still passes; staying behind inside the narrow corridor is appropriate, but the wider-case completion regression remains.
- Braking leader: car0 finishes29.8746s versus27.0247s, exceeding the allowance. It still passes the leader and avoids contact.

Both rear-merge sides satisfy the independent witness deadlines. The corner case finishes cleanly and earns ordered gates. Clear solo trials keep their previous times, so the field gain is not a hidden power increase. This is not rendered-frame performance evidence: the latest measured visual smoothness remains the earlier41 review, and the AI CPU/startup misses remain open.

## Next implementation target

Preserve the original risk0 comparison and the faster45 candidate. Add the observed fast outside-car/opponent crossing encounter from44 as a controlled regression, then evaluate the whole passing approach against moving opponent occupancy. A present gap and an own-car road-contained forecast do not prove the path through that gap. Include steering/body rotation and an explicit response when the previous path becomes invalid; merely retaining the old crossing path is not sufficient.

Separately inspect preference selection against the downstream leader envelope in the three delayed controlled cases. Reducing the soft margin can choose a target that remains constrained by following, so smaller margins do not guarantee a quicker overtake. Evaluate actual progress and pass completion, rather than only lateral gap width or number of candidate paths. Do not weaken the three failed checks or restore blanket caution to disguise the problem.

The wider campaign scope remains unchanged. Mixed driver identities, track learning, pre-race loadouts/personality UI and adventure integration follow a racecraft milestone that passes its functional and moving review checks. This experiment does not complete that milestone or the overall goal.
