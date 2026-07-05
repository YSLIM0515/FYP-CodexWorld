// Codex World — Command Interpreter (3D version)
// Ported from the validated 2D prototype. Executes a player-built command
// sequence (sequences, counted loops, if-obstacle/if-clear conditionals,
// arbitrarily nested) against a grid state and records snapshots for playback.

using System.Collections.Generic;
using UnityEngine;

public enum CommandType
{
    MoveForward,
    TurnLeft,
    TurnRight,
    LoopStart,
    LoopEnd,
    IfObstacle,
    IfClear,
    IfEnd
}

public enum Facing { North, East, South, West }

public struct CommandToken
{
    public CommandType Type;
    public int LoopCount; // only used when Type == LoopStart

    public static CommandToken Simple(CommandType type) => new CommandToken { Type = type };
    public static CommandToken Loop(int count) => new CommandToken { Type = CommandType.LoopStart, LoopCount = count };
}

public class GridState
{
    public Vector2Int Position;
    public Facing Facing;
    public HashSet<Vector2Int> Obstacles = new HashSet<Vector2Int>();
    public Vector2Int GoalPosition;
    public int Width;
    public int Height;

    public bool InBounds(Vector2Int cell) =>
        cell.x >= 0 && cell.x < Width && cell.y >= 0 && cell.y < Height;

    // A cell is blocked if it holds a corrupted block or lies outside the grid.
    public bool IsBlocked(Vector2Int cell) => !InBounds(cell) || Obstacles.Contains(cell);
}

public class ExecutionResult
{
    public bool ReachedGoal;
    public bool Failed;
    public string FailureReason;
    public List<string> Log = new List<string>();
    // One snapshot per state-changing step (move/turn), in order — used by visual playback.
    public List<(Vector2Int pos, Facing facing)> Snapshots = new List<(Vector2Int, Facing)>();
}

// Tree nodes produced by parsing the flat token list — this is what makes nested
// LOOP/IF blocks executable without ambiguity about where a block ends.
public abstract class Node { }
public class LeafNode : Node { public CommandType Type; }
public class LoopNode : Node { public int Count; public List<Node> Body; }
public class ConditionalNode : Node { public CommandType Condition; public List<Node> Body; }

public static class CommandInterpreter
{
    public static Vector2Int DirVector(Facing f) => f switch
    {
        Facing.North => new Vector2Int(0, 1),
        Facing.East => new Vector2Int(1, 0),
        Facing.South => new Vector2Int(0, -1),
        Facing.West => new Vector2Int(-1, 0),
        _ => Vector2Int.zero
    };

    static Facing TurnLeftOf(Facing f) => f switch
    {
        Facing.North => Facing.West,
        Facing.West => Facing.South,
        Facing.South => Facing.East,
        Facing.East => Facing.North,
        _ => f
    };

    static Facing TurnRightOf(Facing f) => f switch
    {
        Facing.North => Facing.East,
        Facing.East => Facing.South,
        Facing.South => Facing.West,
        Facing.West => Facing.North,
        _ => f
    };

    static bool IsObstacleAhead(GridState state)
    {
        var ahead = state.Position + DirVector(state.Facing);
        return state.IsBlocked(ahead);
    }

    // Recursive-descent parse of a flat token list into a tree of Nodes.
    // LOOP_START/LOOP_END and IF_OBSTACLE|IF_CLEAR/IF_END are treated as bracketed blocks.
    public static List<Node> Parse(List<CommandToken> tokens)
    {
        int i = 0;
        return ParseBlock(tokens, ref i);
    }

    static List<Node> ParseBlock(List<CommandToken> tokens, ref int i)
    {
        var result = new List<Node>();
        while (i < tokens.Count)
        {
            var t = tokens[i];

            if (t.Type == CommandType.LoopEnd || t.Type == CommandType.IfEnd)
            {
                // Caller consumes this terminator — return control without advancing past it here.
                return result;
            }

            if (t.Type == CommandType.LoopStart)
            {
                int count = t.LoopCount;
                i++; // consume LOOP_START
                var body = ParseBlock(tokens, ref i);
                i++; // consume LOOP_END
                result.Add(new LoopNode { Count = count, Body = body });
            }
            else if (t.Type == CommandType.IfObstacle || t.Type == CommandType.IfClear)
            {
                var condition = t.Type;
                i++; // consume IF_OBSTACLE / IF_CLEAR
                var body = ParseBlock(tokens, ref i);
                i++; // consume IF_END
                result.Add(new ConditionalNode { Condition = condition, Body = body });
            }
            else
            {
                result.Add(new LeafNode { Type = t.Type });
                i++;
            }
        }
        return result;
    }

    public static ExecutionResult Run(List<CommandToken> tokens, GridState state)
    {
        var tree = Parse(tokens);
        var result = new ExecutionResult();
        Execute(tree, state, result);
        if (state.Position == state.GoalPosition) result.ReachedGoal = true;
        return result;
    }

    // Returns false to signal "stop executing" (propagates up through nested loops/conditionals).
    // Execution halts on failure OR as soon as the goal is reached.
    static bool Execute(List<Node> nodes, GridState state, ExecutionResult result)
    {
        foreach (var node in nodes)
        {
            switch (node)
            {
                case LeafNode leaf:
                    switch (leaf.Type)
                    {
                        case CommandType.MoveForward:
                            var next = state.Position + DirVector(state.Facing);
                            if (!state.InBounds(next))
                            {
                                result.Failed = true;
                                result.FailureReason = $"AXIOM stepped off the grid edge at {next}";
                                result.Log.Add($"MOVE_FORWARD -> OFF GRID at {next}");
                                return false;
                            }
                            if (state.Obstacles.Contains(next))
                            {
                                result.Failed = true;
                                result.FailureReason = $"AXIOM crashed into a corrupted block at {next}";
                                result.Log.Add($"MOVE_FORWARD -> BLOCKED by obstacle at {next}");
                                return false;
                            }
                            state.Position = next;
                            result.Log.Add($"MOVE_FORWARD -> {state.Position}");
                            result.Snapshots.Add((state.Position, state.Facing));
                            break;
                        case CommandType.TurnLeft:
                            state.Facing = TurnLeftOf(state.Facing);
                            result.Log.Add($"TURN_LEFT -> facing {state.Facing}");
                            result.Snapshots.Add((state.Position, state.Facing));
                            break;
                        case CommandType.TurnRight:
                            state.Facing = TurnRightOf(state.Facing);
                            result.Log.Add($"TURN_RIGHT -> facing {state.Facing}");
                            result.Snapshots.Add((state.Position, state.Facing));
                            break;
                    }
                    break;

                case LoopNode loop:
                    for (int k = 0; k < loop.Count; k++)
                    {
                        result.Log.Add($"-- loop iteration {k + 1}/{loop.Count} --");
                        if (!Execute(loop.Body, state, result)) return false;
                    }
                    break;

                case ConditionalNode cond:
                    bool obstacleAhead = IsObstacleAhead(state);
                    bool shouldRun = cond.Condition == CommandType.IfObstacle ? obstacleAhead : !obstacleAhead;
                    result.Log.Add($"{cond.Condition} -> {(shouldRun ? "TRUE, executing block" : "FALSE, skipping block")}");
                    if (shouldRun)
                    {
                        if (!Execute(cond.Body, state, result)) return false;
                    }
                    break;
            }

            if (state.Position == state.GoalPosition)
            {
                result.ReachedGoal = true;
                return false; // goal reached — halt execution immediately
            }
        }
        return true;
    }
}
