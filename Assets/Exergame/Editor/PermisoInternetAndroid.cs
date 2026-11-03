using System.IO;
using System.Xml;
using UnityEditor.Android;
using UnityEngine;

namespace Exergame.EditorTools
{
    /// <summary>
    /// Asegura el permiso INTERNET en el APK: el panel del evaluador abre un servidor web en el visor
    /// y sin este permiso Android responde "Access denied". Se ejecuta despues de los procesadores de XR.
    /// </summary>
    public class PermisoInternetAndroid : IPostGenerateGradleAndroidProject
    {
        const string AndroidNs = "http://schemas.android.com/apk/res/android";
        const string Permiso = "android.permission.INTERNET";

        public int callbackOrder => 10000;

        public void OnPostGenerateGradleAndroidProject(string ruta)
        {
            string manifiesto = Path.Combine(ruta, "src", "main", "AndroidManifest.xml");
            if (!File.Exists(manifiesto)) return;

            var doc = new XmlDocument();
            doc.Load(manifiesto);
            var raiz = doc.DocumentElement;
            foreach (XmlElement e in raiz.SelectNodes("uses-permission"))
                if (e.GetAttribute("name", AndroidNs) == Permiso) return;

            var permiso = doc.CreateElement("uses-permission");
            permiso.SetAttribute("name", AndroidNs, Permiso);
            raiz.PrependChild(permiso);
            doc.Save(manifiesto);
            Debug.Log("[Exergame] Permiso INTERNET agregado al AndroidManifest (panel del evaluador).");
        }
    }
}
