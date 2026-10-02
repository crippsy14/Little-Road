# 52 — Road network: junctions that join

On 1 October 2026 Richard reported that "the joins on each road are a little off, with space between and road lines etc not matching up". He asked for "a road snapping system... where we can then add T junctions and splits etc? Think SimCity type vibes."

Hillcrest Valley's roads now come from a small road network (`RoadNetwork.cs`), so its junctions are built rather than hand-placed.

## How it works

**The pieces.**
- **Roads** are polylines with a width.
- **Nodes** are the points where roads meet.
- **Arms** are the road ends at a node.
- A road can pass straight through a node (`Through`): the network splits it there into two arms.
- Roads the network doesn't own, such as the Bayview loop, join as **external** arms (`External`). They shape the corners, but the network never rebuilds them.

**Solving a junction (`Solve`).**
1. **Corners.** For each pair of neighbouring arms, the corner is a fillet (6 m radius) tangent to both roads' kerbs. It is found from where the offset kerb lines cross.
2. **Trims.** Each road is cut back to where its corners begin, measured along the road from the node.
3. **Surface.** The node gets one junction surface. It is a polygon through each arm's mouth and around the corners, at the roads' own heights, so there is no step.
4. **Exact mouths.** The mouth corners are the trimmed ribbon's own end vertices, not a recomputed offset. Edges therefore meet exactly, with no notches or hairline gaps.

**External roads.** An external road keeps its own surface. The network adds only the corner pieces either side of the side road's mouth, and tucks the side road 0.4 m under the external road's edge.

## What the builder adds (`HillcrestValley.JunctionSurface`)

- The junction surface, with world-space UVs so the asphalt pattern carries straight across.
- Kerb paint on every corner, 0.28–0.42 m in from the edge, matching the roads' edge lines.
- A gap in the through road's edge line at the mouth.
- Give-way lines across the side road's approach lane, and a give-way sign.

## Junctions in the valley today

| Node | Arms | Notes |
|---|---|---|
| Mainline meets the valley road (109, −905) | 3 | Trims 10.4 / 10.7 / 9.8 m |
| Via Crest meets the valley road (262, −920) | 3 | |
| Station road meets the valley road (395, −936) | 3 | |
| Mainline leaves the Bayview loop (66, −75) | 3 (2 external) | Only the corner pieces are built over the loop |

Each one logs a `LITTLE_ROAD_JUNCTION` line at start-up.

## Adding a junction

```csharp
var net=new RoadNetwork{Fillet=6};
var a=net.Add(junctionPoint);
net.Through("High street",streetLine,8,.02f,a);                    // passes through a
net.Connect("Mill lane",laneLine,7,.02f,a,null,minorStart:true);   // starts at a, gives way
net.Solve();   // then build ribbons from road.trimmed and a surface per node
```

Splits (a Y) and skewed Ts work the same way: the acute side is simply trimmed further. Crossroads (four arms) use the same code but haven't been built or tested yet. Roundabouts would be a new node type.

## Fixed along the way

- **Trims measured from the wrong end.** Trims were measured from the road's end rather than the node, which left the Bayview mainline 3 m short. They are now measured from the node.
- **Duplicate vertex.** When a cut landed exactly on a vertex, it duplicated that vertex. That gave a zero end direction, and every corner was treated as straight.

## Tests

- **Editor checks.** `Editor/RoadNetworkChecks.cs`, run by `MigrationBuild`, covers:
  - a right-angle T: trims 9.5 / 10 m, two 6 m corners, a flat surface, a 70 m side ribbon;
  - a 60° skewed T;
  - a side road off an external road: 7.6 m trim and two corner pieces.
- **World review.** `capture-map.ps1 -Name joins-N -Rendered -Flags @('--spot-views')` captures each junction from the side and from above. joins-5 is the accepted run.
