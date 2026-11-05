# Tasks: campaign-missions

## 1. Mockup (now)

- [x] 1.1 Proposal, design and spec delta
- [x] 1.2 `docs/mockups/lib/campaign-missions.js`: the registry, the seven contracts and the Shakedown chain as scripts
- [x] 1.3 Accept, mission log, flight along the route, Deploy or Complete, results from the script
- [x] 1.4 New campaign per side with one smallest-tier ship from its home port; saves per faction
- [x] 1.5 Test maps from `data/battle/scenarios.json` in the skirmish
- [x] 1.6 Shots at desktop and phone sizes, looked at; `dom_fit.mjs` passes; published

## 2. After approval for build

- [ ] 2.1 The owner's mission scripts, each a `FCMissions.define` call
- [ ] 2.2 Every scenario in the spec mapped to a test or a playtest beat

## 3. After approval for implementation (a separate request)

- [ ] 3.1 Mission scripts as data or C# behind the same shape, in `src/Core` with tests
- [ ] 3.2 Campaign state, flight and saves in the engine; playtest
