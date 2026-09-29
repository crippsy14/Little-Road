# First racing AI: archived notes

These are the design documents, research and handoffs of Little Road's first racing AI (the context/forecast driver policy and everything built around it, September 2026). On 29 September Richard decided to design a new racing AI from the ground up, around the six driver abilities in [doc 45](../../45-roadmap-and-feature-spec.md) F2.

- **Status:** these notes are no longer the plan. They're kept as reading, since what worked, what failed and why is useful input for the new design.
- **Links:** their links to review folders and study reports are dead. That evidence was removed in the same tidy-up and is still in git history, up to commit `65d8008`.
- **Code:** the first AI's classes (`ContextDriverPolicy`, `TrafficDriverPolicy`, `ReferenceDriverPolicy` and so on) remain in the code as the benchmark the new AI races against.
