# CLAUDE.md — docs

**`GOALS.md`** — the source of truth for scope, stretch goals, and explicit non-goals.

**`api/`** — API documentation.

**`architecture/decisions/`** — ADRs. The source of truth for _why_ a technology or structural choice was made, including decisions not yet reflected in code.

`.adr-dir` at the repo root points here, for `adr-tools`.

## ADRs

- ADRs are for **big architecture decisions** — ones that would be costly to reverse.
- ADRs shouldn't include specific implementation details or minor chores—they should capture our design decisions and larger, higher-level work that requires real planning, not a how-to guide.
- **Context is neutral.** It presents the problem without smuggling in the outcome. The context should not read like the decision is settled before the Decision section commits to it. It includes a phrase along the lines of, **"We need to decide"** followed by the problem or question at the end of the section.
- Options get their own **"Options Considered"** section, after Context. Where possible, options should be kept mutually exclusive (only one can be chosen), unless doing so would mean mapping out every combination of smaller options and produce too many choices. If the options end up not being mutually exclusive, add a short note stating that. If the options aren't tightly coupled, consider splitting them into separate ADRs instead. If the options _are_ mutually exclusive, do not add a note; that is the presumed default.
- The Decision section starts with **"We will"**, followed by a **Reasons** subsection — the specific factors that tipped it, not a restatement of Context.
- Consequences splits into **Positive** and **Negative**.
- Decision and Consequences use future tense.
- A rejected option still gets its own file with `Status: Rejected`. Add a short note above Context saying what was rejected and why, and pointing to whatever ADR supercedes it.
