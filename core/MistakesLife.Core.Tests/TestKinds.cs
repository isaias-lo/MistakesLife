namespace MistakesLife.Core.Tests
{
    /// <summary>
    /// Tipos de prueba, para correrlas por separado. Las de muchas vidas (juegan cientos de
    /// vidas para revisar el equilibrio: que todos los eventos aparezcan, que los porcentajes
    /// se parezcan a los reales) son las lentas; en GitHub van en su propia fila, al mismo
    /// tiempo que las demás, así las rápidas avisan antes.
    /// <code>
    /// dotnet test MistakesLife.sln --filter "Tipo!=MuchasVidas"   (las rápidas, unos minutos)
    /// dotnet test MistakesLife.sln --filter "Tipo=MuchasVidas"    (las de muchas vidas)
    /// </code>
    /// </summary>
    public static class TestKinds
    {
        /// <summary>Nombre de la marca (<c>[Trait(TestKinds.Name, ...)]</c>).</summary>
        public const string Name = "Tipo";

        /// <summary>Juega cientos de vidas para revisar el equilibrio del contenido.</summary>
        public const string ManyLives = "MuchasVidas";
    }
}
