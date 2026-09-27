import { useState, type FormEvent } from 'react'
import { ApiError } from '../../shared/api/client'
import { Board } from './Board'
import { getGame, pass, playMove, resign, startGame, type Color, type Coordinates, type Game } from './api'

const strengths = ['Kyu20', 'Kyu15', 'Kyu10', 'Kyu5', 'Kyu1', 'Dan1', 'Dan5', 'Dan9', 'Superhuman']

const outcomeText = {
  PlayerResigned: 'You resigned.',
  BotResigned: 'The bot resigned — you win!',
  TwoConsecutivePasses: 'Both players passed. Count the score yourselves.',
}

export function GamePage() {
  const [game, setGame] = useState<Game | null>(null)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function run(action: () => Promise<Game>) {
    setBusy(true)
    setError(null)
    try {
      setGame(await action())
    } catch (err) {
      // 409 means the game changed underneath us: reload, don't retry.
      if (err instanceof ApiError && err.status === 409 && game) setGame(await getGame(game.id))
      else if (err instanceof ApiError && err.status === 400) setError('That move is not allowed.')
      else setError('Something went wrong.')
    } finally {
      setBusy(false)
    }
  }

  if (!game) return <NewGameForm busy={busy} error={error} onStart={(r) => run(() => startGame(r))} />

  const finished = game.outcome !== null
  const myTurn = !finished && game.turn === game.playerColor

  return (
    <div className="flex flex-col items-center gap-3">
      <p className="text-sm text-gray-700">
        You play {game.playerColor} vs {game.botStrength} · komi {game.komi}
        {game.lastMove && !game.lastMove.coordinates && ' · last move was a pass'}
      </p>
      <Board
        board={game.board}
        lastMove={game.lastMove?.coordinates ?? null}
        disabled={busy || !myTurn}
        onPlay={(at: Coordinates) => run(() => playMove(game.id, at))}
      />
      {error && <p className="text-sm text-red-600">{error}</p>}
      {finished ? (
        <>
          <p className="font-semibold">{outcomeText[game.outcome!]}</p>
          <button onClick={() => setGame(null)} className="rounded bg-gray-900 px-3 py-1 text-white">
            New game
          </button>
        </>
      ) : (
        <div className="flex gap-2">
          <button disabled={busy || !myTurn} onClick={() => run(() => pass(game.id))} className="rounded border px-3 py-1 disabled:opacity-50">
            Pass
          </button>
          <button disabled={busy} onClick={() => run(() => resign(game.id))} className="rounded border px-3 py-1 disabled:opacity-50">
            Resign
          </button>
          {busy && <span className="self-center text-sm text-gray-500">Bot is thinking…</span>}
        </div>
      )}
    </div>
  )
}

type NewGameFormProps = {
  busy: boolean
  error: string | null
  onStart: (request: { playerColor: Color; boardSize: number; botStrength: string }) => void
}

function NewGameForm({ busy, error, onStart }: NewGameFormProps) {
  const [playerColor, setPlayerColor] = useState<Color>('Black')
  const [botStrength, setBotStrength] = useState('Kyu20')

  function handleSubmit(event: FormEvent) {
    event.preventDefault()
    onStart({ playerColor, boardSize: 9, botStrength })
  }

  return (
    <form onSubmit={handleSubmit} className="flex w-72 flex-col gap-3">
      <h2 className="text-xl font-semibold">New 9×9 game</h2>
      <label className="flex justify-between">
        Your color
        <select value={playerColor} onChange={(e) => setPlayerColor(e.target.value as Color)} className="rounded border">
          <option>Black</option>
          <option>White</option>
        </select>
      </label>
      <label className="flex justify-between">
        Bot strength
        <select value={botStrength} onChange={(e) => setBotStrength(e.target.value)} className="rounded border">
          {strengths.map((s) => (
            <option key={s}>{s}</option>
          ))}
        </select>
      </label>
      {error && <p className="text-sm text-red-600">{error}</p>}
      <button disabled={busy} className="rounded bg-gray-900 py-1 text-white disabled:opacity-50">
        {busy ? 'Starting…' : 'Start'}
      </button>
    </form>
  )
}
