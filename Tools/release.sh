#!/usr/bin/env bash
# Release : merges dev into main as a real merge commit (main keeps its own lane in Fork), named "Release vX.Y.Z : ...".
# The version lives in the merge commit's name only, no tags. Never checks out main, so the open Unity project is not
# reimported.
#
#   Tools/release.sh "What changed"            -> next minor version (v0.10.0 -> v0.11.0)
#   Tools/release.sh "What changed" v1.0.0     -> explicit version
#   Tools/release.sh "What changed" "" major   -> next major (v0.10.0 -> v1.0.0) ; "patch" for v0.10.1
set -euo pipefail

SUMMARY="${1:-}"
VERSION="${2:-}"
BUMP="${3:-minor}"
if [ -z "$SUMMARY" ]; then
    echo "usage: Tools/release.sh \"What changed\" [version] [minor|major|patch]" >&2
    exit 1
fi

cd "$(git rev-parse --show-toplevel)"
git fetch -q origin main dev 2>/dev/null || true

DEV=$(git rev-parse dev)
MAIN=$(git rev-parse main)
if git merge-base --is-ancestor dev main; then
    echo "main already has everything on dev : nothing to release." >&2
    exit 1
fi
# main holds earlier release merges that dev does not : fine, as long as main has no content dev lacks.
BASE=$(git merge-base main dev)
if ! git diff --quiet "$BASE" main; then
    echo "main has changes that dev does not have : merge main into dev first." >&2
    exit 1
fi

# Last version : from the newest "Release vX.Y.Z" on main's own line.
LAST=$(git log main --first-parent --format=%s | grep -m 1 -oE '^Release v[0-9]+\.[0-9]+\.[0-9]+' | sed 's/^Release //' || true)
LAST=${LAST:-v0.0.0}
if [ -z "$VERSION" ]; then
    IFS=. read -r MAJOR MINOR PATCH <<< "${LAST#v}"
    case "$BUMP" in
        major) MAJOR=$((MAJOR + 1)); MINOR=0; PATCH=0 ;;
        patch) PATCH=$((PATCH + 1)) ;;
        *)     MINOR=$((MINOR + 1)); PATCH=0 ;;
    esac
    VERSION="v$MAJOR.$MINOR.$PATCH"
fi
if git log main --first-parent --format=%s | grep -q "^Release $VERSION "; then
    echo "$VERSION was already released." >&2
    exit 1
fi

# The merged tree is dev's tree, with main and dev as parents.
TITLE="Release $VERSION : $SUMMARY"
MERGE=$(git commit-tree "dev^{tree}" -p main -p dev -m "$TITLE" -m "Merge branch 'dev' into main.")
git update-ref refs/heads/main "$MERGE" "$MAIN"
git push -q origin main
echo "$TITLE"
echo "main -> $(git rev-parse --short main) (previous $LAST), pushed."
