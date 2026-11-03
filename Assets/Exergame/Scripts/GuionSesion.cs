using System;
using System.Collections.Generic;
using UnityEngine;

namespace Exergame
{
    [Serializable]
    public class EnsayoGuion
    {
        public int numero;
        [Range(1, 3)] public int nivel = 1;
        [Tooltip("Vacio en niveles 1 y 2. 'A' o 'B' en el nivel 3.")]
        public string bloque = "";
        public Regla regla = Regla.Escucha;
        public ColorJuego anunciado;
        public bool incongruente;
        [Tooltip("0 = sin limite de tiempo")]
        public float ventanaSegundos;
        [Tooltip("Nivel 1: solo rojo y azul activos")]
        public bool soloRojoAzul;

        public ColorJuego Mostrado => incongruente ? Colores.Siguiente(anunciado) : anunciado;
        public ColorJuego Objetivo => regla == Regla.Escucha ? anunciado : Mostrado;
    }

    [CreateAssetMenu(menuName = "Exergame/Guion de sesion")]
    public class GuionSesion : ScriptableObject
    {
        [Tooltip("Marcar en falso cuando se copie el guion oficial del documento de Combinatorias.")]
        public bool provisional = true;
        public List<EnsayoGuion> ensayos = new List<EnsayoGuion>();

        public void CargarProvisional()
        {
            provisional = true;
            ensayos = new List<EnsayoGuion>();
            N(1, "", Regla.Escucha, ColorJuego.Rojo, false, 0f, true);
            N(1, "", Regla.Escucha, ColorJuego.Azul, false, 0f, true);
            N(1, "", Regla.Escucha, ColorJuego.Azul, false, 0f, true);
            N(1, "", Regla.Escucha, ColorJuego.Rojo, false, 0f, true);

            N(2, "", Regla.Escucha, ColorJuego.Verde, false, 6f, false);
            N(2, "", Regla.Escucha, ColorJuego.Amarillo, true, 6f, false);
            N(2, "", Regla.Escucha, ColorJuego.Rojo, false, 6f, false);
            N(2, "", Regla.Escucha, ColorJuego.Azul, false, 6f, false);

            N(3, "A", Regla.Escucha, ColorJuego.Azul, true, 4f, false);
            N(3, "A", Regla.Escucha, ColorJuego.Verde, false, 4f, false);
            N(3, "A", Regla.Escucha, ColorJuego.Amarillo, true, 4f, false);
            N(3, "A", Regla.Escucha, ColorJuego.Rojo, false, 4f, false);

            N(3, "B", Regla.Muestra, ColorJuego.Rojo, true, 4f, false);
            N(3, "B", Regla.Muestra, ColorJuego.Amarillo, false, 4f, false);
            N(3, "B", Regla.Muestra, ColorJuego.Verde, true, 4f, false);
            N(3, "B", Regla.Muestra, ColorJuego.Azul, false, 4f, false);
        }

        void N(int nivel, string bloque, Regla regla, ColorJuego c, bool inc, float ventana, bool soloRA)
        {
            ensayos.Add(new EnsayoGuion
            {
                numero = ensayos.Count + 1, nivel = nivel, bloque = bloque, regla = regla,
                anunciado = c, incongruente = inc, ventanaSegundos = ventana, soloRojoAzul = soloRA
            });
        }
    }
}
