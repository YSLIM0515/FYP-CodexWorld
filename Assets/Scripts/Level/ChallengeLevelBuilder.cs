// Codex World — Challenge mode runtime level builder
// Turns a generated ChallengeMap into a real level root the existing game loop
// can play: LevelDefinition on the root, ground tiles under "Tiles", CellMarker
// clones for obstacles/goal/spawn. Visual prototypes (tile, obstacle, goal,
// spawn) are cloned from a scene-authored beginner level so the art style
// matches without any new assets. ChallengeLevelData keeps the map + path tile
// renderers for the hint system's route glow.

using System.Collections.Generic;
using UnityEngine;

public class ChallengeLevelData : MonoBehaviour
{
    public ChallengeMap Map;                                  // runtime only (not serialized)
    public List<Renderer> PathTiles = new List<Renderer>();   // renderers on the optimal route
}

public static class ChallengeLevelBuilder
{
    public static LevelDefinition Build(ChallengeMap map, int index, LevelDefinition template, Transform parent)
    {
        // ---- prototypes from the template level ----
        Transform tileProtoA = null, tileProtoB = null;
        var templateTiles = template.transform.Find("Tiles");
        if (templateTiles != null && templateTiles.childCount > 0)
        {
            tileProtoA = templateTiles.GetChild(0);
            tileProtoB = templateTiles.childCount > 1 ? templateTiles.GetChild(1) : tileProtoA;
        }
        Transform obstacleProto = null, goalProto = null, spawnProto = null;
        foreach (var marker in template.GetComponentsInChildren<CellMarker>(true))
        {
            if (marker.type == CellType.Obstacle && obstacleProto == null) obstacleProto = marker.transform;
            if (marker.type == CellType.Goal && goalProto == null) goalProto = marker.transform;
            if (marker.type == CellType.Spawn && spawnProto == null) spawnProto = marker.transform;
        }
        if (tileProtoA == null || obstacleProto == null || goalProto == null || spawnProto == null)
        {
            Debug.LogError("Codex World: challenge template level is missing tile/obstacle/goal/spawn prototypes.");
            return null;
        }

        // ---- level root ----
        var root = new GameObject($"ChallengeLevel_{index + 1}");
        root.transform.SetParent(parent, false);
        var def = root.AddComponent<LevelDefinition>();
        def.levelName = $"{map.Archetype.ToUpper()} SECTOR";
        def.regionTitle = $"CHALLENGE {index + 1} / 5";
        def.story = $"MAP SEED {map.Seed:X}\nBudget: {map.Budget} commands (optimal {map.OptimalTokens}). A flat sequence needs {map.SequenceTokens} — compress with loops and logic!";
        def.hint = ""; // no tips in challenge mode
        def.allowLoops = true;
        def.allowConditionals = true;
        def.optimalTokens = map.OptimalTokens;
        def.cameraBackground = new Color(0.10f, 0.05f, 0.12f); // corrupted purple

        var data = root.AddComponent<ChallengeLevelData>();
        data.Map = map;

        // ---- ground tiles ----
        var pathCells = new HashSet<Vector2Int>(map.Path);
        var tiles = new GameObject("Tiles");
        tiles.transform.SetParent(root.transform, false);
        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
            {
                var proto = (x + y) % 2 == 0 ? tileProtoA : tileProtoB;
                var tile = Object.Instantiate(proto.gameObject, tiles.transform);
                tile.name = $"Tile_{x}_{y}";
                var pos = proto.position;
                tile.transform.position = new Vector3(x, pos.y, y);
                tile.SetActive(true);
                if (pathCells.Contains(new Vector2Int(x, y)))
                {
                    var r = tile.GetComponentInChildren<Renderer>();
                    if (r != null) data.PathTiles.Add(r);
                }
            }

        // ---- markers (clones carry their CellMarker + visuals) ----
        foreach (var cell in map.Obstacles)
        {
            var ob = Clone(obstacleProto, root.transform, cell, "Obstacle");
            // Give each obstacle a unique crystal so a sector is not a field of
            // identical copies (seed is stable per cell + map).
            var cb = ob.GetComponent<CorruptedBlock>();
            if (cb != null) cb.Randomize(cell.x * 7919 + cell.y * 104729 + index * 613);
        }
        var goal = Clone(goalProto, root.transform, map.Goal, "Goal");
        var spawn = Clone(spawnProto, root.transform, map.Spawn, "Spawn");
        var spawnMarker = spawn.GetComponentInChildren<CellMarker>(true);
        if (spawnMarker != null) spawnMarker.spawnFacing = map.SpawnFacing;

        root.SetActive(false); // GameController activates the current level
        return def;
    }

    static GameObject Clone(Transform proto, Transform parent, Vector2Int cell, string name)
    {
        var go = Object.Instantiate(proto.gameObject, parent);
        go.name = $"{name}_{cell.x}_{cell.y}";
        go.transform.position = new Vector3(cell.x, proto.position.y, cell.y);
        go.SetActive(true);
        return go;
    }
}
