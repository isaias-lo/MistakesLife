#nullable enable

using System;
using System.Collections.Generic;
using MistakesLife.Core.Content;
using MistakesLife.Core.Game;
using MistakesLife.Core.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MistakesLife.Unity
{
    /// <summary>
    /// La primera impresión: el menú de inicio (continuar, nueva vida, cómo se juega),
    /// confirmar antes de perder una vida en curso y elegir entre algunos nacimientos.
    /// Es una capa que tapa la pantalla de la vida. Aquí no se decide nada del juego:
    /// las vidas las crea el núcleo (<see cref="LifeSession"/>) y los textos salen de las claves ui.*.
    /// </summary>
    public sealed class StartMenu
    {
        /// <summary>Cuántas vidas se ofrecen para elegir al empezar.</summary>
        public const int BirthChoices = 3;

        private readonly Transform _parent;
        private readonly ContentDatabase _content;
        private RectTransform? _layer;

        public StartMenu(Transform parent, ContentDatabase content)
        {
            _parent = parent;
            _content = content;
        }

        /// <summary>Se llama con la vida elegida para empezar a jugarla.</summary>
        public Action<LifePresenter>? LifeChosen { get; set; }

        /// <summary>Se llama al tocar "Continuar".</summary>
        public Action? ContinueChosen { get; set; }

        /// <summary>Se llama al tocar "Cómo se juega".</summary>
        public Action? HelpChosen { get; set; }

        /// <summary>La vida en curso (para "Continuar"), o null si no hay.</summary>
        public Func<LifeSnapshot?>? CurrentLife { get; set; }

        public bool IsOpen => _layer != null;

        /// <summary>
        /// El menú de inicio. Si hay una vida en curso, el primer botón la continúa
        /// ("Continuar: Ana Pérez, 34 años").
        /// </summary>
        public void ShowMenu()
        {
            LifeSnapshot? current = CurrentLife?.Invoke();
            RectTransform card = OpenLayer("Menu de inicio");
            UiFactory.Label(card, Text("ui.inicio.titulo"), 84, UiFactory.Ink, FontStyles.Normal, Face.Title).alignment = TextAlignmentOptions.Center;
            UiFactory.Label(card, Text("ui.inicio.lema"), 36, UiFactory.MutedInk).alignment = TextAlignmentOptions.Center;
            Space(card, 24);

            bool canContinue = current != null && current.Phase != LifePhase.Over;
            if (canContinue)
            {
                string label = Text("ui.inicio.continuar", current!.PlayerName, current.AgeLabel);
                UiFactory.MakeButton(card, label, UiFactory.Accent, UiFactory.AccentText, () => { Close(); ContinueChosen?.Invoke(); });
            }

            UiFactory.MakeButton(card, Text("ui.inicio.nueva_vida"), canContinue ? UiFactory.Option : UiFactory.Accent, canContinue ? UiFactory.Ink : UiFactory.AccentText,
                () =>
                {
                    if (canContinue)
                    {
                        ShowReplaceWarning();
                    }
                    else
                    {
                        ShowBirths();
                    }
                });
            UiFactory.MakeButton(card, Text("ui.inicio.como_jugar"), UiFactory.Card, UiFactory.MutedInk, () => HelpChosen?.Invoke());
        }

        /// <summary>Antes de empezar otra vida se avisa que la vida en curso se pierde.</summary>
        private void ShowReplaceWarning()
        {
            RectTransform card = OpenLayer("Confirmar nueva vida");
            UiFactory.Label(card, Text("ui.inicio.reemplazar"), 40, UiFactory.Ink, FontStyles.Normal, Face.Title);
            Space(card, 12);
            UiFactory.MakeButton(card, Text("ui.inicio.reemplazar_si"), UiFactory.Bad, UiFactory.AccentText, ShowBirths);
            UiFactory.MakeButton(card, Text("ui.boton.cancelar"), UiFactory.Option, UiFactory.Ink, ShowMenu);
        }

        /// <summary>
        /// Algunas vidas posibles, cada una con su nombre, lugar, hogar y padres.
        /// El jugador elige una o pide otras. Cada vida es una partida distinta del núcleo.
        /// </summary>
        public void ShowBirths()
        {
            RectTransform card = OpenLayer("Elegir vida");
            UiFactory.Label(card, Text("ui.nacer.titulo"), 52, UiFactory.Ink, FontStyles.Normal, Face.Title);
            UiFactory.Label(card, Text("ui.nacer.ayuda"), 32, UiFactory.MutedInk);

            // Fuera de la simulación sí se puede usar el reloj: solo elige los números de partida.
            ulong first = (ulong)DateTime.UtcNow.Ticks;
            var lives = new List<LifePresenter>();
            for (int i = 0; i < BirthChoices; i++)
            {
                lives.Add(new LifePresenter(new LifeSession(_content, first + (ulong)i * 7919UL)));
            }

            foreach (LifePresenter life in lives)
            {
                DrawBirth(card, life);
            }

            UiFactory.MakeButton(card, Text("ui.nacer.otras"), UiFactory.Option, UiFactory.Ink, ShowBirths, 34, UiFactory.MinButtonHeight, 20);
            UiFactory.MakeButton(card, Text("ui.boton.cancelar"), UiFactory.Card, UiFactory.MutedInk, ShowMenu, 34, UiFactory.MinButtonHeight, 20);
        }

        public void Close()
        {
            if (_layer != null)
            {
                UnityEngine.Object.Destroy(_layer.gameObject);
                _layer = null;
            }
        }

        private void DrawBirth(RectTransform parent, LifePresenter life)
        {
            BirthView birth = life.Birth();
            RectTransform box = UiFactory.Column(parent, "Vida posible", 28, 6, UiFactory.Background);
            UiFactory.Label(box, $"{birth.Name} · {birth.SexLabel}", 40, UiFactory.Ink, FontStyles.Normal, Face.Title);
            UiFactory.Label(box, birth.Place, 30, UiFactory.MutedInk);
            UiFactory.Label(box, birth.HomeLabel, 30, UiFactory.Ink);
            foreach (PersonView person in birth.Parents)
            {
                UiFactory.Label(box, $"{person.Relation}: {person.Name} ({person.AgeInYears})", 30, UiFactory.Ink);
            }

            Space(box, 6);
            UiFactory.MakeButton(box, Text("ui.nacer.elegir"), UiFactory.Accent, UiFactory.AccentText, () => { Close(); LifeChosen?.Invoke(life); }, 34, UiFactory.MinButtonHeight, 20);
        }

        /// <summary>Abre una capa nueva (cerrando la anterior) con una columna centrada con scroll.</summary>
        private RectTransform OpenLayer(string name)
        {
            Close();
            _layer = UiFactory.Overlay(_parent, name, UiFactory.Background);
            RectTransform page = UiFactory.Column(_layer, "Pagina", 48, 0);
            UiFactory.Stretch(page);
            RectTransform scroll = UiFactory.ScrollColumn(page, "Contenido", 40, 22);
            return scroll;
        }

        private static void Space(Transform parent, int height)
        {
            UiFactory.Child(parent, "Espacio").gameObject.AddComponent<LayoutElement>().preferredHeight = height;
        }

        private string Text(string key, params object[] values) =>
            values.Length == 0 ? _content.Text(key) : string.Format(System.Globalization.CultureInfo.InvariantCulture, _content.Text(key), values);
    }
}
