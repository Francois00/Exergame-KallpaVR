using UnityEngine;

namespace Exergame
{
    /// <summary>
    /// Va en el personaje animado (junto al Animator). Lleva la mano derecha hacia el
    /// circulo de color cuando se muestra, y hace que la cabeza mire al residente.
    /// La capa del Animator debe tener "IK Pass" activado.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class IKManoPresentador : MonoBehaviour
    {
        [Tooltip("Punto donde va la mano (el mango del circulo).")]
        public Transform objetivo;
        [Tooltip("0 = mano libre (animacion), 1 = mano en el circulo.")]
        [Range(0f, 1f)] public float pesoDeseado;
        [Tooltip("Que tan rapido sube o baja la mano (por segundo).")]
        public float velocidad = 4f;

        [Header("Mirada")]
        public bool mirarAlResidente = true;
        [Range(0f, 1f)] public float pesoMirada = 0.6f;

        Animator animador;
        Transform camara;
        float peso;

        void Awake() { animador = GetComponent<Animator>(); }

        void Update() { peso = Mathf.MoveTowards(peso, pesoDeseado, velocidad * Time.deltaTime); }

        void OnAnimatorIK(int capa)
        {
            if (animador == null) return;

            if (objetivo != null)
            {
                float w = Mathf.SmoothStep(0f, 1f, peso);
                animador.SetIKPositionWeight(AvatarIKGoal.RightHand, w);
                animador.SetIKPosition(AvatarIKGoal.RightHand, objetivo.position);
                animador.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
            }

            if (mirarAlResidente)
            {
                if (camara == null && Camera.main != null) camara = Camera.main.transform;
                if (camara != null)
                {
                    animador.SetLookAtWeight(pesoMirada, 0.15f, 0.7f, 0.9f, 0.6f);
                    animador.SetLookAtPosition(camara.position);
                }
            }
        }
    }
}
