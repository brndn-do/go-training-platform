import { apiFetch } from '../../shared/api/client'

export type Color = 'Black' | 'White'
export type Content = 'Empty' | Color
export type Outcome = 'PlayerResigned' | 'BotResigned' | 'TwoConsecutivePasses'
export type Coordinates = { x: number; y: number }

export type Game = {
  id: string
  playerColor: Color
  botColor: Color
  turn: Color
  boardSize: number
  komi: number
  botStrength: string
  outcome: Outcome | null
  /** Indexed board[x][y], not row-major. */
  board: Content[][]
  /** Coordinates null means that move was a pass. */
  lastMove: { coordinates: Coordinates | null; moveNumber: number } | null
  suggestion: { coordinates: Coordinates | null; blackWinRate: number } | null
}

export type StartGameRequest = { playerColor: Color; boardSize: number; botStrength: string }

const post = (path: string, body?: unknown) => apiFetch<Game>(path, { method: 'POST', body })

export const startGame = (request: StartGameRequest) => post('/api/games', request)
export const getGame = (id: string) => apiFetch<Game>(`/api/games/${id}`)
export const playMove = (id: string, at: Coordinates) => post(`/api/games/${id}/moves`, at)
export const pass = (id: string) => post(`/api/games/${id}/pass`)
export const resign = (id: string) => post(`/api/games/${id}/resign`)
