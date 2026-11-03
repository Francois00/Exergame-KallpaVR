# Exergame VR (UCSM)

Exergame de realidad virtual para adultos mayores sentados, en Meta Quest 2. El residente toca uno de
cuatro círculos (tablero 2×2) según el color que **escucha** o que **ve** en el letrero de la
presentadora (Mousey). Se registra el rendimiento cognitivo observado (precisión, tiempo de reacción,
tipo de error, costo de la tarea dual). **No** es una evaluación de riesgo de caídas ni un diagnóstico.

Un evaluador controla la sesión desde un panel web (laptop o celular en la misma red).

## Requisitos

| Elemento | Versión / valor |
|---|---|
| Unity | **2022.3.62f3** exacto, con *Android Build Support* (OpenJDK + SDK & NDK) |
| URP | 14.0.12 |
| XR Interaction Toolkit | 2.6.4 (Samples > Starter Assets importados en `Assets/Samples/XR Interaction Toolkit/2.5.2`) |
| OpenXR | 1.14.3 |
| XR Plug-in Management | 4.4.0 |
| Input System | 1.14.0 |
| TextMesh Pro | 3.0.7 |
| Visor | Meta Quest 2 (APK Android o Meta Horizon Link) |
| Disco | ~10 GB libres; ruta corta y sin tildes (por ejemplo `D:\Exergame`) |

Las versiones salen de `Packages/manifest.json` y `ProjectSettings/ProjectVersion.txt`.

## Instalación

1. Clonar o descomprimir en una ruta corta. Debe quedar `Assets/`, `Packages/`, `ProjectSettings/`, `Documentacion/`.
2. Unity Hub > *Add project from disk* > la carpeta que contiene `Assets`. Abrir con 2022.3.62f3 (no aceptar *Upgrade*).
3. La primera vez se necesita internet (Unity descarga los paquetes).
4. Menú **Exergame > Diagnostico del proyecto** > *Arreglar automaticamente* > *Volver a revisar*.
   Hace 13 comprobaciones y deja el reporte en `Diagnostico_Exergame.txt` (no se versiona).

Guía detallada: `LEEME.txt` y `Documentacion/GUIA_INSTALACION_Y_DIAGNOSTICO.pdf`.

## Configuración

Los parámetros se editan en el Inspector (componentes de la escena `SalaExergame`) y en dos assets:

- `Assets/Exergame/Participantes.asset` (`ListaParticipantes`): códigos anónimos y combinatoria de colores por participante.
- `Assets/Exergame/GuionSesion.asset` (`GuionSesion`): 16 ensayos en 3 niveles. Hoy está marcado `provisional = true`.

Valores principales y dónde están: ver [`docs/configuracion.ejemplo.md`](docs/configuracion.ejemplo.md).

Ajustes de Android (los aplica **Exergame > Configurar para Meta Quest 2**): package id
`pe.edu.ucsm.exergamevr`, IL2CPP, ARM64, API mínima 29, Vulkan + OpenGLES3, ASTC, permiso INTERNET.

## Ejecución

**En la PC sin visor (editor):** abrir `Assets/Exergame/Scenes/SalaExergame.unity` y dar Play.
Se agrega solo el componente `PruebaConMouse`: clic izquierdo = mano correcta, clic derecho = mano equivocada.
Tecla **M** abre el menú de respaldo del supervisor.

**Panel del evaluador:** al dar Play se abre `http://localhost:8095` (desde la misma PC no pide PIN).
Desde el celular: botón *Abrir en el celular* (QR) en la misma red; desde otro equipo se pide el PIN.

**En el visor:**
- Link: Meta Horizon Link + Play en Unity.
- APK: **Exergame > Configurar para Meta Quest 2**, luego **Compilar e instalar en Quest (USB)** o **Solo crear el APK**
  (sale en `Builds/ExergameVR.apk`, no se versiona).

Menús disponibles en **Exergame**: Construir sala · Diagnostico del proyecto · Crear paquete para compartir (.zip) ·
Configurar para Meta Quest 2 · Solo crear el APK · Compilar e instalar en Quest (USB) · Presentadora: usar Mousey ·
Asignar voces grabadas (12 a 21) · Archivar historial de pruebas (esta PC).

## Estructura

```
Assets/
  Exergame/
    Scripts/                 Lógica del juego (namespace Exergame)
    Editor/                  Menús Exergame (namespace Exergame.EditorTools)
    Herramientas/Editor/     Diagnóstico y empaquetado (ensamblado propio Exergame.Herramientas.Editor)
    Resources/               panel_evaluador.html (panel web servido por PanelEvaluador)
    Scenes/                  SalaExergame.unity (escena del juego)
    Audio/Voz/               Voces (.wav) y GUION_GRABACION.txt
    Personajes/Mousey/       Presentadora (FBX + animaciones + texturas)
    Materiales/  Texturas/
    GuionSesion.asset  Participantes.asset
  Samples/  Settings/  VRTemplateAssets/  XR/  XRI/  TextMesh Pro/  Scenes/   (plantilla VR de Unity)
Packages/                    manifest.json, packages-lock.json
ProjectSettings/
Documentacion/               Guía PDF, LEEME, cambios, propuestas (Escenario_Futbol)
docs/                        Configuración de ejemplo
tools/evidencia_git.sh       Genera EVIDENCIA_GIT.txt
```

No hay carpetas `Assets/StreamingAssets` ni `Assets/Data`.

## Datos

Cada sesión escribe dos CSV en `Application.persistentDataPath`
(en Windows: `C:\Users\<usuario>\AppData\LocalLow\UCSM\Exergame VR`):

- `<codigo>_RV_S<n>_<yyyyMMdd_HHmm>_ensayos.csv`: una fila por ensayo (regla, colores, posición, mano, tr_ms, tipo de error, escenario, tipo de sesión, modo de alcance…).
- `<codigo>_RV_S<n>_<yyyyMMdd_HHmm>_resumen.csv`: métricas `metrica,valor` (precisión, TR medio congruente/incongruente, CDT, nivel máximo, errores por tipo…).

"Vaciar historial" mueve los archivos a `Archivo_pruebas/` (no borra). Los CSV **no** se suben al repositorio.

## Privacidad

- Solo se guardan **códigos anónimos** (P01, P02…). El nombre del participante se usa en la lista del supervisor y no se escribe en los CSV.
- Consentimiento informado conforme a la Ley N.° 29733 (Protección de Datos Personales).
- El PIN del panel se genera al azar (4 dígitos) la primera vez y se guarda en `PlayerPrefs` del equipo (clave `exergame_pin`); no está en el repositorio. Escríbalo como `<PIN>` en documentos.
- `.gitignore` excluye CSV, bases de datos, APK, keystores, reportes locales y secretos.
