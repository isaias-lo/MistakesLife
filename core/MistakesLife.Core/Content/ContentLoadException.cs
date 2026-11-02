using System;
using System.Collections.Generic;
using System.Linq;

namespace MistakesLife.Core.Content
{
    /// <summary>Se lanza cuando uno o más paquetes tienen errores.</summary>
    public sealed class ContentLoadException : Exception
    {
        public ContentLoadException(IReadOnlyList<ContentError> errors)
            : base("Los paquetes tienen errores:\n" + string.Join("\n", errors.Select(e => " - " + e)))
        {
            Errors = errors;
        }

        public IReadOnlyList<ContentError> Errors { get; }
    }
}
