using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Exergame
{
    public class PanelEvaluador : MonoBehaviour
    {
        public SesionExergame sesion;
        public int puerto = 8095;
        [Tooltip("En la PC (Unity con Meta Link): al dar Play se abre solo el panel en el navegador (http://localhost:8095).")]
        public bool abrirNavegadorEnPC = true;

        public string Pin { get; private set; }
        public string Url { get; private set; }

        TcpListener servidor;
        Thread hilo;
        volatile bool activo;
        string html = "<h1>Panel no disponible</h1>";
        string carpetaDatos;
        string estadoJson = "{}";
        string historialJson = "[]";
        float proximoEstado, proximoHistorial;
        readonly object candado = new object();
        readonly ConcurrentQueue<Action> comandos = new ConcurrentQueue<Action>();

        void Awake()
        {
            carpetaDatos = Application.persistentDataPath;
            // PIN fijo para este visor: no cambia al reiniciar el juego.
            Pin = PlayerPrefs.GetString("exergame_pin", "");
            if (Pin.Length != 4)
            {
                Pin = UnityEngine.Random.Range(1000, 10000).ToString();
                PlayerPrefs.SetString("exergame_pin", Pin);
                PlayerPrefs.Save();
            }
            var recurso = Resources.Load<TextAsset>("panel_evaluador");
            if (recurso != null) html = recurso.text;
            Url = ArmarUrl();
        }

        /// <summary>Hay red WiFi/hotspot: http://IP:8095. Sin red: solo por cable USB (http://localhost:8095).</summary>
        public bool SinRed { get; private set; }

        string ArmarUrl()
        {
            var ip = IpLocal();
            SinRed = string.IsNullOrEmpty(ip);
            return SinRed ? $"http://localhost:{puerto}" : $"http://{ip}:{puerto}";
        }

        void Start()
        {
            try
            {
                servidor = new TcpListener(IPAddress.Any, puerto);
                servidor.Start();
                activo = true;
                hilo = new Thread(Escuchar) { IsBackground = true };
                hilo.Start();
                Debug.Log($"[Exergame] Panel del evaluador en {Url}  PIN {Pin}");
                AbrirEnNavegadorDeLaPC();
            }
            catch (Exception e)
            {
                Debug.LogError("[Exergame] No se pudo iniciar el panel del evaluador: " + e.Message);
            }
            ActualizarHistorial();
        }

        // Con Meta Link el juego corre en la laptop: se abre el panel solo, sin copiar el link.
        // Para no abrir una pestana nueva en cada Play, solo si pasaron mas de 2 horas desde la ultima vez
        // (la pestana abierta se reconecta sola).
        void AbrirEnNavegadorDeLaPC()
        {
            if (!abrirNavegadorEnPC || Application.platform == RuntimePlatform.Android) return;
            long ahora = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMinute;
            long antes = long.TryParse(PlayerPrefs.GetString("exergame_panel_abierto", "0"), out var v) ? v : 0;
            if (ahora - antes < 120) return;
            PlayerPrefs.SetString("exergame_panel_abierto", ahora.ToString());
            PlayerPrefs.Save();
            Application.OpenURL($"http://localhost:{puerto}/");
        }

        void Update()
        {
            while (comandos.TryDequeue(out var c))
            {
                try { c(); } catch (Exception e) { Debug.LogError(e); }
            }
            if (Time.unscaledTime >= proximoEstado && sesion != null)
            {
                string json = sesion.EstadoJson();
                lock (candado) estadoJson = json;
                proximoEstado = Time.unscaledTime + 0.4f;
            }
            if (Time.unscaledTime >= proximoHistorial)
            {
                ActualizarHistorial();
                Url = ArmarUrl();
                proximoHistorial = Time.unscaledTime + 5f;
            }
        }

        void OnDestroy()
        {
            activo = false;
            try { servidor?.Stop(); } catch { }
        }

        void Escuchar()
        {
            while (activo)
            {
                try
                {
                    var cliente = servidor.AcceptTcpClient();
                    ThreadPool.QueueUserWorkItem(_ => Atender(cliente));
                }
                catch
                {
                    if (!activo) return;
                }
            }
        }

        void Atender(TcpClient cliente)
        {
            try
            {
                using (cliente)
                {
                    cliente.ReceiveTimeout = 3000;
                    var flujo = cliente.GetStream();
                    var lector = new StreamReader(flujo, Encoding.UTF8);
                    string primera = lector.ReadLine();
                    if (string.IsNullOrEmpty(primera)) return;
                    var partes = primera.Split(' ');
                    string objetivo = partes.Length > 1 ? partes[1] : "/";
                    string pin = null;
                    string linea;
                    while (!string.IsNullOrEmpty(linea = lector.ReadLine()))
                        if (linea.StartsWith("X-Pin:", StringComparison.OrdinalIgnoreCase)) pin = linea.Substring(6).Trim();
                    cliente.SendTimeout = 3000;
                    // Desde la misma PC (localhost) no se pide PIN.
                    bool local = cliente.Client.RemoteEndPoint is IPEndPoint remoto && IPAddress.IsLoopback(remoto.Address);

                    string ruta = objetivo;
                    var consulta = new Dictionary<string, string>();
                    int q = objetivo.IndexOf('?');
                    if (q >= 0)
                    {
                        ruta = objetivo.Substring(0, q);
                        foreach (var par in objetivo.Substring(q + 1).Split('&'))
                        {
                            var kv = par.Split('=');
                            if (kv.Length == 2) consulta[Uri.UnescapeDataString(kv[0])] = Uri.UnescapeDataString(kv[1]);
                        }
                    }

                    if (ruta == "/" || ruta == "/index.html")
                    {
                        Responder(flujo, 200, "text/html; charset=utf-8", html);
                        return;
                    }
                    if (pin == null && consulta.TryGetValue("pin", out var pinQ)) pin = pinQ;
                    if (ruta == "/api/ping")
                    {
                        Responder(flujo, 200, "application/json", "{\"ok\":true}");
                        return;
                    }
                    if (!ruta.StartsWith("/api/"))
                    {
                        Responder(flujo, 404, "text/plain", "No encontrado");
                        return;
                    }
                    if (ruta == "/api/acceso")
                    {
                        // Link y PIN para abrir el panel en el celular con un codigo QR (solo desde la misma PC).
                        if (!local) { Responder(flujo, 401, "application/json", "{\"error\":\"pin\"}"); return; }
                        Responder(flujo, 200, "application/json; charset=utf-8",
                            $"{{\"url\":\"{Escapar(Url)}\",\"pin\":\"{Pin}\",\"sinRed\":{(SinRed ? "true" : "false")}}}");
                        return;
                    }
                    if (!local && pin != Pin)
                    {
                        Responder(flujo, 401, "application/json", "{\"error\":\"pin\"}");
                        return;
                    }

                    string respuesta = "{\"ok\":true}";
                    switch (ruta)
                    {
                        case "/api/estado":
                            lock (candado) respuesta = estadoJson;
                            break;
                        case "/api/historial":
                            lock (candado) respuesta = historialJson;
                            break;
                        case "/api/pausar":
                            comandos.Enqueue(() => sesion.PausarDesdePanel());
                            break;
                        case "/api/continuar":
                            comandos.Enqueue(() => sesion.ContinuarDesdePanel());
                            break;
                        case "/api/terminar":
                            comandos.Enqueue(() => sesion.TerminarDesdePanel());
                            break;
                        case "/api/recentrar":
                            comandos.Enqueue(() => sesion.RecentrarDesdePanel());
                            break;
                        case "/api/repetir":
                            comandos.Enqueue(() => sesion.RepetirDesdePanel());
                            break;
                        case "/api/saltar":
                            comandos.Enqueue(() => sesion.SaltarEnsayoDesdePanel());
                            break;
                        case "/api/volumen":
                            consulta.TryGetValue("d", out var textoVol);
                            float.TryParse(textoVol, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float delta);
                            comandos.Enqueue(() => sesion.VolumenDesdePanel(delta));
                            break;
                        case "/api/iniciar":
                            consulta.TryGetValue("codigo", out var codigo);
                            consulta.TryGetValue("sesion", out var textoSesion);
                            consulta.TryGetValue("escenario", out var textoEscenario);
                            consulta.TryGetValue("tipo", out var textoTipo);
                            consulta.TryGetValue("modo", out var textoModo);
                            consulta.TryGetValue("actividad", out var textoActividad);
                            int.TryParse(textoSesion, out int numero);
                            comandos.Enqueue(() => sesion.IniciarDesdePanel(codigo, numero, textoEscenario, textoTipo, textoModo, textoActividad));
                            break;
                        case "/api/aprueba":
                            comandos.Enqueue(() => sesion.PasarAPruebaDesdePanel());
                            break;
                        case "/api/marcar":
                        {
                            // Marca una sesion guardada como "real" (con residente) o "prueba".
                            consulta.TryGetValue("archivo", out var arch);
                            consulta.TryGetValue("tipo", out var tipoNuevo);
                            var ruta2 = RutaSegura(arch, "_resumen.csv");
                            if (ruta2 == null || (tipoNuevo != "real" && tipoNuevo != "prueba"))
                            {
                                Responder(flujo, 404, "application/json", "{\"error\":\"archivo\"}");
                                return;
                            }
                            lock (candado)
                            {
                                var lineas = File.ReadAllLines(ruta2).Where(l => !l.TrimStart('\uFEFF').StartsWith("tipo,")).ToList();
                                lineas.Add("tipo," + tipoNuevo);
                                File.WriteAllText(ruta2, string.Join("\n", lineas) + "\n", new UTF8Encoding(true));
                            }
                            comandos.Enqueue(ActualizarHistorial);
                            break;
                        }
                        case "/api/archivar":
                        {
                            // Saca sesiones de prueba del historial (las mueve a Archivo_pruebas, no las borra).
                            int n = 0;
                            lock (candado)
                            {
                                if (consulta.TryGetValue("todo", out var todo) && todo == "1") n = ArchivoDatos.ArchivarTodo(carpetaDatos);
                                else if (consulta.TryGetValue("archivo", out var arch) && ArchivoDatos.ArchivarSesion(carpetaDatos, arch)) n = 1;
                            }
                            comandos.Enqueue(ActualizarHistorial);
                            respuesta = "{\"ok\":true,\"archivadas\":" + n + "}";
                            break;
                        }
                        case "/api/ensayos":
                        {
                            // Detalle de una sesion: devuelve el CSV de ensayos tal cual.
                            consulta.TryGetValue("archivo", out var arch);
                            var ruta2 = RutaSegura(arch != null ? arch.Replace("_resumen.csv", "_ensayos.csv") : null, "_ensayos.csv");
                            if (ruta2 == null) { Responder(flujo, 404, "application/json", "{\"error\":\"archivo\"}"); return; }
                            Responder(flujo, 200, "text/csv; charset=utf-8", File.ReadAllText(ruta2));
                            return;
                        }
                        default:
                            Responder(flujo, 404, "application/json", "{\"error\":\"ruta\"}");
                            return;
                    }
                    Responder(flujo, 200, "application/json; charset=utf-8", respuesta);
                }
            }
            catch
            {
            }
        }

        static void Responder(Stream flujo, int codigo, string tipo, string cuerpo)
        {
            byte[] datos = Encoding.UTF8.GetBytes(cuerpo);
            string estado = codigo == 200 ? "OK" : codigo == 401 ? "Unauthorized" : "Not Found";
            string cabecera = $"HTTP/1.1 {codigo} {estado}\r\nContent-Type: {tipo}\r\nContent-Length: {datos.Length}\r\nCache-Control: no-store\r\nConnection: close\r\n\r\n";
            byte[] cab = Encoding.ASCII.GetBytes(cabecera);
            flujo.Write(cab, 0, cab.Length);
            flujo.Write(datos, 0, datos.Length);
            flujo.Flush();
        }

        void ActualizarHistorial()
        {
            var claves = new[]
            {
                "codigo", "sesion", "escenario", "modo", "repeticiones_regla", "fecha", "hora_inicio", "sesion_finalizada", "guion_provisional", "precision_pct",
                "tr_promedio_congruente_ms", "tr_promedio_incongruente_ms", "cdt_pct", "ccr_pct",
                "nivel_maximo_alcanzado", "errores_total", "ensayos_completados", "errores_mano", "errores_circulo",
                "errores_sin_respuesta", "errores_tardia", "siguio_distractor", "tipo", "hora_fin", "tr_desviacion_estandar_ms"
            };
            var sb = new StringBuilder("[");
            bool primero = true;
            try
            {
                foreach (var archivo in Directory.GetFiles(carpetaDatos, "*_resumen.csv").OrderBy(f => f))
                {
                    var valores = new Dictionary<string, string>();
                    foreach (var linea in File.ReadAllLines(archivo).Skip(1))
                    {
                        int coma = linea.IndexOf(',');
                        if (coma <= 0) continue;
                        valores[linea.Substring(0, coma).Trim('﻿')] = linea.Substring(coma + 1).Trim('"');
                    }
                    if (!primero) sb.Append(',');
                    primero = false;
                    sb.Append('{');
                    sb.Append($"\"archivo\":\"{Escapar(Path.GetFileName(archivo))}\",");
                    sb.Append(string.Join(",", claves.Select(k => $"\"{k}\":\"{Escapar(valores.TryGetValue(k, out var v) ? v : "")}\"")));
                    sb.Append('}');
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Exergame] No se pudo leer el historial: " + e.Message);
            }
            sb.Append(']');
            lock (candado) historialJson = sb.ToString();
        }

        /// <summary>Ruta de un archivo de datos por su nombre (sin carpetas), solo si existe.</summary>
        string RutaSegura(string nombre, string terminacion)
        {
            if (string.IsNullOrEmpty(nombre) || nombre.IndexOfAny(new[] { '/', '\\', ':' }) >= 0 || nombre.Contains("..") || !nombre.EndsWith(terminacion)) return null;
            var ruta = Path.Combine(carpetaDatos, nombre);
            return File.Exists(ruta) ? ruta : null;
        }

        public static string Escapar(string s)
        {
            return (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", " ");
        }

        static string IpLocal()
        {
            try
            {
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                {
                    socket.Connect("10.255.255.255", 1);
                    return ((IPEndPoint)socket.LocalEndPoint).Address.ToString();
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
