using System;
using System.Collections.Generic;
using MistakesLife.Core.Time;

namespace MistakesLife.Core.Devices
{
    /// <summary>
    /// El celular de un personaje: desde cuándo lo tiene y qué apps tiene
    /// instaladas (ids de packs/*/apps/, por ejemplo "app.instante"). Si lo
    /// pierde o se lo roban, se pierde el celular con sus apps; el correo sigue
    /// existiendo (es una cuenta, no está en el aparato).
    /// </summary>
    public sealed class Phone
    {
        private readonly List<string> _apps = new List<string>();

        public Phone(GameDate since)
        {
            Since = since;
        }

        /// <summary>Desde cuándo tiene este celular.</summary>
        public GameDate Since { get; }

        /// <summary>Las apps instaladas, en el orden en que las instaló.</summary>
        public IReadOnlyList<string> Apps => _apps;

        public bool Has(string appId) => _apps.Contains(appId);

        /// <summary>Instala una app (si ya la tiene, no pasa nada).</summary>
        public void Install(string appId)
        {
            if (string.IsNullOrWhiteSpace(appId))
            {
                throw new ArgumentException("Falta el id de la app.", nameof(appId));
            }

            if (!Has(appId))
            {
                _apps.Add(appId);
            }
        }

        /// <summary>Desinstala una app (si no la tiene, no pasa nada).</summary>
        public void Uninstall(string appId) => _apps.Remove(appId);
    }
}
