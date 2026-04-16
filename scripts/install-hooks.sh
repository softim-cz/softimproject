#!/usr/bin/env bash
# Register .githooks/ as the hook path for this clone.
# Run once after cloning the repo.

set -e
cd "$(git rev-parse --show-toplevel)"
git config core.hooksPath .githooks
chmod +x .githooks/pre-push 2>/dev/null || true
echo "Installed. Hooks will now run from .githooks/"
echo "Bypass a single push (discouraged) with: git push --no-verify"
