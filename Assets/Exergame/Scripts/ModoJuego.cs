namespace Exergame
{
    /// <summary>
    /// Como llega la mano al tablero (lo elige el evaluador segun la movilidad del residente).
    /// Proximidad = la mano real toca la maqueta cercana (como hasta ahora).
    /// Completa = maqueta mas lejos; la mano se ve un poco adelantada (palo invisible de 15 cm).
    /// Limitaciones = maqueta mas lejos; se ve un palo de 30 cm en la mano para llegar.
    /// </summary>
    public enum ModoAlcance { Proximidad, Completa, Limitaciones }

    /// <summary>Prueba = los 16 ensayos que se registran. ManosLibres = tocar sin limite para acostumbrarse (no se registra).</summary>
    public enum Actividad { Prueba, ManosLibres }

    public static class ModosJuego
    {
        public static ModoAlcance LeerModo(string texto, ModoAlcance siFalla = ModoAlcance.Proximidad)
        {
            if (string.IsNullOrEmpty(texto)) return siFalla;
            switch (texto.Trim().ToLowerInvariant())
            {
                case "completa": case "movilidad_completa": return ModoAlcance.Completa;
                case "limitaciones": case "limitada": return ModoAlcance.Limitaciones;
                case "proximidad": return ModoAlcance.Proximidad;
                default: return siFalla;
            }
        }

        public static Actividad LeerActividad(string texto)
        {
            return !string.IsNullOrEmpty(texto) && texto.Trim().ToLowerInvariant() == "libre" ? Actividad.ManosLibres : Actividad.Prueba;
        }

        public static string Nombre(ModoAlcance m)
        {
            switch (m)
            {
                case ModoAlcance.Completa: return "Movilidad completa";
                case ModoAlcance.Limitaciones: return "Con limitaciones";
                default: return "Proximidad";
            }
        }

        /// <summary>Los modos con ayuda (palo) usan la maqueta mas lejos, donde se ven los 4 objetos y el letrero.</summary>
        public static bool MaquetaLejos(ModoAlcance m) => m != ModoAlcance.Proximidad;
    }
}
