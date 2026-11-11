# Proposal: commander-rank

## Why

Owner, 2026-11-09: "Lets introduce the concept of commander leveling up. The higher level your commander is the more
difficult general missions get. It seems like you can easily be overwhelmed by enemy ships in your tiny frigate."

Today a general mission's strength is rolled evenly from Light, Moderate and Heavy (`Campaign.MakeOffer`), which sets how
many enemy ships it brings (`"count": "strength"`: 1, 2 or 3), and each enemy group alternates tier 1 and tier 2 hulls
(`MissionRun`, `1 + i % 2`). So a new captain in a tier 1 frigate meets three ships, one in three times, and destroyers in
half of them. The mission sweeps showed it: the Concord lost belt patrol, nebula sweep, orbital patrol and patrol at the
start (`docs/playtests/2026-11-08-mission-fixes.md`).

## What changes

- The captain has a **rank**, earned from **career prestige** (every point of prestige gained, never lost), shown on the
  campaign map's top bar and announced in the debrief when it rises.
- A general mission's **strength** is rolled from weights set by the rank: a new captain sees mostly Light missions, a senior
  one mostly Heavy.
- Enemy hulls in a general mission are capped at a **tier ceiling** set by the rank, so a new captain meets frigates.
- Rewards grow with strength, so the harder missions of a senior captain pay more.
- Story missions keep their own fixed forces.

## Not in this change

Rank gating what you may buy (ships by rank), officer ranks, and rank titles per faction culture (an open question: the
titles below are provisional).
