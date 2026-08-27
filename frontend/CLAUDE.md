# CLAUDE.md — frontend

React single-page app built with Vite.

**Status: scaffold only.**

## Structure

Feature-based, not type-based:

```
src/
  features/<feature>/    added incrementally — one folder per feature, currently empty
  shared/
    components/          generic reusable UI
    hooks/
    api/                 base fetch client, etc.
```

Anything specific to one feature lives with that feature. `shared/` is for what genuinely crosses features — resist promoting things there early.

## Commands

From `frontend/`:

```
npm install
npm run dev       # Vite dev server, http://localhost:5173
npm run build     # tsc -b && vite build
npm run lint      # oxlint
```

The frontend is **not** a `docker-compose` service — run it locally.

## Tooling

- **React 19**, **TypeScript**, **Vite**.
- **Tailwind CSS v4** via `@tailwindcss/vite`, CSS-first config: `src/index.css` is just `@import "tailwindcss";`. There is no `tailwind.config.js`; customization goes in CSS via `@theme`.
- **oxlint**, not ESLint (`.oxlintrc.json`, `react`/`typescript`/`oxc` plugins).
- `VITE_API_BASE_URL` comes from the root `.env` (`http://localhost:5000` locally). Vite only exposes vars prefixed `VITE_`.

## The API it consumes

See [docs/api/games.md](../docs/api/games.md).
