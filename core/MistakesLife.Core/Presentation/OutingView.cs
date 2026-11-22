namespace MistakesLife.Core.Presentation
{
    /// <summary>
    /// La salida se ve en la escena (D5c2, boceto BSalida): el lugar donde fueron, con el jugador
    /// y con quien fue. Se arma con <see cref="LifePresenter.Outing"/> antes o después de hacerla.
    /// </summary>
    public sealed class OutingView
    {
        public OutingView(string placeId, string shape, string title, HomeMemberView player, HomeMemberView? companion)
        {
            PlaceId = placeId;
            Shape = shape;
            Title = title;
            Player = player;
            Companion = companion;
        }

        /// <summary>El lugar del barrio ("lugar.barrio.mall").</summary>
        public string PlaceId { get; }

        /// <summary>Su forma 3D ("mall", "fonda"): Unity dibuja ahí la escena de la salida.</summary>
        public string Shape { get; }

        /// <summary>"Mall y cine · con Tomás", o solo el lugar si fue sin nadie.</summary>
        public string Title { get; }

        public HomeMemberView Player { get; }

        /// <summary>Con quién fue; null si fue sin nadie.</summary>
        public HomeMemberView? Companion { get; }

        /// <summary>Cambia si cambia el lugar o la persona (para no volver a armar la escena).</summary>
        public string Signature => PlaceId + "|" + (Companion?.Id.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "-") + "|" + Player.AgeInYears;
    }
}
