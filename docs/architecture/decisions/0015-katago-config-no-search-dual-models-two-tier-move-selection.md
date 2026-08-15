# 15. KataGo config: no search, dual models, two-tier move selection

Date: 2026-08-14

## Status

Accepted

## Context

The deployment target for our engine microservice (ADR 2, ADR 9) is a CPU-only, scale-to-zero platform with limited cores and memory, so we run KataGo on its Eigen backend rather than a GPU backend. Search (many forward passes through the network per query, to build a move tree) is far too slow on CPU-only hardware at any bot strength.

However, even without search, KataGo's self-play network's raw policy output — its move prediction with zero tree search, just one "visit" — plays far better than you might expect. Informal testing has had 1-visit play hold around **8 dan** on KGS, which is strong enough to beat the overwhelming majority of human players.

Separately, the product needs two different kinds of play: a single strong, reliable "best move" for hints and the strongest bot tier, and a range of human-like bots at varying levels that play *plausibly*, not optimally.

The obvious first idea for the weaker tiers was to simply weaken the same self-play network directly — fewer visits, a time limit, less compute — rather than reach for a second model. This turns out to be naive. The self-play network's raw, un-searched policy already plays around 8d KGS (see above), so there's no floor to hit by limiting resources further: even zero search is already far stronger than a beginner or intermediate player, so resource-limiting alone can't reach those levels. Artificially degrading the network further (adding noise, restricting to suboptimal moves) produces play that's worse in ways that don't resemble how a human at that level actually thinks.

KataGo instead ships a second, separate network called the "Human SL" (supervised-learning) model — trained differently from the self-play network. Where the self-play network learns purely by playing against itself and optimizing to win (AlphaZero-style reinforcement learning), the Human SL network trains on real human game records, learning to predict what a human player *would* do rather than what an optimal move is. Picking a rank is also a single categorical `humanSLProfile` setting KataGo ships pre-trained, rather than something we'd need to calibrate ourselves.

That range spans beginner (20 kyu) to strong dan (9 dan). The rank labels are training-data categories — the model was trained to *imitate* move distributions from games tagged at each rank — not validated strength benchmarks the way the self-play network's ~8d KGS figure is. The model was calibrated using 1 visit, sampling proportionally from the policy array, no search. Even so, KataGo's own developers note that this calibration degrades at mid-to-high dan, meaning the top-end profiles (like 9d) are the *least* reliable part. It's very likely the 9d Human SL profile, at no search, plays weaker in practice than the self-play network at no search (which historically held 8d on KGS) — despite the label suggesting otherwise.

## Decision

We will run KataGo with no search (`maxVisits = 1`) anywhere, for any strength, including the top tier (self-play network). We will load two neural networks simultaneously — a strong self-play network and a human-supervised-learning network. The top tier bot responses and player hints will pick the strongest move (argmax) from the self-play network's policy output, while ranked bots will sample proportionally from the human network's policy output

**Reasons:**

- Search-based play is far too slow on this CPU-only hardware at any bot strength, and no-search play is strong enough to be worth serving anyway (see Context).
- The product needs two different kinds of play (reliable best-move vs. plausible human-like mistakes).
- The human-supervised-learning network is specifically calibrated at 1 visit with proportional sampling. Adding search makes it play *stronger* than its target rank.

## Consequences

**Positive:**

- Every response — top tier or ranked bot — resolves in a single forward pass with no search, so compute and latency stays predictable and cheap.
- Rank selection is a config value (`humanSLProfile`), not something we calibrate or maintain ourselves.
- Both models fit the "no search" constraint uniformly — one code path handles inference for every tier, just switching which network and sampling strategy it uses.

**Negative:**

- Two networks loaded simultaneously roughly doubles memory footprint and model load time versus a single-network setup.
- The top tier is capped at whatever 1-visit self-play policy actually plays at (~8d KGS per informal testing), not KataGo's full strength. This is a ceiling we're accepting for now.
- The top end of the ranked ladder (per Context, likely 9d) is the least trustworthy part of the system: it's plausibly *weaker* than its label suggests, and we have no independent benchmark for it.
- If either network's behavior needs correction later (e.g. 9d feels too weak in practice), the fix is empirical tuning against play-testing feedback.
