## ADDED Requirements

### Requirement: Nacelles show their draw and their damage
The warp page SHALL show each nacelle's health in its status colour and the energy it draws, and the energy put into
warp SHALL be split between the nacelles still working, charging the banks in proportion to their health.

#### Scenario: A damaged nacelle
- **WHEN** the port nacelle is at 70 % health and 4 E is put into warp
- **THEN** each nacelle draws 2 E (less under a brownout), the port nacelle shows amber at 70 %, and it charges the banks at 70 % of the starboard's rate

### Requirement: The jump risk combines six factors
The jump risk SHALL combine the risks from nacelle damage, the coolant reserve, the energy in the banks, the course plot,
reactor stability and the quantum-gravitational field, and the jump SHALL roll against the same number the page shows.

#### Scenario: A planned jump
- **WHEN** the banks hold 60 E, the plot is complete, the reactor is stable and the field is weak
- **THEN** the page shows a low jump risk and a jump at that risk takes the ship out of the battle when the roll passes

### Requirement: A strong gravity field needs the safety override
The jump SHALL be refused while the banks hold less than the jump's energy, or while the quantum-gravitational field at
the ship is over the threshold and the safety override is off, with the reason shown.

#### Scenario: Near a neutron star
- **WHEN** the field at the ship is 56 % with a 50 % threshold and the override is off
- **THEN** the Jump key is disabled and the page says "Q-G field: override needed"

### Requirement: A failed jump is a mishap
A failed jump SHALL damage the nacelles, the coolant reserve and the hull in proportion to the risk, drain the banks and
reset the plot, and a failed jump at a very high risk SHALL breach the core.

#### Scenario: Forcing it
- **WHEN** the captain overrides the safeties near the star with no plot and the jump fails at 83 % risk
- **THEN** the warp core breaches and the ship is lost
