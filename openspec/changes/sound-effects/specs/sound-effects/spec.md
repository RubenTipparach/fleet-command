## ADDED Requirements

### Requirement: Every sound effect names its originals and its licence

Every recorded cue SHALL be cut by `tools/audio/cut_cc0.py` from originals fetched by `tools/audio/fetch_cc0.py`, which
SHALL refuse any pack whose own page does not say CC0 or public domain, and `assets/audio/sfx/SOURCES.md`,
`assets/audio/ambience/SOURCES.md` and `AUDIO_SOURCES.md` SHALL name every original, its pack, its maker and its
licence, written from the cue spec so they cannot drift from what was used.

#### Scenario: A pack without CC0
- **WHEN** a pack is added whose page offers only CC-BY
- **THEN** the fetch records it as refused and no cue can be cut from it

#### Scenario: The sources list after a rebuild
- **WHEN** `tools/audio/render_sfx.sh` runs
- **THEN** every cue in `assets/audio/sfx/` has a row in `SOURCES.md` naming each file it is made from, or saying it is
  synthesised by `tools/audio/gen_sfx.py`

### Requirement: Battle sounds play from where they happen

A sound from the battle (a weapon firing, a hit, an explosion, a ship's engines) SHALL play in 3D from its ship or its
named mount node, and SHALL be chosen from the same battle events the view draws its effects from; only the console
keys, the alerts, the ambience beds and the music SHALL be unplaced.

#### Scenario: A Lance fired from the port mount
- **WHEN** the player's ship fires its port Lance
- **THEN** `lance_fire` plays from the port Lance's mount node, and the hit it makes plays `shield_hit` or `hull_hit` as
  the battle's hit event says

#### Scenario: A console key
- **WHEN** the captain presses a helm key on the Concord console
- **THEN** `ui_chirp` plays unplaced on the console bus, at the same level wherever the camera is
