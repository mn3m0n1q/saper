using System.Security.Cryptography;

public sealed class Cell {
    public bool Mine, Revealed, Flagged;
    public int Adjacent;
}
public sealed class Game(int width, int height, int mines) {
    public Guid Id { get; } = Guid.NewGuid();
    public Cell[] Cells { get; } = Enumerable.Range(0, width * height).Select(_ => new Cell()).ToArray();
    private DateTimeOffset? started, ended;
    private string status = "ready";
    private int? exploded;
    private IEnumerable<int> Neighbors(int index) {
        var x = index % width; var y = index / width;
        for (var dy = -1; dy <= 1; dy++)
            for (var dx = -1; dx <= 1; dx++)
                if ((dx != 0 || dy != 0) && x + dx >= 0 && x + dx < width && y + dy >= 0 && y + dy < height)
                    yield return (y + dy) * width + x + dx;
    }
    private void Seed(int first) {
        var safe = Neighbors(first).Append(first).ToHashSet();
        var candidates = Enumerable.Range(0, Cells.Length).Where(i => !safe.Contains(i)).ToArray();
        RandomNumberGenerator.Shuffle(candidates.AsSpan());
        foreach (var i in candidates.Take(mines)) Cells[i].Mine = true;
        for (var i = 0; i < Cells.Length; i++) Cells[i].Adjacent = Neighbors(i).Count(n => Cells[n].Mine);
        started = DateTimeOffset.UtcNow; status = "playing";
    }
    public void Play(int index, string action) {
        if (status is "won" or "lost") return;
        var cell = Cells[index];
        if (action == "flag") {
            if (!cell.Revealed && (cell.Flagged || Cells.Count(c => c.Flagged) < mines)) cell.Flagged = !cell.Flagged;
            return;
        }
        if (cell.Flagged) return;
        var queue = new Queue<int>();
        if (cell.Revealed) {
            var neighbors = Neighbors(index).ToArray();
            if (cell.Adjacent == 0 || neighbors.Count(n => Cells[n].Flagged) != cell.Adjacent) return;
            foreach (var neighbor in neighbors) queue.Enqueue(neighbor);
        } else {
            if (status == "ready") Seed(index);
            queue.Enqueue(index);
        }
        while (queue.TryDequeue(out var next)) {
            var target = Cells[next];
            if (target.Revealed || target.Flagged) continue;
            if (target.Mine) {
                status = "lost"; exploded = next; ended = DateTimeOffset.UtcNow;
                return;
            }
            target.Revealed = true;
            if (target.Adjacent == 0) foreach (var neighbor in Neighbors(next)) queue.Enqueue(neighbor);
        }
        if (Cells.Count(c => !c.Mine && !c.Revealed) == 0) { status = "won"; ended = DateTimeOffset.UtcNow; }
    }
    public object View() => new {
        id = Id, width, height, mines, status, exploded,
        flags = Cells.Count(c => c.Flagged),
        elapsed = started is null ? 0 : (int)((ended ?? DateTimeOffset.UtcNow) - started.Value).TotalSeconds,
        cells = Cells.Select(c => new { revealed = c.Revealed, flagged = c.Flagged,
            adjacent = c.Revealed ? (int?)c.Adjacent : null,
            mine = status is "lost" or "won" ? c.Mine : (bool?)null }).ToArray()
    };
}
