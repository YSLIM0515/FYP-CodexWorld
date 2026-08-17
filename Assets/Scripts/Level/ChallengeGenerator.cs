// Codex World — Challenge mode map generator 

using System.Collections.Generic;
using UnityEngine;

public class ChallengeMap
{
    public int Width, Height;
    public Vector2Int Spawn, Goal;
    public Facing SpawnFacing;
    public HashSet<Vector2Int> Obstacles = new HashSet<Vector2Int>();
    public List<Vector2Int> Path = new List<Vector2Int>();   // optimal route cells (for the glow hint)
    public List<CommandToken> Solution = new List<CommandToken>(); // compressed optimal program
    public List<CommandType> Flat = new List<CommandType>();  // the same route before compression
    public int OptimalTokens;   // compressed token count (3-star target)
    public int SequenceTokens;  // flat move/turn token count (must exceed Budget)
    public int Budget;          // hard command limit per run
    public string Archetype;
    public int Seed;
}

public static class ChallengeGenerator
{
    const int BudgetSlack = 2;      // Budget = OptimalTokens + slack
    const int MinCompressionGain = 3; // SequenceTokens - OptimalTokens must reach this

    public static ChallengeMap[] GenerateRun(int seed)
    {
        var rng = new System.Random(seed);
        var archetypes = new List<string> { "Serpentine", "Spiral", "Minefield", "Maze", "Gauntlet" };
        // Shuffle so every run has a different map order.
        for (int i = archetypes.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (archetypes[i], archetypes[j]) = (archetypes[j], archetypes[i]);
        }

        var maps = new ChallengeMap[archetypes.Count];
        for (int i = 0; i < archetypes.Count; i++)
            maps[i] = GenerateMap(archetypes[i], rng.Next(), seed);
        return maps;
    }

    static ChallengeMap GenerateMap(string archetype, int mapSeed, int runSeed)
    {
        ChallengeMap best = null;
        // Retry with fresh sub-seeds until the dual-optimal rule holds; keep the
        // most compression-demanding candidate as a fallback so we never hang.
        for (int attempt = 0; attempt < 60; attempt++)
        {
            var rng = new System.Random(mapSeed + attempt * 7919);
            var map = BuildArchetype(archetype, rng);
            if (map == null) continue;
            map.Seed = runSeed;
            map.Archetype = archetype;
            if (!Solve(map)) continue;
            if (best == null || (map.SequenceTokens - map.OptimalTokens) > (best.SequenceTokens - best.OptimalTokens))
                best = map;
            if (map.SequenceTokens - map.OptimalTokens >= MinCompressionGain) return map;
        }
        return best; // relaxed fallback — still solvable, just less compression gain
    }


    static ChallengeMap BuildArchetype(string archetype, System.Random rng)
    {
        switch (archetype)
        {
            case "Serpentine": return Serpentine(rng);
            case "Spiral": return Spiral(rng);
            case "Minefield": return Minefield(rng);
            case "Maze": return Maze(rng);
            default: return Gauntlet(rng);
        }
    }

    // Boustrophedon corridors: wall rows with alternating end gaps force S-bends.
    static ChallengeMap Serpentine(System.Random rng)
    {
        int w = 9 + rng.Next(3), h = 7 + (rng.Next(2) * 2); // odd corridor count
        var m = new ChallengeMap { Width = w, Height = h, Spawn = new Vector2Int(0, 0), SpawnFacing = Facing.East };
        bool gapRight = true;
        for (int y = 1; y < h; y += 2)
        {
            int gap = gapRight ? w - 1 : 0;
            for (int x = 0; x < w; x++)
                if (x != gap) m.Obstacles.Add(new Vector2Int(x, y));
            gapRight = !gapRight;
        }
        int lastRow = h % 2 == 1 ? h - 1 : h - 2;
        m.Goal = new Vector2Int(gapRight ? w - 1 : 0, lastRow);
        return Finalize(m);
    }

    static ChallengeMap Spiral(System.Random rng)
    {
        int n = 9 + (rng.Next(2) * 2); // 9 or 11, square
        bool mirrored = rng.Next(2) == 0; // half the runs spiral the other way
        var m = new ChallengeMap
        {
            Width = n,
            Height = n,
            Spawn = new Vector2Int(0, 0),
            SpawnFacing = mirrored ? Facing.North : Facing.East
        };
        var corridor = new HashSet<Vector2Int> { new Vector2Int(0, 0) };
        var dirs = new[] { new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(-1, 0), new Vector2Int(0, -1) };
        var pos = new Vector2Int(0, 0);
        int len = n - 1, d = 0;
        while (len > 0)
        {
            for (int rep = 0; rep < 2 && len > 0; rep++)
            {
                for (int i = 0; i < len; i++) { pos += dirs[d & 3]; corridor.Add(pos); }
                d++;
            }
            len -= 2;
        }
        m.Goal = pos; // spiral center
        for (int x = 0; x < n; x++)
            for (int y = 0; y < n; y++)
            {
                var cell = new Vector2Int(x, y);
                var check = mirrored ? new Vector2Int(y, x) : cell;
                if (!corridor.Contains(check)) m.Obstacles.Add(cell);
            }
        if (mirrored) m.Goal = new Vector2Int(m.Goal.y, m.Goal.x);
        return Finalize(m);
    }

    // Open field, obstacles scattered on and around the diagonal ideal line.
    static ChallengeMap Minefield(System.Random rng)
    {
        int w = 9 + rng.Next(3), h = 9 + rng.Next(3);
        var m = new ChallengeMap { Width = w, Height = h, Spawn = new Vector2Int(0, 0), SpawnFacing = Facing.North, Goal = new Vector2Int(w - 1, h - 1) };
        int count = (int)(w * h * (0.14f + (float)rng.NextDouble() * 0.06f));
        for (int i = 0; i < count; i++)
            m.Obstacles.Add(new Vector2Int(rng.Next(w), rng.Next(h)));
        return Finalize(m);
    }

    // Dense random walls
    static ChallengeMap Maze(System.Random rng)
    {
        int w = 9 + rng.Next(3), h = 9 + rng.Next(3);
        var m = new ChallengeMap { Width = w, Height = h, Spawn = new Vector2Int(0, 0), SpawnFacing = Facing.North, Goal = new Vector2Int(w - 1, h - 1) };
        int count = (int)(w * h * (0.26f + (float)rng.NextDouble() * 0.08f));
        for (int i = 0; i < count; i++)
            m.Obstacles.Add(new Vector2Int(rng.Next(w), rng.Next(h)));
        return Finalize(m);
    }

    // Left half serpentine walls, right half minefield — everything combined.
    static ChallengeMap Gauntlet(System.Random rng)
    {
        int w = 11, h = 9;
        var m = new ChallengeMap { Width = w, Height = h, Spawn = new Vector2Int(0, 0), SpawnFacing = Facing.North, Goal = new Vector2Int(w - 1, h - 1) };
        int half = w / 2;
        bool gapTop = true;
        for (int x = 1; x < half; x += 2)
        {
            int gap = gapTop ? h - 1 : 0;
            for (int y = 0; y < h; y++)
                if (y != gap) m.Obstacles.Add(new Vector2Int(x, y));
            gapTop = !gapTop;
        }
        int count = (int)(half * h * 0.16f);
        for (int i = 0; i < count; i++)
            m.Obstacles.Add(new Vector2Int(half + rng.Next(w - half), rng.Next(h)));
        return Finalize(m);
    }

    static ChallengeMap Finalize(ChallengeMap m)
    {
        m.Obstacles.Remove(m.Spawn);
        m.Obstacles.Remove(m.Goal);
        return m;
    }


    static readonly Vector2Int[] Dirs = { new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(0, -1), new Vector2Int(-1, 0) };

    // Fills Path / Solution / token counts. False when unreachable.
    // The three actions cost one command each, so BFS stays optimal whatever order
    // they are expanded in - but the *shape* of the route it finds changes, and a
    // more regular route compresses into fewer loop tokens. Trying every order and
    // keeping the best compression costs six cheap searches on an 11x11 grid.
    // 0 = move forward, 1 = turn left, 2 = turn right.
    static readonly int[][] ExpandOrders =
    {
        new[] { 0, 1, 2 }, new[] { 0, 2, 1 }, new[] { 1, 0, 2 },
        new[] { 1, 2, 0 }, new[] { 2, 0, 1 }, new[] { 2, 1, 0 },
    };

    // Generation keeps the original expansion order, so generated maps are unchanged.
    static bool Solve(ChallengeMap m) => Solve(m, 0);

    static bool Solve(ChallengeMap m, int order)
    {
        int W = m.Width, H = m.Height;
        int States = W * H * 4;
        int Idx(Vector2Int p, int f) => (p.y * W + p.x) * 4 + f;

        var prev = new int[States];
        var prevCmd = new CommandType[States];
        for (int i = 0; i < States; i++) prev[i] = -1;

        int startIdx = Idx(m.Spawn, (int)m.SpawnFacing);
        prev[startIdx] = startIdx;
        var q = new Queue<(Vector2Int p, int f)>();
        q.Enqueue((m.Spawn, (int)m.SpawnFacing));
        int goalIdx = -1;

        while (q.Count > 0 && goalIdx < 0)
        {
            var (p, f) = q.Dequeue();
            int cur = Idx(p, f);
            bool reachedGoal = false;
            foreach (int act in ExpandOrders[order])
            {
                if (act == 0) // move forward
                {
                    var np = p + Dirs[f];
                    if (np.x >= 0 && np.x < W && np.y >= 0 && np.y < H && !m.Obstacles.Contains(np) && prev[Idx(np, f)] < 0)
                    {
                        prev[Idx(np, f)] = cur; prevCmd[Idx(np, f)] = CommandType.MoveForward;
                        if (np == m.Goal) { goalIdx = Idx(np, f); reachedGoal = true; break; }
                        q.Enqueue((np, f));
                    }
                }
                else // turn left (1) or right (2)
                {
                    int nf = act == 1 ? (f + 3) % 4 : (f + 1) % 4;
                    if (prev[Idx(p, nf)] < 0)
                    {
                        prev[Idx(p, nf)] = cur;
                        prevCmd[Idx(p, nf)] = act == 1 ? CommandType.TurnLeft : CommandType.TurnRight;
                        q.Enqueue((p, nf));
                    }
                }
            }
            if (reachedGoal) break;
        }
        if (goalIdx < 0) return false;

        // Reconstruct the flat command list and route cells.
        var flat = new List<CommandType>();
        var cells = new List<Vector2Int>();
        int node = goalIdx;
        while (node != startIdx)
        {
            flat.Add(prevCmd[node]);
            if (prevCmd[node] == CommandType.MoveForward)
                cells.Add(new Vector2Int((node / 4) % W, (node / 4) / W));
            node = prev[node];
        }
        flat.Reverse();
        cells.Add(m.Spawn);
        cells.Reverse();

        m.Path = cells;
        m.Flat = flat;
        m.SequenceTokens = flat.Count;
        m.Solution = Compress(flat);
        m.OptimalTokens = m.Solution.Count;
        m.Budget = m.OptimalTokens + BudgetSlack;
        return true;
    }

    // Solves any grid with the same BFS + loop compression the generator uses,
    // so a hand-authored beginner level can be solved too. Returns null when the
    // beacon is unreachable. allowLoops=false hands back the flat route, which is
    // what a Sequence Valley level's palette can actually express.
    public static List<CommandToken> SolveGrid(GridState grid, bool allowLoops)
    {
        // Without loops every route of minimum length costs the same, so one
        // search is enough. With loops the route's shape decides how well it
        // compresses, so try each expansion order and keep the shortest program.
        int orders = allowLoops ? ExpandOrders.Length : 1;
        List<CommandToken> best = null;

        for (int order = 0; order < orders; order++)
        {
            var m = new ChallengeMap
            {
                Width = grid.Width,
                Height = grid.Height,
                Spawn = grid.Position,
                SpawnFacing = grid.Facing,
                Goal = grid.GoalPosition
            };
            foreach (var cell in grid.Obstacles) m.Obstacles.Add(cell);

            if (!Solve(m, order)) return null; // unreachable regardless of order

            List<CommandToken> candidate;
            if (allowLoops)
            {
                candidate = m.Solution;
            }
            else
            {
                candidate = new List<CommandToken>();
                foreach (var c in m.Flat) candidate.Add(CommandToken.Simple(c));
            }

            if (best == null || candidate.Count < best.Count) best = candidate;
        }

        return best;
    }


    static List<CommandToken> Compress(List<CommandType> flat)
    {
        int n = flat.Count;
        var cost = new int[n + 1, n + 1];
        var kind = new int[n + 1, n + 1];   // 0 flat, 1 split at aux, 2 repeat aux times
        var aux = new int[n + 1, n + 1];

        for (int len = 1; len <= n; len++)
            for (int i = 0; i + len <= n; i++)
            {
                int j = i + len;
                cost[i, j] = len; kind[i, j] = 0;
                for (int k = i + 1; k < j; k++)
                    if (cost[i, k] + cost[k, j] < cost[i, j])
                    {
                        cost[i, j] = cost[i, k] + cost[k, j];
                        kind[i, j] = 1; aux[i, j] = k;
                    }
                for (int r = 2; r <= 4; r++)
                {
                    if (len % r != 0) continue;
                    int b = len / r;
                    bool same = true;
                    for (int t = b; t < len && same; t++)
                        if (flat[i + t] != flat[i + (t % b)]) same = false;
                    if (same && 2 + cost[i, i + b] < cost[i, j])
                    {
                        cost[i, j] = 2 + cost[i, i + b];
                        kind[i, j] = 2; aux[i, j] = r;
                    }
                }
            }

        var tokens = new List<CommandToken>();
        void Emit(int i, int j)
        {
            if (i >= j) return;
            switch (kind[i, j])
            {
                case 0:
                    for (int t = i; t < j; t++) tokens.Add(CommandToken.Simple(flat[t]));
                    break;
                case 1:
                    Emit(i, aux[i, j]); Emit(aux[i, j], j);
                    break;
                default:
                    tokens.Add(CommandToken.Loop(aux[i, j]));
                    Emit(i, i + (j - i) / aux[i, j]);
                    tokens.Add(CommandToken.Simple(CommandType.LoopEnd));
                    break;
            }
        }
        Emit(0, n);
        return tokens;
    }
}
