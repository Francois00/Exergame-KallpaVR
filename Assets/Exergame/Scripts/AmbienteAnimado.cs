using System.Collections.Generic;
using UnityEngine;

namespace Exergame
{
    /// <summary>
    /// Vida suave en el fondo (lejos del residente y lenta, para no marear):
    /// globos pastel que suben en la plaza, notas musicales que flotan y mariposas en el jardin.
    /// </summary>
    public class AmbienteAnimado : MonoBehaviour
    {
        class Sube { public Transform t; public float vel, fase, yMax, yMin; }
        class Flota { public Transform t; public Vector3 baseP; public float fase, amp; }
        class Mariposa { public Transform t, alaI, alaD; public Vector3 centro; public float radio, vel, fase; }

        readonly List<Sube> suben = new List<Sube>();
        readonly List<Flota> flotan = new List<Flota>();
        readonly List<Mariposa> mariposas = new List<Mariposa>();

        public void AgregarQueSube(Transform t, float vel, float yMin, float yMax)
            => suben.Add(new Sube { t = t, vel = vel, fase = Random.value * 10f, yMin = yMin, yMax = yMax });

        public void AgregarQueFlota(Transform t, float amp)
            => flotan.Add(new Flota { t = t, baseP = t.localPosition, fase = Random.value * 10f, amp = amp });

        public void AgregarMariposa(Transform t, Transform alaI, Transform alaD, Vector3 centro, float radio)
            => mariposas.Add(new Mariposa { t = t, alaI = alaI, alaD = alaD, centro = centro, radio = radio, vel = Random.Range(0.25f, 0.45f), fase = Random.value * 6.28f });

        void Update()
        {
            float dt = Time.deltaTime, tiempo = Time.time;
            foreach (var s in suben)
            {
                var p = s.t.localPosition;
                p.y += s.vel * dt;
                p.x += Mathf.Sin(tiempo * 0.4f + s.fase) * 0.15f * dt;
                if (p.y > s.yMax) p.y = s.yMin;
                s.t.localPosition = p;
                s.t.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(tiempo * 0.8f + s.fase) * 6f);
            }
            foreach (var f in flotan)
            {
                f.t.localPosition = f.baseP + new Vector3(Mathf.Sin(tiempo * 0.5f + f.fase) * f.amp, Mathf.Sin(tiempo * 0.9f + f.fase) * f.amp * 0.6f, 0f);
                f.t.localRotation = Quaternion.Euler(0f, Mathf.Sin(tiempo * 0.3f + f.fase) * 25f, Mathf.Sin(tiempo * 0.7f + f.fase) * 10f);
            }
            foreach (var m in mariposas)
            {
                float a = tiempo * m.vel + m.fase;
                var pos = m.centro + new Vector3(Mathf.Cos(a) * m.radio, 0.35f + Mathf.Sin(a * 2.3f) * 0.15f, Mathf.Sin(a) * m.radio * 0.6f);
                var dir = new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a) * 0.6f);
                m.t.localPosition = pos;
                if (dir.sqrMagnitude > 0.0001f) m.t.localRotation = Quaternion.LookRotation(dir, Vector3.up);
                float aleteo = Mathf.Sin(tiempo * 14f + m.fase) * 55f;
                m.alaI.localRotation = Quaternion.Euler(0f, 0f, aleteo);
                m.alaD.localRotation = Quaternion.Euler(0f, 0f, -aleteo);
            }
        }
    }
}
