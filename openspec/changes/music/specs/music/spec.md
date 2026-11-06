## ADDED Requirements

### Requirement: Every cue is built from the written motifs

Every music cue SHALL be a Strudel score in `music/` that carries the MOTIF LIBRARY block of `music/motifs.strudel`
word for word (checked by `tools/music/check_motifs.mjs`), and every motif SHALL be written out in
`docs/design/music.md` with its notes, its chords and where it plays.

#### Scenario: A motif changes
- **WHEN** the Concord's notes change in `music/motifs.strudel`
- **THEN** `tools/music/check_motifs.mjs` fails on every score still carrying the old notes until each is updated

### Requirement: The music follows the battle

The game SHALL play the Main Title in the menus, the bridge bed alone while no enemy is in contact, Battle Stations as a
seamless loop from contact, Victory when the battle is won and Ship Lost when a ship of the fleet is lost, crossfading
between them, with contact decided by the same rule the console's alerts use.

#### Scenario: Contact
- **WHEN** an enemy comes within its longest weapon's range of a ship in the fleet
- **THEN** Battle Stations fades in over 2 s and loops at 43.64 s with no gap

#### Scenario: The last enemy destroyed
- **WHEN** the battle is won
- **THEN** the loop fades over 1 s and Victory plays once
