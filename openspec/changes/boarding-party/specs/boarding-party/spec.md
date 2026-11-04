## ADDED Requirements

### Requirement: The pads beam marines when nothing is queued

With an empty Capture or Hit & run queue, energizing SHALL beam marines from those aboard, one a pad, and the queue
SHALL show that load darkened.

#### Scenario: Energize all with an empty queue
- **WHEN** the captain presses Energize all on Hit & run with four pads ready, nothing queued and 12 marines aboard
- **THEN** four marines beam over, and the ship has 8 left

### Requirement: Medics heal, hackers hurry and need cover, the pilot flies the prize

A boarding marine SHALL have health that hits reduce, and medics SHALL heal the most wounded marine each volley. Hackers
SHALL shorten the volleys and be hit far more often when fewer than 5 marines cover them. A ship SHALL be taken with an
engineer and a hacker (or the envoy). Until a pilot is aboard, it SHALL hold still, not fight, and show "Pilot needed".

#### Scenario: Taken without a pilot
- **WHEN** a party of marines, a hacker and an engineer takes the Rattlejaw
- **THEN** it strikes its colours, shows "Pilot needed", and gets under way only once a pilot is beamed over
