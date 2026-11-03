## ADDED Requirements

### Requirement: Krai hulls are cut from blades

A Krai hull SHALL be built from a diamond-section spine lofted through stations of `[z, half beam, top, bottom]` in
metres and flat faceted blades, each blade an outline fanned from a low ridge with a honed bevel band round every edge,
with no curved surface anywhere.

#### Scenario: A blade's edge
- **WHEN** a Krai blade is built
- **THEN** every edge of its outline carries a bevel band, red where the blade's data names the edge red and honed steel elsewhere

### Requirement: Krai colours keep one meaning each

Blood red SHALL appear only on leading edges and the head's war mask, and forge orange only as light (engines, window
slits and the hull's fissures).

#### Scenario: Window slits
- **WHEN** a Krai ship's windows are shown
- **THEN** they are person-sized slits, one row a deck 3.4 m apart, lit forge orange

### Requirement: Krai ships are shown at true scale beside the Concord line

The Krai page SHALL show every Krai class beside the Concord ship of its rank, both built from the shared
`docs/mockups/lib/concord-line.js`, and SHALL show every direction and class as a 40 px silhouette, top and side.

#### Scenario: The line
- **WHEN** the line is shown
- **THEN** the corvette, frigate, cruiser and dreadnought stand beside the Swift, Petrel, Albatross and Condor, with each length measured from the built geometry
