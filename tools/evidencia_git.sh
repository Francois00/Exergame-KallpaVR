#!/usr/bin/env bash
# Genera EVIDENCIA_GIT.txt en la raiz del proyecto (solo lectura: no modifica el repositorio).
# Uso (Git Bash, desde la raiz del proyecto):  bash tools/evidencia_git.sh [desde-AAAA-MM-DD]
# Ejemplo: bash tools/evidencia_git.sh 2026-08-01
set -u
cd "$(dirname "$0")/.." || exit 1
SALIDA="EVIDENCIA_GIT.txt"
DESDE="${1:-}"
FILTRO=()
[ -n "$DESDE" ] && FILTRO=(--since="$DESDE")

ARCHIVOS=(
  Assets/Exergame/Scripts/*.cs
  Assets/Exergame/Editor/*.cs
  Assets/Exergame/Herramientas/Editor/*.cs
  Assets/Exergame/Resources/panel_evaluador.html
  Assets/Exergame/GuionSesion.asset
  Assets/Exergame/Participantes.asset
  Assets/Exergame/Scenes/SalaExergame.unity
  Packages/manifest.json
  ProjectSettings/ProjectSettings.asset
)

# PIN del panel guardado en PlayerPrefs del editor (registro de Windows). Nunca se imprime.
leer_pin() {
  command -v powershell.exe >/dev/null 2>&1 || return 0
  powershell.exe -NoProfile -Command '
    $r = "HKCU:\Software\Unity\UnityEditor\UCSM\Exergame VR"
    if (Test-Path $r) { $k = Get-Item $r
      foreach ($n in $k.GetValueNames()) { if ($n -like "exergame_pin*") { $v = $k.GetValue($n)
        if ($v -is [byte[]]) { [Text.Encoding]::UTF8.GetString($v).TrimEnd([char]0) } else { "$v" } } } }' 2>/dev/null | tr -d '\r\n'
}

{
  echo "EVIDENCIA GIT - Exergame VR"
  echo "Generado: $(date '+%Y-%m-%d %H:%M')"
  [ -n "$DESDE" ] && echo "Historial desde: $DESDE"
  echo

  if ! git rev-parse --is-inside-work-tree >/dev/null 2>&1; then
    echo "RESULTADO: esta carpeta NO es un repositorio Git (no existe .git)."
    exit 0
  fi

  echo "== URL del remoto (git remote -v)";  git remote -v; echo
  echo "== Ramas (git branch -a -vv)";       git branch -a -vv; echo
  echo "== Etiquetas (git tag -n)";          git tag -n; echo
  echo "== Grafo (git log --oneline --graph --decorate --all)"
  git log --oneline --graph --decorate --all "${FILTRO[@]}"; echo
  echo "== Historial (git log --all)"
  git log --all --date=short --pretty=format:'%h | %ad | %an | %s' "${FILTRO[@]}"; echo; echo
  echo "== Integraciones (merges)"
  git log --all --merges --date=short --pretty=format:'%h | %ad | %an | %s' "${FILTRO[@]}"; echo; echo
  echo "== Archivos por commit"
  for c in $(git rev-list --all --reverse --no-merges "${FILTRO[@]}"); do
    echo "$(git log -1 --pretty=format:'%h %s' "$c")  ->  $(git show --pretty=format: --name-only "$c" | grep -c .) archivo(s)"
    git show --pretty=format: --name-only "$c" | grep -v '\.meta$' | awk -F/ '{ if (NF>3) print "    " $1"/"$2"/"$3"/..."; else print "    " $0 }' | sort -u | head -15
  done
  echo
  echo "== Ultimo commit por archivo"
  for f in "${ARCHIVOS[@]}"; do
    [ -e "$f" ] || continue
    linea=$(git log -1 --date=short --pretty=format:'%h | %ad | %s' -- "$f")
    echo "$f | ${linea:-(sin commits)}"
  done
  echo
  echo "== Estado final (git status --short --branch)"; git status --short --branch; echo
  echo "== Cantidad de archivos versionados: $(git ls-files | wc -l)"
  echo
  echo "== Control de archivos prohibidos en lo versionado"
  for patron in '*.csv' '*.apk' '*.aab' '*.keystore' '*.jks' '*.db' '.env' 'Diagnostico_Exergame.txt'; do
    n=$(git ls-files -- "$patron" "**/$patron" | wc -l)
    echo "  $patron: $n"
  done
  for carpeta in Library Temp Obj Builds Logs UserSettings; do
    n=$(git ls-files -- "$carpeta/" | wc -l)
    echo "  $carpeta/: $n"
  done
  PIN="$(leer_pin)"
  if [[ "$PIN" =~ ^[0-9]{4}$ ]]; then
    total=$(git grep -I -w -F -c -- "$PIN" | awk -F: '{s+=$NF} END {print s+0}')
    conpin=$(git grep -I -w -F -i -h -- "$PIN" | grep -i -c 'pin')
    echo "  PIN del panel (valor no impreso): $total coincidencia(s) como numero suelto; $conpin en lineas que mencionan 'pin'."
    [ "$total" -gt 0 ] && echo "    (Un numero de 4 cifras puede aparecer por azar en assets de Unity; lo relevante es la cifra en lineas con 'pin'.)"
  else
    echo "  PIN del panel: no se encontro en el registro de esta PC; no se pudo comprobar."
  fi
} > "$SALIDA" 2>&1
unset PIN

echo "Listo: $SALIDA"
