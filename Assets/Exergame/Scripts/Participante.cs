using System;
using UnityEngine;

namespace Exergame
{
    [Serializable]
    public class Participante
    {
        [Tooltip("Codigo anonimo (P01, P02...). Es lo unico que se guarda en los resultados.")]
        public string codigo = "P01";
        [Tooltip("Nombre para la lista del supervisor y el saludo. No se guarda en los resultados.")]
        public string nombre = "Participante";
        [Tooltip("Colores en cada posicion del tablero. Debe ser la MISMA que en su ficha de la fase manual.")]
        public Combinatoria combinatoria = new Combinatoria();
        [Tooltip("Saludo grabado con su nombre (opcional).")]
        public AudioClip saludo;
        [Tooltip("Despedida grabada con su nombre (opcional).")]
        public AudioClip despedida;
    }
}
