I will modify `Assets/CS/Managers/BattleManager.cs` to add a robust fallback mechanism for saving battle rewards.

**Problem:**
The `BattleDataManager` singleton is null during the battle end phase, causing a `NullReferenceException`. This prevents the reward card, gold, and health state from being saved to the permanent `GameDataManager`.

**Solution:**
In `BattleManager.EndBattle`, I will detect if `BattleDataManager.Instance` is null. If it is, I will directly perform the save operations on `GameDataManager.Instance`.

**Detailed Changes:**
1.  **Modify `BattleManager.cs`**:
    *   Inside the `ShowRewardDisplay` callback (around line 991):
        *   Check `if (BattleDataManager.Instance == null)`.
        *   **Fallback Logic**:
            *   Call `GameDataManager.Instance.AddGold(gold)`.
            *   Call `GameDataManager.Instance.AddCard(selectedCard.name)` (using the filename as confirmed).
            *   Sync Hero Health: `GameDataManager.Instance.Health = hero.currentHp`.
            *   Complete Map Node: `GameDataManager.Instance.CompleteNode(GameDataManager.Instance.battleNodeId)`.
            *   Save & Clear: `GameDataManager.Instance.SaveGameData()` and `ClearBattleData()`.
    *   Apply the same fallback logic to the "Auto-select" path (around line 1011) used when UI is missing.

This ensures that regardless of the `BattleDataManager`'s lifecycle state, the player's progress is securely saved to the persistent `GameDataManager`.