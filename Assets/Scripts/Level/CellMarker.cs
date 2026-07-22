// Codex World — Cell marker
// Attach to an object inside a level to mark what its grid cell means.
// The cell is taken from the object's world position (rounded X/Z), so moving
// the object in the Scene view moves it on the puzzle grid.

using UnityEngine;

public enum CellType { Obstacle, Goal, Spawn }

public class CellMarker : MonoBehaviour
{
    public CellType type = CellType.Obstacle;

    [Tooltip("Only used when Type is Spawn - the direction AXIOM starts facing.")]
    public Facing spawnFacing = Facing.North;

    public Vector2Int Cell =>
        new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.z));
}
