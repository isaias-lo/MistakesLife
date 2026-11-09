#nullable enable

using System.Collections.Generic;
using UnityEngine;

namespace MistakesLife.Unity
{
    /// <summary>
    /// Materiales compartidos de la maqueta 3D (docs/diseno-app.md, sección 7).
    /// Cada uno es un archivo en Resources/Materiales/&lt;nombre&gt;.mat: si se cambia su color
    /// o su sombreado en el editor de Unity, cambia en todo el juego a la vez.
    /// Si falta un archivo, se usa el color de reserva que está aquí, así nada se rompe.
    /// </summary>
    public static class Palette
    {
        private static readonly Dictionary<string, Material> Loaded = new Dictionary<string, Material>();

        /// <summary>Madera clara de los muebles: mesa, escritorio, mecedora.</summary>
        public static Material Wood => Get("madera", new Color(0.58f, 0.42f, 0.28f));

        /// <summary>Madera oscura: patas, puertas.</summary>
        public static Material DarkWood => Get("madera_oscura", new Color(0.40f, 0.28f, 0.18f));

        /// <summary>Marcos de cuadros y fotos.</summary>
        public static Material Frame => Get("marco", new Color(0.30f, 0.22f, 0.16f));

        /// <summary>Papel: diplomas, cuentas, velas.</summary>
        public static Material Paper => Get("papel", new Color(0.95f, 0.94f, 0.90f));

        /// <summary>Rojo de alarma: cuentas impagas, tapas de remedios, avisos marcados.</summary>
        public static Material Alarm => Get("alarma", new Color(0.85f, 0.22f, 0.20f));

        /// <summary>Pantallas apagadas: la tele.</summary>
        public static Material Screen => Get("pantalla", new Color(0.12f, 0.12f, 0.14f));

        /// <summary>Piel de las figuras.</summary>
        public static Material Skin => Get("piel", new Color(0.86f, 0.68f, 0.55f));

        /// <summary>Ropa del jugador y techo de su casa.</summary>
        public static Material Player => Get("jugador", new Color(0.20f, 0.45f, 0.55f));

        /// <summary>El amarillo que marca al jugador (círculo a sus pies y bolita sobre la cabeza).</summary>
        public static Material PlayerMark => Get("marca_jugador", new Color(0.98f, 0.82f, 0.25f));

        /// <summary>Busca el material por nombre; si no hay archivo, lo crea una vez con el color de reserva.</summary>
        public static Material Get(string name, Color fallback)
        {
            if (Loaded.TryGetValue(name, out Material? material) && material != null)
            {
                return material;
            }

            material = Resources.Load<Material>("Materiales/" + name);
            if (material == null)
            {
                material = ShapeKit.NewMaterial(fallback);
                material.name = name;
            }

            Loaded[name] = material;
            return material;
        }
    }
}
