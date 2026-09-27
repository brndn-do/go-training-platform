import type { Content, Coordinates } from './api'

const cell = 36

type BoardProps = {
  board: Content[][]
  lastMove: Coordinates | null
  disabled: boolean
  onPlay: (at: Coordinates) => void
}

/** Renders the server's board as-is. board[x][y]: x is the column, y the row. */
export function Board({ board, lastMove, disabled, onPlay }: BoardProps) {
  const size = board.length
  const span = cell * (size - 1)
  const lines = Array.from({ length: size }, (_, i) => i * cell)

  return (
    <svg
      viewBox={`${-cell} ${-cell} ${span + 2 * cell} ${span + 2 * cell}`}
      className="w-full max-w-md rounded bg-amber-300"
    >
      {lines.map((p) => (
        <g key={p} stroke="black" strokeWidth={1}>
          <line x1={0} y1={p} x2={span} y2={p} />
          <line x1={p} y1={0} x2={p} y2={span} />
        </g>
      ))}
      {board.map((column, x) =>
        column.map((content, y) => (
          <g key={`${x},${y}`}>
            {content !== 'Empty' && (
              <circle
                cx={x * cell}
                cy={y * cell}
                r={cell * 0.46}
                fill={content === 'Black' ? 'black' : 'white'}
                stroke="black"
              />
            )}
            {lastMove?.x === x && lastMove?.y === y && (
              <circle cx={x * cell} cy={y * cell} r={cell * 0.15} fill="red" />
            )}
            {content === 'Empty' && !disabled && (
              <rect
                x={x * cell - cell / 2}
                y={y * cell - cell / 2}
                width={cell}
                height={cell}
                fill="transparent"
                className="cursor-pointer"
                onClick={() => onPlay({ x, y })}
              />
            )}
          </g>
        )),
      )}
    </svg>
  )
}
