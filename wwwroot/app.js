const $ = id => document.getElementById(id);
let game, level = 'easy', flagMode = false, busy = false, receivedAt = Date.now();
const labels = { ready: 'Twój pierwszy ruch. Wybierz dowolne pole.', playing: 'Spokojnie. Każda liczba to wskazówka.', won: 'Świetna robota! Wszystkie bezpieczne pola odkryte.', lost: 'To była mina. Jeszcze jedna próba?' };
async function request(path, body) {
  const response = await fetch(path, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) });
  if (!response.ok) {
    const error = await response.json().catch(() => ({}));
    throw new Error(error.error || (response.status === 429 ? 'Za dużo ruchów. Spróbuj za chwilę.' : 'Nie udało się połączyć z serwerem. Spróbuj ponownie.'));
  }
  return response.json();
}
async function start(nextLevel = level) {
  if (busy) return;
  busy = true;
  try {
    const next = await request('/api/games/', { level: nextLevel });
    level = nextLevel; game = next; receivedAt = Date.now(); render();
  } catch (error) { $('message').textContent = error.message; }
  finally { busy = false; }
}
async function move(index, action) {
  if (busy || !game || ['won', 'lost'].includes(game.status)) return;
  busy = true;
  try {
    game = await request(`/api/games/${game.id}/moves`, { index, action });
    receivedAt = Date.now(); render(index);
  } catch (error) { $('message').textContent = error.message; }
  finally { busy = false; }
}
function render(focusIndex) {
  const board = $('board'); board.replaceChildren(); board.className = `board ${level}`;
  game.cells.forEach((cell, index) => {
    const button = document.createElement('button');
    button.className = 'cell' + (cell.revealed ? ' revealed' : '') + (cell.flagged ? ' flagged' : '') + (cell.mine ? ' mine' : '') + (index === game.exploded ? ' exploded' : '');
    button.dataset.number = cell.adjacent;
    button.textContent = cell.mine ? '✳' : cell.flagged ? (game.status === 'lost' ? '×' : '⚑') : cell.revealed && cell.adjacent ? cell.adjacent : '';
    button.setAttribute('aria-label', `Wiersz ${Math.floor(index / game.width) + 1}, kolumna ${index % game.width + 1}: ${cell.mine ? 'mina' : cell.flagged ? 'flaga' : cell.revealed ? (cell.adjacent ? `${cell.adjacent} sąsiednich min` : 'puste') : 'zakryte'}`);
    button.tabIndex = index === (focusIndex ?? 0) ? 0 : -1;
    button.onclick = () => move(index, flagMode ? 'flag' : 'reveal');
    button.oncontextmenu = event => { event.preventDefault(); move(index, 'flag'); };
    button.onkeydown = event => {
      const offsets = { ArrowLeft: -1, ArrowRight: 1, ArrowUp: -game.width, ArrowDown: game.width };
      if (event.key in offsets) {
        event.preventDefault(); const next = index + offsets[event.key];
        if (next >= 0 && next < game.cells.length) { button.tabIndex = -1; board.children[next].tabIndex = 0; board.children[next].focus(); }
      } else if (event.key.toLowerCase() === 'f') { event.preventDefault(); move(index, 'flag'); }
    };
    board.append(button);
  });
  if (focusIndex !== undefined) board.children[focusIndex].focus({ preventScroll: true });
  $('mines').textContent = String(game.mines - game.flags).padStart(3, '0');
  $('face').textContent = game.status === 'lost' ? '☹' : game.status === 'won' ? '✧' : '☺';
  $('message').textContent = labels[game.status];
  $('detail').textContent = `${game.width} × ${game.height} pól · ${game.mines} min`;
  document.querySelectorAll('[data-level]').forEach(button => {
    button.classList.toggle('selected', button.dataset.level === level);
    button.setAttribute('aria-pressed', button.dataset.level === level);
  });
  tick();
}
function tick() {
  const seconds = game ? game.elapsed + (game.status === 'playing' ? Math.floor((Date.now() - receivedAt) / 1000) : 0) : 0;
  $('timer').textContent = `${String(Math.floor(seconds / 60)).padStart(2, '0')}:${String(seconds % 60).padStart(2, '0')}`;
}
document.querySelectorAll('[data-level]').forEach(button => button.onclick = () => start(button.dataset.level));
$('restart').onclick = () => start();
$('flag-mode').onclick = () => { flagMode = !flagMode; $('flag-mode').setAttribute('aria-pressed', flagMode); $('flag-mode').textContent = `⚑ Tryb flagowania: ${flagMode ? 'włączony' : 'wyłączony'}`; };
$('help').onclick = () => { $('instructions').hidden = !$('instructions').hidden; $('help').setAttribute('aria-expanded', !$('instructions').hidden); };
setInterval(tick, 250);
start();
