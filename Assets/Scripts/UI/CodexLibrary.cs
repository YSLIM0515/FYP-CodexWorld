// Codex World — The Codex: guidebook content & unlock rules
// Eight "recovered pages" that connect the game's commands to real programming
// concepts and industry examples. Pages unlock as the player meets each
// concept (progressive disclosure, like the tutorial). Icon indexes point into
// the ACH_Icon_* sprites on AchievementUI.png so the art style stays unified.

using UnityEngine;

public static class CodexLibrary
{
    public struct Entry
    {
        public string Name, What, InGame, Code, RealWorld, UnlockHint;
        public int Icon; // ACH_Icon_{n}
        public Entry(string name, int icon, string what, string inGame, string code, string real, string hint)
        { Name = name; Icon = icon; What = what; InGame = inGame; Code = code; RealWorld = real; UnlockHint = hint; }
    }

    public static readonly Entry[] Entries =
    {
        new Entry("SEQUENCES", 0,
            "A program is a list of instructions executed one after another, in order. The order is everything - computers do exactly what you say, in exactly the sequence you say it.",
            "Every queue you build is a sequence. MOVE MOVE TURN LEFT reads top to bottom, one block at a time - swap two blocks and AXIOM walks a completely different path.",
            "move()\nmove()\nturn_left()\nmove()",
            "A washing machine runs a fixed sequence: fill, wash, drain, spin. Rocket launch checklists and barista workflows are sequences too - one wrong order and the coffee (or the rocket) is ruined.",
            "Recovered at the start of your journey."),

        new Entry("LOOPS", 6,
            "A loop repeats a block of instructions a chosen number of times. Instead of writing the same command again and again, you write it once and say how often it should run.",
            "LOOP x4 [ MOVE ] END walks four tiles using 3 blocks instead of 4. In Challenge Mode the command budget is so tight that loops are the only way your program fits.",
            "for i in range(4):\n    move()",
            "Warehouse robots repeat pick-carry-drop thousands of times a day. A factory assembly line, your playlist on repeat, payroll for 10,000 employees - all loops.",
            "Solve a level using a LOOP to recover this page."),

        new Entry("NESTED LOOPS", 10,
            "A loop inside another loop. The inner loop finishes ALL of its repeats for every single repeat of the outer one - small numbers multiply into a lot of work.",
            "LOOP x3 [ LOOP x2 [ MOVE ] END  TURN RIGHT ] END produces 6 moves and 3 turns. The serpentine Challenge maps are designed exactly for this pattern.",
            "for row in range(3):\n    for step in range(2):\n        move()\n    turn_right()",
            "Your phone camera brightens a photo with \"for every row, for every pixel\" - over 12 million pixels processed in a blink. Spreadsheets recalculate the same way.",
            "Use a loop inside a loop to recover this page."),

        new Entry("CONDITIONALS", 12,
            "An IF statement runs its block only when a condition is true at that moment. It lets a single program react differently to different situations.",
            "IF OBSTACLE [ TURN LEFT ] END makes AXIOM dodge only when a corrupted block is actually ahead - the same program works whether the path is blocked or clear.",
            "if obstacle_ahead():\n    turn_left()\nelse:\n    move()",
            "A self-driving car brakes IF an obstacle is ahead. Thermostats, spam filters, autocorrect and every video-game enemy you have ever fought are stacks of conditionals.",
            "Solve a level using an IF to recover this page."),

        new Entry("BOOLEAN LOGIC", 7,
            "Booleans are values with only two states: true or false. Conditions combine them with AND, OR and NOT to make precise decisions.",
            "IF OBSTACLE asks the world a boolean question - it answers true or false. IF CLEAR is simply NOT obstacle: the exact opposite answer.",
            "if card_valid and pin_correct:\n    dispense_cash()",
            "An ATM pays out only when the card is valid AND the PIN matches AND the balance is enough. Search engines filter results with the same AND / OR logic you type.",
            "Push deeper into the corrupted region to recover this page."),

        new Entry("DEBUGGING", 1,
            "Debugging is finding out why a program did what it did, instead of what you wanted. A failure is information - trace it, never guess.",
            "When AXIOM crashes, the fail panel shows exactly where the run ended. Read your queue, find the block where the plan went wrong, and fix that one block.",
            "print(position)\n# see what the program\n# ACTUALLY did, not what\n# you hoped it did",
            "In 1947 engineers found a real moth jammed inside the Harvard Mark II computer - the first literal \"bug\". NASA still traces every anomaly to a root cause before flying again.",
            "Fail a run (everyone does!) to recover this page."),

        new Entry("OPTIMIZATION", 4,
            "Optimization means achieving the same result with less - fewer steps, less time, less memory. Elegant beats bulky.",
            "The 3-star ratings and Challenge budgets are optimization made visible: your program still wins, but every block you save is proof of skill.",
            "# 12 blocks:\nmove(); move(); move()...\n# 3 blocks:\nfor i in range(12): move()",
            "Google engineers celebrate shaving milliseconds - at their scale it saves millions and real energy. Game studios count draw calls the way you count blocks.",
            "Solve any level with the optimal block count to recover this page."),

        new Entry("ALGORITHMS", 13,
            "An algorithm is a step-by-step recipe that solves a whole class of problems, not just one. The same recipe works on any input you give it.",
            "Every Challenge map you play was carved and solved by BFS (breadth-first search) before you ever saw it - the game proved a solution exists and counted its optimal blocks.",
            "frontier = [start]\nwhile frontier:\n    cell = frontier.pop(0)\n    visit(neighbours(cell))",
            "BFS is how Google Maps finds your route and how social networks find \"friends of friends\". In Challenge Mode you are racing an industry algorithm with your own brain.",
            "Finish Beginner Mode to recover this page."),
    };

    public static bool IsUnlocked(int i)
    {
        switch (i)
        {
            case 0: return true;                                                        // Sequences
            case 1: return AchievementManager.IsUnlocked(6) || ProgressManager.IsCompleted(3);  // Loops
            case 2: return AchievementManager.IsUnlocked(10) || ProgressManager.IsCompleted(5); // Nested
            case 3: return AchievementManager.IsUnlocked(12) || ProgressManager.IsCompleted(6); // Conditionals
            case 4: return ProgressManager.IsCompleted(7);                              // Booleans
            case 5: return PlayerPrefs.GetInt("cw_ach_fails", 0) > 0 || AchievementManager.IsUnlocked(1); // Debugging
            case 6: { for (int l = 0; l < ProgressManager.LevelCount; l++) if (ProgressManager.HasBonus(l)) return true; return false; } // Optimization
            case 7: return GameModeScreen.ChallengeUnlocked;                            // Algorithms
            default: return false;
        }
    }

    public static int UnlockedCount()
    {
        int c = 0;
        for (int i = 0; i < Entries.Length; i++) if (IsUnlocked(i)) c++;
        return c;
    }
}
