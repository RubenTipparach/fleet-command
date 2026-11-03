## ADDED Requirements

### Requirement: A faction's battle console is the Concord's console in another skin

A faction's battle console SHALL run the same rules, panels and controls as the Concord's, differing only in the skin:
its kit pieces, colour roles, ink, type, the shape of the keys it draws and the names of the player's side. Every
control the Concord shows SHALL exist in the faction's console and do the same thing.

#### Scenario: Every control behaves the same in the Krai Warhost console
- **WHEN** each control of each named shot is pressed in the Concord console and in the Krai Warhost console, from the same replayed battle
- **THEN** the console and the battle are in the same state a second later, press for press (tools/mockups/skin_parity.mjs)

#### Scenario: The Concord is unchanged by its skin
- **WHEN** the Concord console is drawn through the skin fields
- **THEN** every named shot is pixel-identical to the console drawn before the skin fields existed, apart from the version label and the parts that animate with the clock

### Requirement: The Krai Warhost console is one cast console

The Krai Warhost console SHALL draw its column on one rendered backplate, clamp each stacked panel to the next with
rendered bridges, brace the column to the strips with rendered gussets, and set every label and panel title on a
rendered metal placard. Every Krai piece with material and depth SHALL be rendered in Blender from fit-checked shapes.

#### Scenario: A label on the Krai console
- **WHEN** the Krai console draws a label (not a value)
- **THEN** the label sits on the rendered label placard, its pip ahead of the letters

#### Scenario: The Krai kit is rendered
- **WHEN** tools/mockups/krai_shapes.py --verify assets/ui/krai runs
- **THEN** every shaped piece passes rule B with no lit pixel outside its plate
