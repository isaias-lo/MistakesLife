#nullable enable

using System.Collections.Generic;
using MistakesLife.Core.Game;
using MistakesLife.Core.Presentation;
using UnityEngine;

namespace MistakesLife.Unity
{
    /// <summary>
    /// Pantalla de prueba (U2): comprueba que el núcleo corre dentro de Unity.
    /// Dibuja la foto de la partida con botones simples; la pantalla de verdad llega en U3.
    /// Se crea sola al apretar Play, en cualquier escena: no hay que configurar nada.
    /// Regla: aquí no se decide nada; todo lo hace LifePresenter (núcleo).
    /// </summary>
    public sealed class LifeScreen : MonoBehaviour
    {
        /// <summary>Ancho de diseño: la pantalla se escala para que quepa en cualquier celular.</summary>
        private const float DesignWidth = 540f;

        private const int JournalLines = 8;

        private readonly List<string> _journal = new List<string>();
        private LifePresenter? _presenter;
        private string? _error;
        private Vector2 _scroll;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateOnPlay()
        {
            if (FindAnyObjectByType<LifeScreen>() == null)
            {
                new GameObject(nameof(LifeScreen)).AddComponent<LifeScreen>();
            }
        }

        private void Start()
        {
            try
            {
                // Fuera de la simulación sí se puede usar el reloj: solo elige el número de partida.
                ulong seed = (ulong)System.DateTime.UtcNow.Ticks;
                _presenter = new LifePresenter(new LifeSession(CoreLoader.LoadContent(), seed));
                ReadJournal();
            }
            catch (System.Exception ex)
            {
                _error = ex.Message;
                Debug.LogException(ex);
            }
        }

        private void OnGUI()
        {
            float scale = Screen.width / DesignWidth;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            GUI.skin.label.wordWrap = true;
            GUI.skin.label.fontSize = 18;
            GUI.skin.button.fontSize = 18;

            GUILayout.BeginArea(new Rect(10, 10, DesignWidth - 20, (Screen.height / scale) - 20));
            _scroll = GUILayout.BeginScrollView(_scroll);
            if (_error != null)
            {
                GUILayout.Label("Error: " + _error);
            }
            else if (_presenter != null)
            {
                Draw(_presenter, _presenter.Snapshot());
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void Draw(LifePresenter presenter, LifeSnapshot life)
        {
            GUILayout.Label($"<b>{life.PlayerName}</b>, {life.AgeLabel}");
            GUILayout.Label($"{life.Place}\n{life.DateLabel} · Partida {life.Seed}");
            GUILayout.Label($"Salud {life.Health} · Felicidad {life.Happiness} · Inteligencia {life.Smarts} · {life.MoneyLabel}");
            GUILayout.Space(10);

            switch (life.Phase)
            {
                case LifePhase.Living:
                    if (GUILayout.Button("Seguir viviendo", GUILayout.Height(50)))
                    {
                        presenter.AdvanceUntilSomething();
                        ReadJournal();
                    }

                    break;
                case LifePhase.Choosing:
                    GUILayout.Label(life.PendingEvent!.Text);
                    for (int i = 0; i < life.PendingEvent.Options.Count; i++)
                    {
                        if (GUILayout.Button(life.PendingEvent.Options[i], GUILayout.MinHeight(44)))
                        {
                            presenter.Choose(i);
                            ReadJournal();
                        }
                    }

                    break;
                case LifePhase.ChoosingHeir:
                    for (int i = 0; i < life.Heirs.Count; i++)
                    {
                        PersonView heir = life.Heirs[i];
                        if (GUILayout.Button($"Seguir con {heir.Name} ({heir.AgeInYears})", GUILayout.MinHeight(44)))
                        {
                            presenter.ContinueWith(i);
                            ReadJournal();
                        }
                    }

                    break;
                case LifePhase.Over:
                    GUILayout.Label("Fin de la partida.");
                    break;
            }

            GUILayout.Space(10);
            foreach (string line in _journal)
            {
                GUILayout.Label(line);
            }
        }

        private void ReadJournal()
        {
            foreach (JournalLine line in _presenter!.TakeNewJournal())
            {
                _journal.Insert(0, $"{line.AgeLabel}: {line.Text}");
            }

            if (_journal.Count > JournalLines)
            {
                _journal.RemoveRange(JournalLines, _journal.Count - JournalLines);
            }
        }
    }
}
