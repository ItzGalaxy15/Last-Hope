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

### Current Balance Strategy (May 2026)
Decision: **Lower overall stats temporarily**. 
By keeping standard node values at their 1x baseline (instead of tripling them to compensate for the `MaxPoints` 3 -> 1 shift), the overall stat inflations are strictly nerfed. Players are forced to branch out to gain power.

**Current 1-Point Baseline (e.g., Warrior):**
- **Haste/Agility:** 1.0% total
- **Base Damage:** 5.0 total
- **Max HP:** 25.0 total
- **Dodge Chance:** 1.0% total
- **Armor Pen:** 1.0% total
- **Block Chance:** 1.0% total

*Note: These baseline values are subject to change. We can scale this up if playtesting shows the game has become too difficult. Layer unlock requirements remain aggressively scaled down (0, 1, 2, 3...) to allow faster late-game ability access without severe stat bloat.*
