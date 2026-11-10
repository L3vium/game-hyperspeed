#!/usr/bin/env bash
# Release : merges dev into main as a real merge commit (main keeps its own lane in Fork) and names it with a version tag.
# Never checks out main, so the open Unity project is not reimported.
#
#   Tools/release.sh "What changed"            -> next minor version (v0.9.0 -> v0.10.0)
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
if [ "$DEV" = "$MAIN" ]; then
    echo "dev and main are the same commit : nothing to release." >&2
    exit 1
fi
# main may hold earlier release merges that dev does not : fine, as long as main has no content dev lacks.
BASE=$(git merge-base main dev)
if ! git diff --quiet "$BASE" main; then
    echo "main has changes that dev does not have : merge main into dev first." >&2
    exit 1
fi

if [ -z "$VERSION" ]; then
    LAST=$(git tag --list 'v*' --sort=-v:refname | head -n 1)
    LAST=${LAST:-v0.0.0}
    IFS=. read -r MAJOR MINOR PATCH <<< "${LAST#v}"
    case "$BUMP" in
        major) MAJOR=$((MAJOR + 1)); MINOR=0; PATCH=0 ;;
        patch) PATCH=$((PATCH + 1)) ;;
        *)     MINOR=$((MINOR + 1)); PATCH=0 ;;
    esac
    VERSION="v$MAJOR.$MINOR.$PATCH"
fi
if git rev-parse -q --verify "refs/tags/$VERSION" >/dev/null; then
    echo "tag $VERSION already exists." >&2
    exit 1
fi

# main has nothing dev lacks : the merged tree is dev's tree, with both as parents.
TITLE="Release $VERSION : $SUMMARY"
MERGE=$(git commit-tree "dev^{tree}" -p main -p dev -m "$TITLE" -m "Merge branch 'dev' into main.")
git update-ref refs/heads/main "$MERGE" "$MAIN"
git tag -a "$VERSION" "$MERGE" -m "$TITLE"
git push -q origin main "$VERSION"
echo "$TITLE"
echo "main -> $(git rev-parse --short main), tag $VERSION pushed."
