using System;
using UnityEngine;

namespace Exergame
{
    public enum ColorJuego { Rojo, Azul, Verde, Amarillo }

    public enum Posicion { SupIzq, InfIzq, SupDer, InfDer }

    public enum Regla { Escucha, Muestra }

    public enum Mano { Ninguna, Izquierda, Derecha }

    public static class Colores
    {
        public static ColorJuego Siguiente(ColorJuego c)
        {
            return (ColorJuego)(((int)c + 1) % 4);
        }

        public static Color Valor(ColorJuego c)
        {
            switch (c)
            {
                case ColorJuego.Rojo: return new Color(0.86f, 0.16f, 0.14f);
                case ColorJuego.Azul: return new Color(0.16f, 0.34f, 0.86f);
                case ColorJuego.Verde: return new Color(0.16f, 0.64f, 0.24f);
                default: return new Color(0.98f, 0.83f, 0.12f);
            }
        }

        public static bool EsIzquierda(Posicion p)
        {
            return p == Posicion.SupIzq || p == Posicion.InfIzq;
        }

        public static string Texto(Posicion p)
        {
            switch (p)
            {
                case Posicion.SupIzq: return "Superior izquierda";
                case Posicion.InfIzq: return "Inferior izquierda";
                case Posicion.SupDer: return "Superior derecha";
                default: return "Inferior derecha";
            }
        }
    }

    [Serializable]
    public class Combinatoria
    {
        public ColorJuego supIzq = ColorJuego.Rojo;
        public ColorJuego supDer = ColorJuego.Azul;
        public ColorJuego infIzq = ColorJuego.Verde;
        public ColorJuego infDer = ColorJuego.Amarillo;

        public ColorJuego En(Posicion p)
        {
            switch (p)
            {
                case Posicion.SupIzq: return supIzq;
                case Posicion.InfIzq: return infIzq;
                case Posicion.SupDer: return supDer;
                default: return infDer;
            }
        }

        public Posicion Donde(ColorJuego c)
        {
            if (supIzq == c) return Posicion.SupIzq;
            if (infIzq == c) return Posicion.InfIzq;
            if (supDer == c) return Posicion.SupDer;
            return Posicion.InfDer;
        }

        public bool EsValida()
        {
            int mascara = (1 << (int)supIzq) | (1 << (int)infIzq) | (1 << (int)supDer) | (1 << (int)infDer);
            return mascara == 0b1111;
        }

        public string Texto()
        {
            return $"SI={supIzq} II={infIzq} SD={supDer} ID={infDer}";
        }
    }
}
