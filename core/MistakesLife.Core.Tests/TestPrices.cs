using System;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Tests
{
    /// <summary>
    /// Para las pruebas que solo pasan el calendario (sin el sistema de economía): pone
    /// los precios del país en los de hoy (índice 1), como los tiene un adulto de unos
    /// 25 años, para comparar montos exactos con los de los paquetes.
    /// </summary>
    public static class TestPrices
    {
        public static void SetToday(World world, string countryId = "pais.chile") =>
            world.EconomyOf(countryId)!.Restore(1, Array.Empty<double>());
    }
}
