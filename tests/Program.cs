using System.Text.Json;
static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
static JsonElement View(Game game) => JsonSerializer.SerializeToElement(game.View());
foreach (var (w, h, m) in new[] { (9, 9, 10), (16, 16, 40), (30, 16, 99) }) {
    for (var run = 0; run < 20; run++) {
        var game = new Game(w, h, m);
        game.Play(0, "flag"); game.Play(0, "reveal");
        Check(View(game).GetProperty("status").GetString() == "ready", "Flag must block reveal");
        game.Play(0, "flag"); game.Play(0, "reveal");
        Check(game.Cells[0].Revealed && game.Cells[0].Adjacent == 0, "Safe first move");
        Check(game.Cells.Count(c => c.Mine) == m, "Mine count");
        Check(View(game).GetProperty("cells").EnumerateArray().All(c => c.GetProperty("mine").ValueKind == JsonValueKind.Null), "Hidden mines");
        for (int i = 0; i < game.Cells.Length; i++) if (!game.Cells[i].Mine) game.Play(i, "reveal");
        Check(View(game).GetProperty("status").GetString() == "won", "Win");
        var before = JsonSerializer.Serialize(game.View()); game.Play(1, "flag");
        Check(before == JsonSerializer.Serialize(game.View()), "Finished game immutable");
        var loss = new Game(w, h, m); loss.Play(w * h / 2, "reveal");
        loss.Play(Array.FindIndex(loss.Cells, c => c.Mine), "reveal");
        Check(View(loss).GetProperty("status").GetString() == "lost", "Loss");
        Check(View(loss).GetProperty("cells").EnumerateArray().Count(c => c.GetProperty("mine").GetBoolean()) == m, "Mines exposed after end");
    }
}
Console.WriteLine("PASS: 60 randomized scenarios, safe start, mine counts, flags, hidden mines, win, loss, immutable finished state.");
