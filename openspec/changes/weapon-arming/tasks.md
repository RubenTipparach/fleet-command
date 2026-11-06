# Tasks: weapon-arming

- [x] 1.1 Proposal, design and spec delta
- [x] 1.2 Modes as overrides (`ARM_MODES`, `spec`, `canArm`), lance overload, torpedo proximity
- [x] 1.3 Proximity's to-hit and wider block (`hitChance`, `exposure(t, f, reach)`, `damage`)
- [x] 1.4 The arming tabs (panel and details), labels that shrink to fit, the mode in the hit details
- [x] 1.5 The Arming scene and shots; both skins
- [ ] 1.6 The owner's review; A1-A3

## 2. Game (openspec apply, 2026-11-06)

- [x] 2.1 Modes as data (weapons.json `overload` on the lance, `proximity` on the torpedo), `Rules.ModeOf`, `CanArm`; mode changes keep at most half a charge
- [x] 2.2 Proximity's to-hit (bonus, speed ignored), its colour and its wider block carried to the hit
- [x] 2.3 Four arming tabs on the weapons panel and the mount details, a mode none selected has dimmed; the Arming scenes' briefing and script
