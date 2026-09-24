# Following-constrained rejection replay

Previous turn made progress:62 captured the opening correctly, and63's checked target-return experiment was implemented/tested and rejected for worse pace, unresolved paths and cost. Keep63 and60 default-off and hitbox work parked.

Replay every attempted candidate in every captured unresolved opening decision from both the reference62 field and rejected63 field. Do not select only favourable failures. Require exact nominal reproduction of rejection reason, road bound, final route gain/speed and recorded rejected state before accepting counterfactual evidence. Inputs contain2 reference decisions/14 candidates and11 experimental decisions/54 candidates, with no audit drops.

Counterfactual: add the existing shared moving-leader controller and owned observed traffic rollout to the own-car forecast. Keep the same path, physical model,2-second horizon,40Hz control/120Hz physics, road margin, current adjacent corridor and moving side-entry check. No runtime policy change. Compare rejection reasons, whole-body bounds and forward progress. Scenery, live emergency history, repeated planning, opponent future commands and collision impulses are not replayed; success is not live collision certification.

Only a demonstrated improvement in this mismatch justifies a bounded integration test with serial/worker ownership, current-step identity, clean fixture regressions, field pace and costs. Otherwise preserve the result and choose a different causal issue instead of weakening safety or adding destination exceptions.
