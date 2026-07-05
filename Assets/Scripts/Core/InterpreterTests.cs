// Codex World — Interpreter smoke tests (optional dev tool)
// Attach to any GameObject and press Play to verify interpreter behavior in the
// Console. Ported from the 2D prototype's PrototypeRunner and extended with
// grid-bounds and goal-halt cases introduced in the 3D version.

using System.Collections.Generic;
using UnityEngine;

public class InterpreterTests : MonoBehaviour
{
    void Start()
    {
        int passed = 0;
        int total = 0;

        RunTest("1. Simple sequence (3x MoveForward)", ref total, ref passed,
            tokens: new List<CommandToken> {
                CommandToken.Simple(CommandType.MoveForward),
                CommandToken.Simple(CommandType.MoveForward),
                CommandToken.Simple(CommandType.MoveForward),
            },
            obstacles: new Vector2Int[] { },
            expectedPos: new Vector2Int(0, 3),
            expectedFacing: Facing.North,
            expectFailed: false);

        RunTest("2. Turn + move (TurnRight, 2x MoveForward)", ref total, ref passed,
            tokens: new List<CommandToken> {
                CommandToken.Simple(CommandType.TurnRight),
                CommandToken.Simple(CommandType.MoveForward),
                CommandToken.Simple(CommandType.MoveForward),
            },
            obstacles: new Vector2Int[] { },
            expectedPos: new Vector2Int(2, 0),
            expectedFacing: Facing.East,
            expectFailed: false);

        RunTest("3. Loop only — LOOP(3){MoveForward}", ref total, ref passed,
            tokens: new List<CommandToken> {
                CommandToken.Loop(3),
                CommandToken.Simple(CommandType.MoveForward),
                CommandToken.Simple(CommandType.LoopEnd),
            },
            obstacles: new Vector2Int[] { },
            expectedPos: new Vector2Int(0, 3),
            expectedFacing: Facing.North,
            expectFailed: false);

        RunTest("4. Conditional true — IF_CLEAR{MoveForward} (path is clear)", ref total, ref passed,
            tokens: new List<CommandToken> {
                CommandToken.Simple(CommandType.IfClear),
                CommandToken.Simple(CommandType.MoveForward),
                CommandToken.Simple(CommandType.IfEnd),
            },
            obstacles: new Vector2Int[] { },
            expectedPos: new Vector2Int(0, 1),
            expectedFacing: Facing.North,
            expectFailed: false);

        RunTest("5. Conditional true — IF_OBSTACLE{TurnRight}, obstacle ahead forces a turn", ref total, ref passed,
            tokens: new List<CommandToken> {
                CommandToken.Simple(CommandType.IfObstacle),
                CommandToken.Simple(CommandType.TurnRight),
                CommandToken.Simple(CommandType.IfEnd),
                CommandToken.Simple(CommandType.MoveForward),
            },
            obstacles: new Vector2Int[] { new Vector2Int(0, 1) },
            expectedPos: new Vector2Int(1, 0),
            expectedFacing: Facing.East,
            expectFailed: false);

        RunTest("6. NESTED — LOOP(3){ IF_OBSTACLE{TurnRight} ; MoveForward }, obstacle only blocks iteration 1", ref total, ref passed,
            tokens: new List<CommandToken> {
                CommandToken.Loop(3),
                CommandToken.Simple(CommandType.IfObstacle),
                CommandToken.Simple(CommandType.TurnRight),
                CommandToken.Simple(CommandType.IfEnd),
                CommandToken.Simple(CommandType.MoveForward),
                CommandToken.Simple(CommandType.LoopEnd),
            },
            obstacles: new Vector2Int[] { new Vector2Int(0, 1) },
            expectedPos: new Vector2Int(3, 0),
            expectedFacing: Facing.East,
            expectFailed: false);

        RunTest("7. Failure — MoveForward into an obstacle halts execution", ref total, ref passed,
            tokens: new List<CommandToken> {
                CommandToken.Simple(CommandType.MoveForward),
                CommandToken.Simple(CommandType.MoveForward), // should never run
            },
            obstacles: new Vector2Int[] { new Vector2Int(0, 1) },
            expectedPos: new Vector2Int(0, 0),
            expectedFacing: Facing.North,
            expectFailed: true);

        RunTest("8. Bounds — walking off the grid edge fails (3D addition)", ref total, ref passed,
            tokens: new List<CommandToken> {
                CommandToken.Simple(CommandType.TurnLeft), // face West at x=0
                CommandToken.Simple(CommandType.MoveForward),
            },
            obstacles: new Vector2Int[] { },
            expectedPos: new Vector2Int(0, 0),
            expectedFacing: Facing.West,
            expectFailed: true);

        // 9. Goal halt: execution stops the moment the goal is reached — the third
        // MoveForward (which would crash into the obstacle at (0,3)) never runs.
        RunTest("9. Goal halt — reaching the goal stops execution immediately (3D addition)", ref total, ref passed,
            tokens: new List<CommandToken> {
                CommandToken.Simple(CommandType.MoveForward),
                CommandToken.Simple(CommandType.MoveForward),
                CommandToken.Simple(CommandType.MoveForward),
            },
            obstacles: new Vector2Int[] { new Vector2Int(0, 3) },
            expectedPos: new Vector2Int(0, 2),
            expectedFacing: Facing.North,
            expectFailed: false,
            goal: new Vector2Int(0, 2),
            expectGoal: true);

        Debug.Log($"===== INTERPRETER TESTS: {passed}/{total} PASSED =====");
    }

    void RunTest(string name, ref int total, ref int passed, List<CommandToken> tokens,
        Vector2Int[] obstacles, Vector2Int expectedPos, Facing expectedFacing, bool expectFailed,
        Vector2Int? goal = null, bool expectGoal = false)
    {
        total++;
        var state = new GridState
        {
            Position = Vector2Int.zero,
            Facing = Facing.North,
            GoalPosition = goal ?? new Vector2Int(99, 99), // unreachable unless the test sets one
            Width = 100,
            Height = 100,
        };
        foreach (var o in obstacles) state.Obstacles.Add(o);

        var result = CommandInterpreter.Run(tokens, state);

        bool ok = state.Position == expectedPos
            && state.Facing == expectedFacing
            && result.Failed == expectFailed
            && result.ReachedGoal == expectGoal;

        Debug.Log($"--- TEST: {name} ---");
        foreach (var line in result.Log) Debug.Log("    " + line);
        Debug.Log($"    Expected: pos={expectedPos}, facing={expectedFacing}, failed={expectFailed}, goal={expectGoal}");
        Debug.Log($"    Actual:   pos={state.Position}, facing={state.Facing}, failed={result.Failed}, goal={result.ReachedGoal}" +
                   (result.Failed ? $" ({result.FailureReason})" : ""));
        Debug.Log(ok ? "    RESULT: PASS" : "    RESULT: *** FAIL ***");

        if (ok) passed++;
    }
}
