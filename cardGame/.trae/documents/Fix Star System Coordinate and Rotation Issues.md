## Star System Fix Plan

### Core Issues
1. **Coordinate System Conflict**: Y-axis direction mismatch between ItemDataEditor and UGUI
2. **Parent Container Rotation Interference**: StarsContainer rotation causing coordinate calculation errors
3. **Geometry Center Logic Overload**: Unnecessary center offset calculation
4. **Inconsistent Star Positioning**: Manual coordinate calculation conflicting with container rotation

### Implementation Steps

#### 1. Simplify StarsContainer Configuration
**File**: `Assets/Bag/ItemUI.cs`
- Remove `GetShapeCenter()` method and its usage
- Cancel container rotation: Set `starsContainer.localEulerAngles = Vector3.zero`
- Make StarsContainer cover entire ItemUI area
- Remove shape center offset calculations

#### 2. Fix Star Position Calculation
**File**: `Assets/Bag/ItemUI.cs`
- Update `GenerateStarIcons()` method:
  - Iterate original `itemInstance.data.starOffsets`
  - Use `RotateOffset()` to calculate rotated grid position
  - Convert to UI position using formula: `UI_X = (rotatedX + 0.5) * cellSize; UI_Y = -(rotatedY + 0.5) * cellSize`
  - Remove container rotation logic

#### 3. Improve Star Cleanup Logic
**File**: `Assets/Bag/ItemUI.cs`
- Add thorough cleanup at start of `GenerateStarIcons()`:
  - Clear `starImages` list
  - Destroy all child objects of `starsContainer` using direct iteration

#### 4. Verify Adjacency Check Logic
**File**: `Assets/Bag/InventoryGrid.cs`
- Ensure `CheckStarAdjacency()` method correctly:
  - Gets star positions using `item.GetStarPositions()`
  - Checks grid boundaries
  - Verifies neighboring items exist and are not the same as current item

#### 5. Ensure Coordinate System Consistency
**File**: Multiple files
- Verify `RotateOffset()` implementation is consistent across all uses
- Ensure star offsets are correctly rotated based on item rotation
- Confirm UI position calculation uses correct Y-axis conversion

### Expected Results
- Stars display in correct positions matching their grid offsets
- Stars follow item rotation without positional deviation
- Consistent star count between data and UI display
- Improved performance by removing unnecessary calculations
- Simplified code structure for easier maintenance

### Key Changes Summary
- **Removed**: Container rotation, shape center calculation, complex offset logic
- **Added**: Direct UI position calculation, thorough star cleanup, consistent rotation handling
- **Modified**: Star generation workflow, coordinate system conversion

This plan addresses all identified issues by simplifying the star generation logic, removing conflicting rotations, and ensuring consistent coordinate system usage throughout the codebase.