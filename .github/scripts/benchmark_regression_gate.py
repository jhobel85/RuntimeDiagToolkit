#!/usr/bin/env python3
"""
Benchmark regression gate: compare current results against baseline.
Fails with exit code 1 if any metric regresses > threshold.
"""

import json
import sys
import os
import argparse

def main():
    parser = argparse.ArgumentParser(description="Compare benchmark results against baseline")
    parser.add_argument("--results", required=True, help="Path to benchmark results JSON file")
    parser.add_argument("--baseline", default="benchmark-baselines.json", help="Path to baseline JSON file")
    parser.add_argument("--threshold", type=float, default=0.10, help="Regression threshold (default 10%)")
    args = parser.parse_args()

    results_file = args.results
    baseline_file = args.baseline
    regression_threshold = args.threshold

    if not os.path.exists(results_file):
        print(f"❌ Benchmark results file not found: {results_file}")
        return 1

    try:
        with open(results_file) as f:
            results = json.load(f)
    except Exception as e:
        print(f"❌ Failed to load results: {e}")
        return 1

    # Load baseline if it exists
    baseline = {}
    if os.path.exists(baseline_file):
        try:
            with open(baseline_file) as f:
                baseline = json.load(f)
        except Exception as e:
            print(f"⚠️  Warning: Could not load baseline: {e}")

    regressions = []
    improvements = []
    new_benchmarks = []

    # Process benchmark results
    for item in results.get("benchmarks", []):
        method = item.get("method")
        mean = float(item.get("mean", 0))

        if method in baseline:
            baseline_mean = baseline[method]
            if baseline_mean == 0:
                change = 0
            else:
                change = (mean - baseline_mean) / baseline_mean

            if change > regression_threshold:
                regressions.append(
                    f"{method}: {change*100:.1f}% regression "
                    f"(baseline: {baseline_mean:.0f}ns, current: {mean:.0f}ns)"
                )
            elif change > 0:
                improvements.append(
                    f"{method}: {change*100:.1f}% slower (within threshold)"
                )
            else:
                improvements.append(
                    f"{method}: {abs(change)*100:.1f}% faster (improvement)"
                )
        else:
            new_benchmarks.append(f"{method}: new benchmark (mean: {mean:.0f}ns)")

    # Print results
    print("\n" + "="*70)
    print("BENCHMARK COMPARISON REPORT")
    print("="*70)

    if improvements:
        print("\n✓ Improvements/Within Threshold:")
        for msg in improvements:
            print(f"  {msg}")

    if new_benchmarks:
        print("\n+ New Benchmarks:")
        for msg in new_benchmarks:
            print(f"  {msg}")

    if regressions:
        print("\n❌ REGRESSIONS DETECTED (> {:.0f}% threshold):".format(regression_threshold * 100))
        for msg in regressions:
            print(f"  {msg}")
        print("\n" + "="*70)
        return 1

    print("\n✓ All benchmarks within acceptable threshold")
    print("="*70 + "\n")
    return 0

if __name__ == "__main__":
    sys.exit(main())
