# Goals & Non-Goals

## Vision

Play Go (Baduk/Weiqi) against bots of varying strength, with optional live hints and undo, powered by KataGo.

## Goals (v1 / MVP)

- Play a full game of Go against a KataGo-powered bot on a single board size (9x9 first)
- Choose bot strength before starting a game (limit KataGo visits/playouts)
- Correct core rules enforcement: legal moves, captures, ko, pass, resign, Chinese scoring.
- Undo: retract your last move (and the bot's response) during a game
- Live hint: ask the engine for a single suggested move and a win-rate estimate at any point during play
- User accounts (register/login) so a game can be saved and resumed later
- Game persistence (Postgres): move history stored, at least one in-progress game per user resumable after refresh/logout

## Stretch goals (post-v1, explicitly not blocking v1)

- Additional board sizes (13x13, 19x19)
- Post-game review mode: step through a finished game move-by-move with engine analysis
- Time controls (byo-yomi / Fischer increment)
- Public shareable link to a completed game
- Basic rating/progress tracking across games (e.g. win rate vs. a given bot strength over time)

## Non-goals (explicitly out of scope)

- Player-vs-player over network (bots only)
- Mobile native apps (web only)
- Tournament management / matchmaking
- Chat or other social features
- Teaching content beyond in-game hints (no joseki dictionary, no interactive lessons)
- Supporting multiple rulesets (only Chinese)
- Production-grade scaling/HA — this is a learning/portfolio project, not a product with real users
