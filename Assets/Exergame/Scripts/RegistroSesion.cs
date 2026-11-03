using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Exergame
{
    [Serializable]
    public class ResultadoEnsayo
    {
        public int numero;
        public int nivel;
        public string bloque;
        public Regla regla;
        public ColorJuego anunciado;
        public ColorJuego mostrado;
        public bool incongruente;
        public Posicion posicionSenalada;
        public Posicion posicionCorrecta;
        public bool hayRespuesta;
        public Posicion posicionTocada;
        public ColorJuego colorTocado;
        public Mano mano;
        public bool manoCorrecta;
        public float trMs = -1f;
        public bool correcto;
        public string tipoError = "";
        public bool siguioDistractor;
        public float recorridoManoM;
        public bool interrumpido;
        public string hora;
    }

    public class RegistroSesion
    {
        public readonly string codigo;
        public readonly string combinatoria;
        public readonly int numeroSesion;
        public readonly DateTime inicio;
        public readonly string rutaEnsayos;
        public readonly string rutaResumen;
        public float umbralErrorNivel = 0.5f;
        public bool guionProvisional;
        public int totalPlaneados;
        public string escenario = "Clasico";
        /// <summary>"real" = sesion con un residente (cuenta en resultados); "prueba" = practica del equipo.</summary>
        public string tipo = "real";
        /// <summary>Modo de alcance: Proximidad, Completa o Limitaciones.</summary>
        public string modo = "Proximidad";
        /// <summary>Veces que la presentadora repitio la regla porque se equivoco varias veces seguidas.</summary>
        public int repeticionesRegla;

        readonly List<ResultadoEnsayo> resultados = new List<ResultadoEnsayo>();
        static readonly CultureInfo C = CultureInfo.InvariantCulture;

        public IReadOnlyList<ResultadoEnsayo> Resultados => resultados;

        public RegistroSesion(string codigo, string combinatoria, int numeroSesion)
        {
            this.codigo = codigo;
            this.combinatoria = combinatoria;
            this.numeroSesion = numeroSesion;
            inicio = DateTime.Now;
            string baseNombre = $"{codigo}_RV_S{numeroSesion}_{inicio:yyyyMMdd_HHmm}";
            rutaEnsayos = Path.Combine(Application.persistentDataPath, baseNombre + "_ensayos.csv");
            rutaResumen = Path.Combine(Application.persistentDataPath, baseNombre + "_resumen.csv");
            Escribir(rutaEnsayos,
                "codigo,combinatoria,fase,sesion,fecha,n,nivel,bloque,regla,color_anunciado,color_mostrado,tipo_ensayo," +
                "posicion_color_mostrado,posicion_correcta,posicion_tocada,color_tocado,mano,mano_correcta,tr_ms,correcto," +
                "tipo_error,siguio_distractor,recorrido_mano_m,interrumpido,hora,escenario,tipo_sesion,modo_alcance\n", false);
            Debug.Log("[Exergame] Registro en " + rutaEnsayos);
        }

        public void Agregar(ResultadoEnsayo r)
        {
            r.hora = DateTime.Now.ToString("HH:mm:ss");
            if (!r.interrumpido) resultados.Add(r);
            var campos = new[]
            {
                codigo, Q(combinatoria), "RV", numeroSesion.ToString(C), inicio.ToString("yyyy-MM-dd"),
                r.numero.ToString(C), r.nivel.ToString(C), r.bloque, r.regla.ToString(),
                r.anunciado.ToString(), r.mostrado.ToString(), r.incongruente ? "incongruente" : "congruente",
                r.posicionSenalada.ToString(), r.posicionCorrecta.ToString(),
                r.hayRespuesta ? r.posicionTocada.ToString() : "", r.hayRespuesta ? r.colorTocado.ToString() : "",
                r.mano == Mano.Ninguna ? "" : r.mano.ToString(), r.hayRespuesta ? (r.manoCorrecta ? "1" : "0") : "",
                r.trMs >= 0 ? r.trMs.ToString("0", C) : "", r.correcto ? "1" : "0", r.tipoError,
                r.siguioDistractor ? "1" : "0", r.recorridoManoM.ToString("0.000", C), r.interrumpido ? "1" : "0", r.hora, escenario, tipo, modo
            };
            Escribir(rutaEnsayos, string.Join(",", campos) + "\n", true);
        }

        public void EscribirResumen(bool finalizada)
        {
            var validos = resultados;
            int total = validos.Count;
            int aciertos = validos.Count(r => r.correcto);
            var trCong = validos.Where(r => r.correcto && !r.incongruente && r.trMs >= 0).Select(r => r.trMs).ToList();
            var trInc = validos.Where(r => r.correcto && r.incongruente && r.trMs >= 0).Select(r => r.trMs).ToList();
            var trA = validos.Where(r => r.correcto && r.bloque == "A" && r.trMs >= 0).Select(r => r.trMs).ToList();
            var trB = validos.Where(r => r.correcto && r.bloque == "B" && r.trMs >= 0).Select(r => r.trMs).ToList();
            var trACong = validos.Where(r => r.correcto && r.bloque == "A" && !r.incongruente && r.trMs >= 0).Select(r => r.trMs).ToList();
            var trAInc = validos.Where(r => r.correcto && r.bloque == "A" && r.incongruente && r.trMs >= 0).Select(r => r.trMs).ToList();
            var trTodos = validos.Where(r => r.correcto && r.trMs >= 0).Select(r => r.trMs).ToList();

            float? mCong = Promedio(trCong), mInc = Promedio(trInc), mA = Promedio(trA), mB = Promedio(trB);
            float? cdt = Costo(mInc, mCong);
            float? cdtBloqueA = Costo(Promedio(trAInc), Promedio(trACong));
            float? ccr = Costo(mB, mA);

            int eMano = validos.Count(r => r.tipoError == "mano_equivocada");
            int eCirculo = validos.Count(r => r.tipoError == "circulo_equivocado");
            int eSin = validos.Count(r => r.tipoError == "sin_respuesta");
            int eTardia = validos.Count(r => r.tipoError == "tardia");

            int nivelMax = 0;
            for (int n = 1; n <= 3; n++)
            {
                var delNivel = validos.Where(r => r.nivel == n).ToList();
                int planeados = n == 3 ? 8 : 4;
                if (delNivel.Count < planeados) break;
                float tasaError = delNivel.Count(r => !r.correcto) / (float)delNivel.Count;
                if (tasaError > umbralErrorNivel) break;
                nivelMax = n;
            }

            var sb = new StringBuilder();
            sb.Append("metrica,valor\n");
            L(sb, "codigo", codigo);
            L(sb, "combinatoria", Q(combinatoria));
            L(sb, "fase", "RV");
            L(sb, "escenario", escenario);
            L(sb, "tipo", tipo);
            L(sb, "modo", modo);
            L(sb, "repeticiones_regla", repeticionesRegla.ToString(C));
            L(sb, "sesion", numeroSesion.ToString(C));
            L(sb, "fecha", inicio.ToString("yyyy-MM-dd"));
            L(sb, "hora_inicio", inicio.ToString("HH:mm:ss"));
            L(sb, "hora_fin", DateTime.Now.ToString("HH:mm:ss"));
            L(sb, "sesion_finalizada", finalizada ? "1" : "0");
            L(sb, "guion_provisional", guionProvisional ? "1" : "0");
            L(sb, "ensayos_completados", total.ToString(C));
            L(sb, "ensayos_abandonados", Mathf.Max(0, totalPlaneados - total).ToString(C));
            L(sb, "precision_pct", total > 0 ? (aciertos * 100f / total).ToString("0.0", C) : "");
            L(sb, "tr_promedio_congruente_ms", F(mCong));
            L(sb, "tr_promedio_incongruente_ms", F(mInc));
            L(sb, "cdt_pct", F(cdt));
            L(sb, "cdt_bloque_A_pct", F(cdtBloqueA));
            L(sb, "tr_promedio_bloque_A_ms", F(mA));
            L(sb, "tr_promedio_bloque_B_ms", F(mB));
            L(sb, "ccr_pct", F(ccr));
            L(sb, "tr_desviacion_estandar_ms", F(Desviacion(trTodos)));
            L(sb, "errores_total", (total - aciertos).ToString(C));
            L(sb, "errores_mano", eMano.ToString(C));
            L(sb, "errores_circulo", eCirculo.ToString(C));
            L(sb, "errores_sin_respuesta", eSin.ToString(C));
            L(sb, "errores_tardia", eTardia.ToString(C));
            L(sb, "siguio_distractor", validos.Count(r => r.siguioDistractor).ToString(C));
            L(sb, "nivel_maximo_alcanzado", nivelMax.ToString(C));
            L(sb, "umbral_error_nivel", umbralErrorNivel.ToString("0.00", C));
            Escribir(rutaResumen, sb.ToString(), false);
            Debug.Log("[Exergame] Resumen en " + rutaResumen);
        }

        static void L(StringBuilder sb, string k, string v) => sb.Append(k).Append(',').Append(v).Append('\n');

        static float? Promedio(List<float> v) => v.Count > 0 ? v.Average() : (float?)null;

        static float? Costo(float? alto, float? bajo)
        {
            if (!alto.HasValue || !bajo.HasValue || bajo.Value <= 0f) return null;
            return (alto.Value - bajo.Value) / bajo.Value * 100f;
        }

        static float? Desviacion(List<float> v)
        {
            if (v.Count < 2) return null;
            float m = v.Average();
            return Mathf.Sqrt(v.Sum(x => (x - m) * (x - m)) / (v.Count - 1));
        }

        static string F(float? v) => v.HasValue ? v.Value.ToString("0.0", C) : "";
        static string Q(string s) => "\"" + s.Replace("\"", "'") + "\"";

        static void Escribir(string ruta, string texto, bool anexar)
        {
            try
            {
                if (anexar) File.AppendAllText(ruta, texto, new UTF8Encoding(false));
                else File.WriteAllText(ruta, texto, new UTF8Encoding(true));
            }
            catch (Exception e)
            {
                Debug.LogError("[Exergame] No se pudo escribir " + ruta + ": " + e.Message);
            }
        }
    }
}
