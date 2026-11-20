# Contributing

## Item System Development Workflow

- Begin with Phase 0: audit the existing Item Definition, Item Instance, Equipment, Weapon Data, Inventory, Character Stats, and Save/Load systems before writing new Item System code.
- Document the current architecture and classify each existing component as: keep, extend, refactor, create new, or retire.
- Implement one roadmap phase at a time. Do not introduce functionality belonging to later phases while completing the current phase.
- Complete and validate the Item system before beginning deeper crafting systems.
- Preserve compatibility with the existing Combat, Weapon Data, Character Stats, and Inventory systems.
- Prefer data-driven configuration, including ScriptableObjects, for values that require tuning.
- Keep changes controlled: use a dedicated branch, small commits, and avoid unrelated system changes.

## Item System Data Rules

- Keep Item Definition data separate from per-item Item Instance data.
- Equipment items use `Item_Instance` only when `UsesInstance` is enabled.
- Equipment item modifications are organized into three primary categories: Base, Affix, and Influ/InfluRoll.
- Base modifiers are flat values fixed by the base type; only their rolled values vary.
- Affix modifiers are percentage-based; their type and roll limits depend on the material.
- Influ provides a special effect implemented by dedicated code. `InfluRoll` is rolled within the Influ limits defined by the material.
- Tier increases with material rarity and determines the applicable roll range limits.
- Do not introduce a separate Unique modifier category; Influ replaces Unique.
- Keep Base, Bonus, and Influence Affix databases independent.
- Equipment Affix Resolver may query, validate, and return compatible affixes, but must not mutate Equipment Instances.
- Preserve Instance Isolation: changing one item instance must not affect another instance or its definition.
- Follow the approved affix handling rules for adding, updating, replacing, compatibility, and slot limits.

## Validation Requirements

Before advancing a roadmap phase, verify compilation, data integrity, instance isolation, independent database access, compatibility filtering, affix handling, persistence behavior, and regression safety for existing gameplay systems.