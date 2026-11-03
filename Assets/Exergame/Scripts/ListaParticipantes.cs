using System.Collections.Generic;
using UnityEngine;

namespace Exergame
{
    [CreateAssetMenu(menuName = "Exergame/Lista de participantes")]
    public class ListaParticipantes : ScriptableObject
    {
        public List<Participante> participantes = new List<Participante>();
    }
}
