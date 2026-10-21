#!/usr/bin/env python3
"""Reject SDK releases missing contracts used by this example; not a C# compiler."""

import argparse
import json
from pathlib import Path
import re


# These newer contracts are used directly by the game and its executable smoke
# checks. Keep this gate bounded: overload compatibility and runtime behaviour
# still require a real Unity import, build and player run.
REQUIRED_TYPES = {
    "PlayerValuesManager", "PlayerValuesResult", "PlayerValuesOffersResult",
    "PlayerValuesState", "PlayerValueAmount", "PlayerValueOffer",
    "StatisticsManager", "StatisticsSnapshot", "StatisticDefinition",
    "StatisticUpdate", "UpdateStatisticsRequest", "ValidatedActionsManager",
    "ValidatedRun", "ValidatedSubmitResult", "ValidatedEvidenceFailure",
    "EvidenceUploadResult", "PlayerState", "PlayerStateValue", "EarnedValue",
}
REQUIRED_METHODS = {
    "PlayerValuesManager": {"GetState", "GetOffers", "Purchase", "Adjust"},
    "StatisticsManager": {"GetStatistics", "UpdateStatistics"},
    "UserManager": {"HasCachedAnonymousToken", "RestoreAnonymousSession"},
    "ValidatedActionsManager": {"StartRun", "SubmitValidated", "DiscardRun"},
}


def code_only(source):
    # Ignore comments and ordinary/verbatim strings when looking for declarations.
    return re.sub(r'@"(?:""|[^"])*"|"(?:\\.|[^"\\])*"|//[^\n]*|/\*.*?\*/',
                  " ", source, flags=re.DOTALL)


def verify(root, sdk):
    package = json.loads((sdk / "package.json").read_text())
    if package.get("name") != "com.projectmakers.horizon":
        raise ValueError("Expected the official com.projectmakers.horizon package")
    game_files = list((root / "Assets").rglob("*.cs"))
    if not game_files:
        raise ValueError("Example C# sources are missing")
    game = "\n".join(code_only(p.read_text()) for p in game_files)
    declared = {}
    for path in (sdk / "CloudSDK").rglob("*.cs"):
        source = code_only(path.read_text())
        for name in re.findall(r'\bpublic\s+(?:(?:sealed|static|abstract|partial)\s+)*'
                               r'(?:class|struct|enum|interface)\s+(\w+)', source):
            declared.setdefault(name, []).append(source)
    missing = []
    required = {name for name in REQUIRED_TYPES if re.search(r'\b' + name + r'\b', game)}
    for name in sorted(required):
        if name not in declared:
            missing.append(name)
    method_count = 0
    for owner, methods in REQUIRED_METHODS.items():
        for method in sorted(methods):
            if not re.search(r'\b' + method + r'\s*\(', game):
                continue
            method_count += 1
            # This checks a public declaration, not a comment, call site or string.
            pattern = (r'\bpublic\s+(?:(?:async|static|virtual|override|new|sealed)\s+)*'
                       r'[\w.<>,?\[\]\s]+\b' + method + r'\s*\(')
            if not any(re.search(pattern, source) for source in declared.get(owner, [])):
                missing.append(owner + "." + method)
    if missing:
        raise ValueError("SDK is incompatible with the example; missing public contracts: "
                         + ", ".join(missing))
    print(f"SDK source guard passed: {len(required)} required types, {method_count} public methods. "
          "Native Unity validation remains a separate gate.")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--sdk-source", required=True, type=Path)
    args = parser.parse_args()
    try:
        verify(Path(__file__).resolve().parents[1], args.sdk_source.resolve())
    except (ValueError, OSError) as error:
        parser.exit(1, str(error) + "\n")
