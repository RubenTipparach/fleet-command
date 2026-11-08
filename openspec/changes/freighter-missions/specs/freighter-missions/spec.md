## ADDED Requirements

### Requirement: A freighter in your sector offers a mission

When the fleet shares a sector with a freighter that is on her route, the campaign SHALL offer an encounter with her, with no
mission marker needed: an escort if she is your faction's or an ally's, a raid otherwise. The encounter SHALL be offered once a
turn a freighter and SHALL be gone at the end of the turn. Encounters SHALL never be offered at random.

#### Scenario: Escort or raid
- **WHEN** a Concord fleet stands in the sector of a Concord freighter, then of a Krai freighter
- **THEN** the first offers Freighter escort, the second Freighter raid, each naming her

#### Scenario: Gone with her
- **WHEN** the turn ends
- **THEN** no encounter is on offer

### Requirement: A freighter taken is cashed out at a port

A raided freighter that strikes or is boarded SHALL join the fleet with her cargo, stay out of the fleet's battles, and not be
sold as a hull. At a port that deals with you, other than her own faction's, Cash out SHALL pay her cargo's value and take her
from the fleet. A freighter destroyed or taken SHALL be off her route for `rules.freighter_refit_turns` turns.

#### Scenario: Taken and cashed out
- **WHEN** a raid ends with the freighter struck and the escorts gone, and the fleet then reaches its home port
- **THEN** she is in the fleet with her haul, left out of the next battle, and Cash out pays the haul and removes her

### Requirement: Each faction's freighters are its own approved ship

Missions that field freighters SHALL field the faction's own freighter (its battle class and model), and the campaign map
SHALL draw every freighter it shows as that ship with her containers: your own always, anyone's where your ships see.

#### Scenario: The map
- **WHEN** a Krai freighter is in a sector the fleet sees
- **THEN** the map shows the Krai's Spear train with her containers in that sector
