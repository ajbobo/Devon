using Devon.Services;
using Devon.Models;

namespace Devon;

class Program
{
    static void Main(string[] args)
    {
        if (args.Contains("test"))
        {
            RunTests();
            return;
        }

        var game = new Game();
        game.Run();
    }

    static void RunTests()
    {
        Console.WriteLine("Running ConditionEvaluator tests...");
        var evaluator = new ConditionEvaluator();
        var player = new Player();
        var room = new Room { Conditions = new HashSet<string>() };
        var state = new GameState { Player = player, CurrentRoom = room };

        bool Test(string expr, bool expected)
        {
            try
            {
                var result = evaluator.Evaluate(expr, state);
                bool pass = result == expected;
                Console.WriteLine($"  {expr} => {result} (expected {expected}) {(pass ? "✓" : "✗")}");
                return pass;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  {expr} => ERROR: {ex.Message} ✗");
                return false;
            }
        }

        bool all = true;
        all &= Test("true", true);
        all &= Test("false", false);
        player.AddItem("key");
        all &= Test("HasItem(\"key\")", true);
        all &= Test("HasItem(\"sword\")", false);
        player.AddCondition("has_armor");
        all &= Test("HasCondition(\"has_armor\")", true);
        all &= Test("!HasCondition(\"has_armor\")", false);
        room.Conditions.Add("lit");
        all &= Test("RoomHasCondition(\"lit\")", true);
        // Combinators with literals
        all &= Test("And(true, true)", true);
        all &= Test("And(true, false)", false);
        all &= Test("Or(false, false)", false);
        all &= Test("Or(false, true)", true);
        all &= Test("Not(true)", false);
        all &= Test("Not(false)", true);
        // Mixed functions
        all &= Test("And(HasItem(\"key\"), true)", true);
        all &= Test("And(false, HasCondition(\"has_armor\"))", false);
        all &= Test("And(HasItem(\"key\"), HasCondition(\"has_armor\"))", true);
        all &= Test("Or(HasItem(\"sword\"), HasCondition(\"has_armor\"))", true); // sword false, armor true => true
        all &= Test("Not(HasItem(\"key\"))", false);
        all &= Test("And(HasItem(\"key\"), RoomHasCondition(\"lit\"))", true);
        all &= Test("Not(And(HasItem(\"key\"), HasCondition(\"has_armor\")))", false);

        Console.WriteLine(all ? "\nAll tests passed!" : "\nSome tests failed.");
    }
}
