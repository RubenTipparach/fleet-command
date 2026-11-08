# Proposal: field-repair

## Why

The owner, 2026-11-07: "also ships dont auto heal, you have to repair them at stations", then "or use you repair kits".

A ship's hull already carried from battle to battle (`CampShip.HullPct`) and was mended only by Repair at your own port, which
the campaign already had. The supply "Spare parts" was bought by the hull point and never used.

## What changes

- **Nothing mends on its own.** A hull stays as the last battle left it, turn after turn (now pinned by a test).
- **Repair at a port**, as before: the hull's share (`rules.repair_share`, 0.5) of its price for what is missing.
- **Repair kits.** The supply "Spare parts" becomes "Repair kits" (id `parts` kept, so saves load). Use a kit, anywhere
  between battles, and the most damaged ship's hull mends `rules.kit_repair_pct` (25 %), never past whole. Kits come from any
  ship of the fleet, the patched ship's own first.

## Impact

- `data/campaign/campaign.json` (`rules.kit_repair_pct`, the supply's name and line), `Campaign.FieldRepair` and `Kits`,
  `GameFlow.FieldRepair`, the sector panel's Use a kit row. Mockup 1 has no kits: a sync task.
