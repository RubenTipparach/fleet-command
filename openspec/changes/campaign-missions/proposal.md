# Proposal: campaign-missions

## Why

The owner, 2026-11-05: "clicking on it starts a new campaign with the smallest tier ship and you can fly around the galaxy map
performing missions. Mockup campaign systems first. Then I'll get you some mission scripts to implement in our three js mockup
env."

The game flow mockup (mockup 1) had seven mission kinds hard-wired into the page, a two-ship Concord fleet to start, and a
fleet that jumped from hex to hex. Mission scripts from the owner need a place to go that does not mean editing the page.

## What changes

- **Missions are scripts.** One registry (`docs/mockups/lib/campaign-missions.js`) and one shape for every mission: where
  it may be offered, its briefing and objective, what it needs, its battle (or none), and its result (credits, standing, a
  prize, the missions it offers next, a line for the log). The seven contracts become scripts of that shape; a two-step
  story chain (Shakedown cruise, then First blood) shows a mission offering the next.
- **Accept, fly, deploy.** A briefing is accepted into a mission log; the fleet flies there; in the sector, Deploy starts the
  battle, or Complete finishes a mission that has none.
- **A new campaign starts small.** One ship of the side's smallest tier from its home port: the Concord's Swift frigate from
  Halcyon, the Krai corvette from Vahr, a stand-in Hunt corvette from Calder.
- **The fleet flies.** Its miniature lifts off its stand and flies the route sector by sector, turning to each leg.
- **Factions.** The Krai take the Kethran's place as the Concord's rival, as in the game; the Ssarith Hunt has its own
  space in the north.

## Impact

- Mockup 1's campaign. New capability `campaign-missions`; the mission requirements in `game-flow` are restated here as
  scripts. Game code waits for approval (CLAUDE.md section 4).
