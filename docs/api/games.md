# The games API

| Method | Route                    | Notes                                                               |
| ------ | ------------------------ | ------------------------------------------------------------------- |
| `POST` | `/api/games`             | 201 + `Location`. Plays the bot's opening move if the bot is Black. |
| `GET`  | `/api/games/{id}`        | Pure read. Does not advances the bot, Does not return a suggestion. |
| `POST` | `/api/games/{id}/resume` | Read that _does_ advance the bot if it is the bot's turn.           |
| `POST` | `/api/games/{id}/moves`  | `{ x, y }`. Plays the human move, then the bot's reply.             |
| `POST` | `/api/games/{id}/pass`   | Two consecutive passes end the game.                                |
| `POST` | `/api/games/{id}/undo`   | Retracts the human's last move and the bot's reply to it.           |
| `POST` | `/api/games/{id}/resign` | Ends the game immediately.                                          |

## Things to build against

- Every endpoint under `/api/games` has the same `GameResponse` shape in the body.
- `GameResponse` carries `id`, `playerColor`, `botColor`, `turn`, `boardSize`, `komi`, `botStrength`, `outcome`, `board`, `lastMove`, `suggestion`.
- The server is authoritative on the board. Every response ships the full board — render what came back, do not derive it.
- `board` is indexed `board[x][y]`, not row-major, and is jagged rather than rectangular.
- Enums arrive as strings (`"Black"`, `"Kyu20"`, `"TwoConsecutivePasses"`), not numbers.
- `outcome: null` means in progress.
- `lastMove` is normally the bot's reply\*\*, not the human's. `lastMove.coordinates: null` means that move was a pass.
- `suggestion` is the hint for the human's next decision, always computed at Superhuman strength regardless of the game's `botStrength`. It is `null` on `GET` and on a rejected action. `blackWinRate` is always Black's, whichever color the player is.
- A `409` means reload, not retry. Re-fetch the game; do not resend. The body carries no game state.
- `400` on a rejected action carries no reason. Errors otherwise arrive as RFC 9457 problem details.
- A `404` covers both "no such game" and "not yours".
- Board sizes accepted are 9, 13, and 19.

## Note

The backend acts as one fixed player id from `CurrentPlayer__Id` in `.env`. There is no login flow to build against yet.
