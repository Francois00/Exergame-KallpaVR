using UnityEngine;
using UnityEngine.InputSystem;

namespace Exergame
{
    // Solo para probar en el editor de Unity sin visor:
    // clic izquierdo = empujar con la mano correcta, clic derecho = con la mano equivocada.
    public class PruebaConMouse : MonoBehaviour
    {
        [Tooltip("Campo de vision aproximado del Meta Quest 2 para la vista previa en el editor.")]
        public float campoVisionEditor = 90f;

        void Start()
        {
            if (Application.isEditor && !UnityEngine.XR.XRSettings.isDeviceActive && Camera.main != null)
                Camera.main.fieldOfView = campoVisionEditor;
        }

        void Update()
        {
            if (!Application.isEditor) return;
            var mouse = Mouse.current;
            if (mouse == null) return;
            bool izq = mouse.leftButton.wasPressedThisFrame;
            bool der = mouse.rightButton.wasPressedThisFrame;
            if (!izq && !der) return;

            var camara = Camera.main;
            if (camara == null) return;
            var rayo = camara.ScreenPointToRay(mouse.position.ReadValue());
            var impactos = Physics.RaycastAll(rayo, 10f);
            System.Array.Sort(impactos, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var h in impactos)
            {
                var globo = h.collider.GetComponentInParent<GloboPractica>();
                if (globo != null)
                {
                    // Clic izquierdo = mano correcta, clic derecho = mano equivocada.
                    globo.Tocar(izq ? globo.esIzquierdo : !globo.esIzquierdo);
                    return;
                }
                var opcion = h.collider.GetComponentInParent<OpcionMenu>();
                if (opcion != null && izq)
                {
                    opcion.alElegir?.Invoke();
                    return;
                }
                var boton = h.collider.GetComponentInParent<BotonExergame>();
                if (boton != null)
                {
                    bool manoIzquierda = boton.LadoIzquierdo;
                    if (der) manoIzquierda = !manoIzquierda;
                    boton.EmpujarSimulado(manoIzquierda ? Mano.Izquierda : Mano.Derecha);
                    return;
                }
            }
        }
    }
}
