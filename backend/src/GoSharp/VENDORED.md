# Vendored: GoSharp

Source files in this project are copied from [paviad/GoSharp](https://github.com/paviad/GoSharp)
(`GoSharpCore/`), commit `d8ad09ab00e8cb82360e0417fda148b43b74c9e1` (2023-06-23), the last
upstream commit as of vendoring (2026-07-30). MIT licensed — see `LICENSE`.

Not published on NuGet: the `GoSharp` package name on nuget.org belongs to an unrelated
library (a concurrency DSL by a different author), so this is vendored as source rather
than referenced as a package.

Namespace kept as `GoSharpCore` (unchanged from upstream) to make clear this is vendored,
not project code. `sgf.g` (a grammar reference doc, not used by the build — the SGF parser
is hand-written) and test/console projects were not copied, only the library source itself.
