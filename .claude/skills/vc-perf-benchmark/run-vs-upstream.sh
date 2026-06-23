#!/usr/bin/env bash
# run-vs-upstream.sh — "client override vs stock": run the SAME benchmark on THIS module's stock runner
# and on a CLIENT PROJECT's override runner, and emit the overhead verdict via compare-reports.cs.
# Answers "how much overhead does a client project's override add over this module's stock path?"
#
# Uses --match method: the two runners' namespaces and class names differ by design (a client project's
# runner subclasses into its own namespace, often with a prefix), so only the operation + workload params
# coincide — matching on FullName would find nothing. compare-reports.cs reports THIS module's stock side
# as baseline and the client side as current, so an alloc/time ratio > 1 is the client override's overhead.
#
# Validity: compare FULL operations, not isolated overridden methods. An overridden method reimplemented
# differently is two different operations, not an overhead delta. Filter to full mutations / commands.
#
# Usage:
#   run-vs-upstream.sh <cart|order> --client-dir <path> [--filter <pattern>] [--categories <c1,c2,...>]
#                      [--job dry|short|default] [--alloc-threshold <pct>] [--time-threshold <pct>]
#
#   --client-dir   REQUIRED. Path to the client project's benchmark runner dir (the override side), e.g.
#                  /path/to/client-project/benchmarks/ClientProject.Benchmark.Cart.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

usage() {
    cat >&2 <<'USAGE'
Usage:
  run-vs-upstream.sh <cart|order> --client-dir <path> [--filter <pattern>] [--categories <c1,c2,...>]
                     [--job dry|short|default] [--alloc-threshold <pct>] [--time-threshold <pct>]

  cart|order      which domain to compare (this module's stock runner vs the client override runner).
  --client-dir    REQUIRED. The client project's benchmark runner dir (the override side).
  --filter        BenchmarkDotNet filter (default '*'). Prefer full operations over isolated methods.
  --categories    Comma-separated BenchmarkCategory names (e.g. items,configuration) → BDN
                  --anyCategories. Scope to an AREA. Composes with --filter (intersection).
  --job           dry (smoke, default) | short | default. Only `default` lets the TIME axis gate.

  SCOPE: prefer --filter (one operation) or --categories (one area). Do NOT run the full suite ('*')
  in the optimization loop — it is ~13h measured. Measure only what your change touches.
USAGE
}

if [[ $# -lt 1 ]]; then
    usage
    exit 2
fi

DOMAIN="$1"
shift

FILTER='*'
CATEGORIES=()
JOB='dry'
CLIENT_DIR=''
COMPARE_EXTRA=()

while [[ $# -gt 0 ]]; do
    case "$1" in
        --filter) FILTER="$2"; shift 2 ;;
        --categories) IFS=',' read -ra CATEGORIES <<< "$2"; shift 2 ;;
        --job) JOB="$2"; shift 2 ;;
        --client-dir) CLIENT_DIR="$2"; shift 2 ;;
        --alloc-threshold|--time-threshold) COMPARE_EXTRA+=("$1" "$2"); shift 2 ;;
        -h|--help) usage; exit 2 ;;
        *) echo "Unknown argument: $1" >&2; usage; exit 2 ;;
    esac
done

# Per-category scoping: --anyCategories selects benchmarks tagged with ANY listed category; composes
# with --filter (AND). The default --filter '*' leaves category as the only narrowing.
CAT_FLAGS=()
[[ ${#CATEGORIES[@]} -gt 0 ]] && CAT_FLAGS=(--anyCategories "${CATEGORIES[@]}")

REPO="$(git -C "$SCRIPT_DIR" rev-parse --show-toplevel)"

case "$DOMAIN" in
    cart)  STOCK_DIR="$REPO/benchmarks/VirtoCommerce.XCart.Benchmark" ;;
    order) STOCK_DIR="$REPO/benchmarks/VirtoCommerce.XOrder.Benchmark" ;;
    *)
        echo "Domain must be 'cart' or 'order', got '$DOMAIN'." >&2; exit 2 ;;
esac

if [[ -z "$CLIENT_DIR" ]]; then
    echo "--client-dir is required: path to the client project's benchmark runner dir (the override side)." >&2
    exit 2
fi
if [[ ! -d "$CLIENT_DIR" ]]; then
    echo "Client runner dir not found: $CLIENT_DIR" >&2
    exit 2
fi
if [[ ! -d "$STOCK_DIR" ]]; then
    echo "This module's stock runner not found: $STOCK_DIR" >&2
    exit 2
fi

# Job → run flags + compare-reports.cs --job-kind. Both runners take native BenchmarkDotNet --job
# (Dry default; only `default` lets the TIME axis gate the verdict), so there is no dialect split.
case "$JOB" in
    dry)    JOB_FLAGS=(--job Dry);   JOB_KIND=dry ;;
    short)  JOB_FLAGS=(--job Short); JOB_KIND=short ;;
    default|measured) JOB_FLAGS=(); JOB_KIND=measured ;;
    *) echo "--job must be dry|short|default, got '$JOB'." >&2; exit 2 ;;
esac

STOCK_JSON="$(mktemp --suffix=.json)"
CLIENT_JSON="$(mktemp --suffix=.json)"

run_one() { # $1 = runner dir, $2 = output json, $3 = label
    local dir="$1" out="$2" label="$3"
    echo "[vs-stock] running $label ($dir)..." >&2
    (
        cd "$dir"
        rm -rf BenchmarkDotNet.Artifacts
        dotnet run -c Release -- "${JOB_FLAGS[@]}" --filter "$FILTER" "${CAT_FLAGS[@]}" --exporters json
    ) >&2
    cp "$dir/BenchmarkDotNet.Artifacts/results/"*-report-full-compressed.json "$out"
}

echo "[vs-stock] domain=$DOMAIN job=$JOB filter='$FILTER' categories='${CATEGORIES[*]}'" >&2
run_one "$STOCK_DIR" "$STOCK_JSON" "stock (baseline)"
run_one "$CLIENT_DIR" "$CLIENT_JSON" "client override (current)"

# compare-reports.cs exit 1 = regression (the client override's overhead exceeds threshold) — a valid verdict.
set +e
dotnet run "$SCRIPT_DIR/compare-reports.cs" -- "$STOCK_JSON" "$CLIENT_JSON" --match method --job-kind "$JOB_KIND" "${COMPARE_EXTRA[@]}"
rc=$?
set -e
exit "$rc"
