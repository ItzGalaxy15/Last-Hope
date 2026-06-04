# Skill Tree Balancing Notes

## Warrior Original Values (Max Points 3 -> Max Points 1 Transition)
When we change nodes from 3 MaxPoints to 1 MaxPoint, we will need to rebalance the total output.
Here are the original node values for reference:

- **Haste/Agility:** 1.0% per point (3.0% max)
- **Base Damage:** 5.0 flat damage per point (15.0 max)
- **Max HP:** 25.0 flat HP per point (75.0 max)
- **Dodge Chance:** 1.0% per point (3.0% max)
- **Armor Pen:** 1.0% per point (3.0% max)
- **Block Chance:** 1.0% per point (3.0% max)

### Layer Unlocks
- Layer 0: 0 points
- Layer 1: 3 points
- Layer 2: 6 points
- Layer 3: 10 points
- Layer 4: 15 points
- Layer 5: 20 points

### Current Balance Strategy (June 2026)
Decision: **Scale Warrior Upwards Appropriately per Stance**. 
We've aggressively buffed Warrior's baseline damage and specifically targeted each skill branch to provide a very distinct identity.

**Updated 1-Point Branch Scalings (Warrior):**

- **Left Wing (Dual Wield/Agility)** 
  - Goal: Higher damage, much faster attack speed.
  - Scale: Gain 0.1 to 0.2 `CurrentHaste` reduction (massive speed) and up to 40 Flat Damage.

- **Middle Wing (Axe/Bruiser)** 
  - Goal: Highest damage, slower attack speed.
  - Scale: Gain 30 to 70 Flat Damage, while letting the innate Heavy Axe Stance lower attack speed gracefully.

- **Right Wing (Shield/Tank)** 
  - Goal: High resistance, great regen, tiny bit of damage increase.
  - Scale: Keep existing proc block-healing effects, gain up to 125 Max HP, and a tiny 15 Flat Damage to keep clearing pace alive.

*Note: The C# backend integration (`Player/Warrior.cs`) was updated to map Effect Values accurately like the Archer implementation. Values scale directly from JSON bindings instead of hardcoded int iterators.*
