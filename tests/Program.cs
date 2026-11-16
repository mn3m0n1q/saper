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

// Fixed board after initialization: two mines in the top corners.
static Game ChordBoard() {
    var game = new Game(5, 5, 2);
    game.Play(11, "flag"); // Safe neighbor stays hidden so initialization cannot win.
    game.Play(12, "reveal");
    foreach (var cell in game.Cells) {
        cell.Mine = cell.Revealed = cell.Flagged = false;
        cell.Adjacent = 0;
    }
    game.Cells[0].Mine = game.Cells[4].Mine = true;
    for (var i = 0; i < 25; i++)
        game.Cells[i].Adjacent = new[] { 0, 4 }.Count(m => m != i && Math.Abs(m % 5 - i % 5) <= 1 && Math.Abs(m / 5 - i / 5) <= 1);
    game.Cells[6].Revealed = true;
    return game;
}
var insufficient = ChordBoard();
insufficient.Play(6, "reveal");
Check(insufficient.Cells.Count(c => c.Revealed) == 1, "Chord without enough flags must do nothing");
var excess = ChordBoard();
excess.Play(0, "flag"); excess.Play(1, "flag"); excess.Play(6, "reveal");
Check(excess.Cells.Count(c => c.Revealed) == 1, "Chord with too many flags must do nothing");
var correct = ChordBoard();
correct.Play(0, "flag"); correct.Play(6, "reveal");
Check(new[] { 1, 2, 5, 7, 10, 11, 12 }.All(i => correct.Cells[i].Revealed), "Chord reveals unflagged neighbors");
Check(correct.Cells[0].Flagged && !correct.Cells[0].Revealed, "Chord preserves flags");
Check(correct.Cells[24].Revealed, "Chord flood-fills connected empty cells");
Check(View(correct).GetProperty("status").GetString() != "lost", "Correct flags are safe");
var wrong = ChordBoard();
wrong.Play(1, "flag"); wrong.Play(6, "reveal");
Check(View(wrong).GetProperty("status").GetString() == "lost", "Wrong flags trigger a mine");
Check(View(wrong).GetProperty("exploded").GetInt32() == 0, "Chord reports exploded mine");
var win = ChordBoard();
foreach (var cell in win.Cells) if (!cell.Mine) cell.Revealed = true;
win.Cells[1].Revealed = false;
win.Play(0, "flag"); win.Play(6, "reveal");
Check(View(win).GetProperty("status").GetString() == "won", "Chord can win the game");
var edge = ChordBoard();
edge.Cells[1].Revealed = true;
edge.Play(0, "flag"); edge.Play(1, "reveal");
Check(edge.Cells[2].Revealed && edge.Cells[5].Revealed && edge.Cells[7].Revealed, "Chord on top edge");
Console.WriteLine("PASS: chording with correct, missing, excess and incorrect flags; flood fill, win and edge behavior.");

// A row of mines above a chain of numbered cells: no empty-cell flood fill
// can account for propagation along this board.
static Game CascadeBoard(bool revealedChain) {
    var game = new Game(7, 2, 7);
    game.Play(1, "flag");
    game.Play(0, "reveal");
    for (var i = 0; i < 14; i++) {
        var cell = game.Cells[i];
        cell.Mine = i < 7;
        cell.Flagged = i < 7;
        cell.Revealed = i == 7 || (revealedChain && i >= 8 && i <= 12);
        cell.Adjacent = Enumerable.Range(0, 7).Count(m => m != i && Math.Abs(m % 7 - i % 7) <= 1 && Math.Abs(m / 7 - i / 7) <= 1);
    }
    return game;
}
foreach (var revealedChain in new[] { false, true }) {
    var cascade = CascadeBoard(revealedChain);
    cascade.Play(7, "reveal");
    Check(cascade.Cells[13].Revealed, "Cascade reaches distant cells through new and previously revealed numbers");
    Check(View(cascade).GetProperty("status").GetString() == "won", "Cascade can win at its far end");
    Check(cascade.Cells.Take(7).All(c => c.Flagged && !c.Revealed), "Cascade preserves flagged mines");
}
var blockedCascade = CascadeBoard(true);
blockedCascade.Play(3, "flag");
blockedCascade.Play(7, "reveal");
Check(!blockedCascade.Cells[13].Revealed, "Cascade stops at unresolved numbers and does not jump across them");
Check(View(blockedCascade).GetProperty("status").GetString() == "playing", "Blocked cascade stays active");
var unsafeCascade = CascadeBoard(true);
unsafeCascade.Play(6, "flag");
unsafeCascade.Play(13, "flag");
unsafeCascade.Play(7, "reveal");
Check(View(unsafeCascade).GetProperty("status").GetString() == "lost", "Wrong flag at the far end loses during cascade");
Check(View(unsafeCascade).GetProperty("exploded").GetInt32() == 6, "Cascade identifies distant exploded mine");
Console.WriteLine("PASS: recursive numbered-cell cascade, revealed chains, stopping at unresolved numbers, distant win and loss.");
