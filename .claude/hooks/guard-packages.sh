#!/usr/bin/env bash
# PreToolUse (Bash|PowerShell): любая shell-команда, упоминающая файлы пакетов,
# требует подтверждения пользователя. Правки через Edit/Write ловит permissions.ask.
input=$(cat)
if printf '%s' "$input" | grep -qiE 'Packages[/\]+(manifest|packages-lock)\.json|(^|[^a-z-])(manifest|packages-lock)\.json'; then
  printf '%s\n' '{"hookSpecificOutput":{"hookEventName":"PreToolUse","permissionDecision":"ask","permissionDecisionReason":"Команда трогает файлы пакетов Unity (manifest.json / packages-lock.json). По package-policy нужно согласие пользователя."}}'
fi
exit 0
