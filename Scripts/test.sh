#!/bin/zsh
set -euo pipefail
cd "${0:A:h:h}"
export CLANG_MODULE_CACHE_PATH="${TMPDIR:-/private/tmp}/miracle-clang-cache"
export SWIFTPM_MODULECACHE_OVERRIDE="${TMPDIR:-/private/tmp}/miracle-module-cache"
swift test --disable-sandbox --cache-path "${TMPDIR:-/private/tmp}/miracle-spm-cache" -Xswiftc -module-cache-path -Xswiftc "$SWIFTPM_MODULECACHE_OVERRIDE" "$@"
