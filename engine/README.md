# Engine notes

Why this service's KataGo setup looks the way it does — both `config/go_training_platform_config.cfg` and the move-selection code in `src/Engine.Api/Analysis/` — and the research behind it.

## Where this runs

Every value in the file is tuned for one target:

- **Eigen backend** — KataGo's pure-CPU build. There is no graphics card.
- **4 vCPU**, with a fixed container memory budget of **8 GB**.
- **Azure Container Apps, Consumption plan, scaling to zero** when idle.

Consequences:

- **Search is expensive without a graphics card.** On CPU alone, every bit of extra search costs real latency.
- **Memory is tight, and this project loads two neural networks at once.** That roughly doubles both the memory footprint and the load time versus a single-network setup.
- **Scaling to zero means the model load is paid on a cold start**, in front of a waiting user — which is what the `/warmup` endpoint exists to trigger ahead of time.

## Background

**KataGo can answer in two modes.** It can glance at the board and answer instantly from pattern recognition, or it can *think ahead* — playing out possible continuations and seeing how they turn out. Thinking ahead is called **search**, and the amount of it is measured in **visits**: one visit means one position examined, so `maxVisits = 1` means no thinking ahead at all, just the instant answer.

When KataGo answers, it can return:

- **`policy`** — the instant answer. A list of numbers, one for every point on the board plus one for "pass", scoring how promising each is. Available immediately.
- **`moveInfos`** — the results of thinking ahead. Detailed and search-informed, covering moves the search actually looked at.

This project reads `policy` and ignores `moveInfos` entirely. In other words, no search is used for any model or bot difficulty (more on this below).

**There are two different neural networks**:

| | Self-play network | Human network |
|---|---|---|
| Learned from | Playing millions of games against itself | Records of real human games |
| Goal | Maximize probability of winning | Play like a human of a given rank |
| Answers with | The strongest move it knows | The move a human at that rank would likely pick — mistakes included |

The human network is told which rank to imitate through a setting called `humanSLProfile`, e.g. `rank_20k` for a beginner or `rank_9d` for a strong amateur. See [Go ranks and ratings](https://en.wikipedia.org/wiki/Go_ranks_and_ratings) for more info on ranks.

## What the research found

### One visit is already strong enough

A self-play network with **no search** has historically been measured at roughly **8d on KGS** — very strong amateur — and one estimate puts the latest network near weak-professional level. That already beats the overwhelming majority of players, which is what made "no search anywhere" an option for this app rather than a compromise.

### Adding search would break the ranked bots

The human network is calibrated to match a given rank **only when it is answering instantly and its answer is used in a specific way**. Once you add search, it starts playing *stronger than the rank on the label*, because search finds tactics a player at that level would have missed. A 10-kyu bot that never blunders is not a 10-kyu bot.

So the two goals pull apart, and cannot be served by one tunable knob:

- a **strong, superhuman-level** opponent → self-play network, add search
- a **realistic ranked** opponent → human network, no search, and pick moves the way described below

### How to pick a move

Given the policy list of scores, there are two ways to choose:

- **Take the highest (argmax).** This is what you want when the goal is the best move available.
- **Pick at random, weighted by the scores.** A move scored twice as high gets picked twice as often, but the lower-scored ones still come up sometimes. This is what makes a ranked bot occasionally play a weaker move, the way a human of that rank would.

The human network's rank calibration assumes the second method. Using argmax would produce a bot that is not realistic.

### The top of the ranked ladder is the least trustworthy part

The human network's rank labels are **categories from its training data, not verified strength ratings**. Rank scales differ between servers and eras, so its "9d" is not the same yardstick as the 8d KGS figure above. KataGo's own developers note that the rank match gets less accurate toward the high end.

**Inference, not a measured result:** `rank_9d` with no search is probably *weaker* in practice than the self-play network with no search, despite the label suggesting otherwise — it is optimizing for being humanlike, not for winning. No independent strength test of the human network was found, only the developers' own calibration notes.

### Known blind spots without search

Researchers probing for weaknesses found that a no-search KataGo can misjudge whether certain groups are alive, and can be tricked into passing when it shouldn't. These are legal moves that are simply bad, not rule violations, and even a little search removes them. Accepted as an edge case for now.

### Illegal moves are already marked

The `policy` list covers *every* point, including ones that would be illegal to play. KataGo marks those with a score of **`-1`**, computed against the ruleset in the request.

## How that lands in the config and the code

Two lanes, one code path, switching network and selection method:

| | Superhuman and player hints | Ranked `Kyu1`–`Kyu20`, `Dan1`–`Dan9` |
|---|---|---|
| Network | self-play | human |
| Scores read from | `policy` | `humanPolicy` |
| Move chosen by | argmax | weighted random pick, `-1` entries excluded |
| `humanSLProfile` sent? | no — omitted entirely | yes: `rank_20k` … `rank_9d` |

Player hints deliberately ignore the game's own bot strength: a hint should be the best move available, not a rank-appropriate one.

`maxVisits = 1` in the config is the *default* for requests that don't state their own.

## The remaining settings

The rest of the config file follows from the environment above rather than from the research:

- **`nnCacheSizePowerOfTwo`** caps a cache of already-evaluated positions. The example config's default works out to roughly 12.9 GB — more than the whole 8 GB container — so it is set to 2^18, about 390 MB.
- **`numEigenThreadsPerModel`** sets how many CPU cores evaluate the network. It does *not* default from the core count; left alone it would have collapsed to 1 and left three of the four vCPUs idle. It is set explicitly to 4.

The config file's inline comments show the arithmetic for both.

## Open questions

- **The cache size is a guess**, pending real measurement of memory use once everything is running.
- **The top of the ranked ladder is unverified.** If `rank_9d` plays too weakly against real users, the fix is tuning against play-testing feedback, since there is no benchmark to calibrate against.
- **KataGo can do the weighted random pick itself** (`humanSLChosenMoveProp`, `humanSLChosenMovePiklLambda` — see its `gtp_human5k_example.cfg` and `gtp_human9d_search_example.cfg`). This project does it in `src/Engine.Api/Analysis/` instead. Worth revisiting if the two ever disagree.
- **The `preaz_*` and `proyear_*` profiles are unused.** They select opening style and historical era rather than strength, and are there if style ever matters.
- **Randomness is unseeded.** The weighted random pick makes ranked bots non-deterministic; reproducible bot behavior would need a seeding strategy.
