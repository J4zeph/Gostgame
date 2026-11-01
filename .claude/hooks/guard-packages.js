// PreToolUse-хук: защита файлов пакетов Unity (Packages/manifest.json, Packages/packages-lock.json).
// - Shell-команды (Bash/PowerShell), обращающиеся к этим файлам: запрет.
// - Edit/Write/MultiEdit этих файлов: вопрос пользователю со списком изменений пакетов.
// Причину изменения агент пишет в чате перед запросом (CLAUDE.md, скилл package-policy).
const fs = require('fs');
const path = require('path');

const PACKAGE_FILE = /(^|[\\/])Packages[\\/]+(manifest|packages-lock)\.json$/i;
const PACKAGE_PATH = /Packages[\\/]+(manifest|packages-lock)\.json/i;
const PACKAGE_DIR = /(^|[\s"'`=;&|(])Packages([\\/\s"'`;&|)]|$)/;
const PACKAGE_NAME = /(manifest|packages-lock)\.json/i;
const DEPENDENCY = /"([a-z0-9][\w.-]*\.[\w.-]+)"\s*:\s*"([^"]+)"/gi;

// Путь к файлу пакетов, либо папка Packages и имя файла в одной команде (cd Packages && ...).
// Простое упоминание имени файла в тексте (сообщение коммита, отчёт) не блокируется.
function touchesPackageFiles(command) {
  return PACKAGE_PATH.test(command) || (PACKAGE_DIR.test(command) && PACKAGE_NAME.test(command));
}

function respond(decision, reason) {
  process.stdout.write(JSON.stringify({
    hookSpecificOutput: { hookEventName: 'PreToolUse', permissionDecision: decision, permissionDecisionReason: reason },
  }));
  process.exit(0);
}

function parseDeps(text) {
  const deps = new Map();
  if (!text) return deps;
  for (const m of text.matchAll(DEPENDENCY)) deps.set(m[1], m[2]);
  return deps;
}

function diffDeps(before, after) {
  const lines = [];
  for (const [name, ver] of after) {
    if (!before.has(name)) lines.push(`+ ${name} ${ver}`);
    else if (before.get(name) !== ver) lines.push(`~ ${name} ${before.get(name)} -> ${ver}`);
  }
  for (const [name, ver] of before) {
    if (!after.has(name)) lines.push(`- ${name} ${ver}`);
  }
  return lines;
}

function readFileSafe(file) {
  try { return fs.readFileSync(file, 'utf8'); } catch { return ''; }
}

let raw = '';
process.stdin.on('data', (c) => { raw += c; });
process.stdin.on('end', () => {
  let input;
  try { input = JSON.parse(raw); } catch { process.exit(0); }
  const tool = input.tool_name || '';
  const ti = input.tool_input || {};

  if (tool === 'Bash' || tool === 'PowerShell') {
    if (touchesPackageFiles(ti.command || '')) {
      respond('deny',
        'Shell-команды не должны трогать файлы пакетов Unity (manifest.json / packages-lock.json). ' +
        'Читать - инструментом Read. Менять - только инструментом Edit и только с согласия пользователя: ' +
        'сначала напиши в чате, какой пакет/версия меняется и зачем (скилл package-policy).');
    }
    process.exit(0);
  }

  if (tool === 'Edit' || tool === 'Write' || tool === 'MultiEdit') {
    const file = ti.file_path || '';
    if (!PACKAGE_FILE.test(file)) process.exit(0);

    let changes;
    if (tool === 'Write') {
      changes = diffDeps(parseDeps(readFileSafe(file)), parseDeps(ti.content));
    } else {
      const edits = tool === 'MultiEdit' ? (ti.edits || []) : [ti];
      changes = edits.flatMap((e) => diffDeps(parseDeps(e.old_string), parseDeps(e.new_string)));
    }

    const what = changes.length ? changes.join('; ') : 'список пакетов не меняется (форматирование/прочее)';
    respond('ask',
      `Правка ${path.basename(file)}: ${what}. Зачем - в сообщении агента перед этим запросом. ` +
      'Без твоего согласия пакеты не меняются (package-policy).');
  }

  process.exit(0);
});
