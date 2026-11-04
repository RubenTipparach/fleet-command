## ADDED Requirements

### Requirement: The console wears the skin of the ship you command

In an allied scene the battle console SHALL wear the skin of the faction whose ship the captain commands, and taking
command of another ship SHALL swap the skin with no change to the console's rules, layout or open panels.

#### Scenario: Hopping from the Concord cruiser to the Ssarith stalker
- **WHEN** the captain, on the Resolute in the Allied fleet scene, presses the Fleet key twice
- **THEN** the console is the Krai's on the Kharz, then the Ssarith Hunt's on the Sskarr, each with that ship's own
  officers

### Requirement: A quiet scene for hopping between races

The battle console SHALL offer a Three races scene with one ship each of the Concord, the Krai and the Ssarith under
the captain's command and no enemy within weapons range, and the keys 1, 2 and 3 SHALL take command of the first,
second and third ship of the captain's side.

#### Scenario: Taking the third ship by its key
- **WHEN** the captain, on the Resolute in the Three races scene, presses 3
- **THEN** the captain commands the Sskarr and the console is the Ssarith Hunt's
