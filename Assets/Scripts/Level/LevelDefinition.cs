// Codex World — Level definition (scene-authored)
// Sits on each level's root object in the hierarchy. Level text and rules are
// edited here in the Inspector; the puzzle grid itself is read from the child
// objects (tiles under "Tiles", plus CellMarker components for obstacles, the
// goal, and the spawn point). Add, move, or delete objects in the Scene view
// and the puzzle logic follows automatically.

using UnityEngine;

public class LevelDefinition : MonoBehaviour
{
    [Header("Level Info")]
    public string levelName = "New Level";
    public string regionTitle = "Sequence Valley";
    [TextArea] public string story = "";
    [TextArea] public string hint = "";

    [Header("Allowed Commands")]
    public bool allowLoops;
    public bool allowConditionals;

    [Header("Scoring")]
    [Tooltip("Bonus XP is awarded for solving at or below this many command blocks.")]
    public int optimalTokens = 10;

    [Header("Presentation")]
    public Color cameraBackground = new Color(0.07f, 0.12f, 0.09f);

    static Vector2Int CellOf(Transform t) =>
        new Vector2Int(Mathf.RoundToInt(t.position.x), Mathf.RoundToInt(t.position.z));

    // Builds the logical grid the interpreter runs against by scanning children.
    public GridState BuildGridState(out Vector2Int start, out Facing startFacing)
    {
        var state = new GridState();
        start = Vector2Int.zero;
        startFacing = Facing.North;

        // Grid bounds come from the ground tiles (children of the "Tiles" group).
        var tiles = transform.Find("Tiles");
        var boundsSource = tiles != null ? tiles : transform;
        int maxX = 0, maxY = 0;
        foreach (Transform child in boundsSource)
        {
            var cell = CellOf(child);
            if (cell.x > maxX) maxX = cell.x;
            if (cell.y > maxY) maxY = cell.y;
        }
        state.Width = maxX + 1;
        state.Height = maxY + 1;

        foreach (var marker in GetComponentsInChildren<CellMarker>(true))
        {
            switch (marker.type)
            {
                case CellType.Obstacle:
                    state.Obstacles.Add(marker.Cell);
                    break;
                case CellType.Goal:
                    state.GoalPosition = marker.Cell;
                    break;
                case CellType.Spawn:
                    start = marker.Cell;
                    startFacing = marker.spawnFacing;
                    break;
            }
        }

        state.Position = start;
        state.Facing = startFacing;
        return state;
    }
}
