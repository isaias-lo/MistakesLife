#nullable enable

using System.Collections.Generic;
using UnityEngine;

namespace MistakesLife.Unity
{
    /// <summary>Los sonidos de la interfaz (docs/archivo/diseno-app.md, versión 4: "cada cosa suena").</summary>
    public enum Sound
    {
        /// <summary>Abrir la libreta o cambiar de hoja: una hoja que se da vuelta.</summary>
        PageTurn,

        /// <summary>Una nota azul: toque de lápiz.</summary>
        PencilTap,

        /// <summary>Una nota roja: golpe grave y seco.</summary>
        RedMark,

        /// <summary>Aprobar el semestre o el año: dos notas que suben.</summary>
        Approved,

        /// <summary>Repetir: dos notas que bajan.</summary>
        Repeat,

        /// <summary>Tocar a alguien: clic suave de foto.</summary>
        PhotoClick,

        /// <summary>Alguien se fue: puerta que se cierra lejos.</summary>
        DoorFar,

        /// <summary>Pagar (el arancel, una cuenta): caja registradora apagada.</summary>
        Register,

        /// <summary>El botón amarillo: golpe de madera firme.</summary>
        WoodKnock,
    }

    /// <summary>
    /// Toca los sonidos cortos de la interfaz, siempre con una vibración leve. No hay archivos de
    /// audio: cada sonido se arma por código (tonos y ruido con su forma), igual que las piezas de
    /// la interfaz se dibujan sin imágenes. Para cambiar cómo suena algo se cambia solo su caso en
    /// <see cref="Build"/>; quien lo toca no se entera. Más adelante se pueden reemplazar por
    /// grabaciones reales con el mismo nombre.
    /// </summary>
    public static class Sounds
    {
        private const int Rate = 44100;

        private static readonly Dictionary<Sound, AudioClip> Clips = new Dictionary<Sound, AudioClip>();
        private static AudioSource? _source;

        /// <summary>Si suenan (más adelante, un ajuste del menú).</summary>
        public static bool Enabled { get; set; } = true;

        /// <summary>Toca un sonido y da una vibración leve.</summary>
        public static void Play(Sound sound)
        {
            // Fuera del juego (las capturas automáticas corren en el editor) no suena nada.
            if (!Enabled || !Application.isPlaying)
            {
                return;
            }

            if (_source == null)
            {
                var go = new GameObject("Sonidos de la interfaz");
                Object.DontDestroyOnLoad(go);
                _source = go.AddComponent<AudioSource>();
                _source.playOnAwake = false;
            }

            if (!Clips.TryGetValue(sound, out AudioClip? clip))
            {
                clip = Build(sound);
                Clips[sound] = clip;
            }

            _source.PlayOneShot(clip, 0.7f);
            Haptics.Light();
        }

        /// <summary>Arma el sonido: cada uno dura menos de medio segundo y tiene su propia forma.</summary>
        private static AudioClip Build(Sound sound)
        {
            float[] samples = sound switch
            {
                // Ruido que sube y baja, filtrado: el roce del papel.
                Sound.PageTurn => Noise(0.22f, 0.35f, t => Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.22f)), 7),
                // Clic agudo y muy corto, con un poco de madera.
                Sound.PencilTap => Mix(Noise(0.03f, 0.5f, t => Decay(t, 90f), 2), Tone(0.06f, 1800f, 0.25f, t => Decay(t, 70f))),
                // Golpe grave y seco.
                Sound.RedMark => Mix(Tone(0.18f, 95f, 0.9f, t => Decay(t, 22f)), Noise(0.04f, 0.3f, t => Decay(t, 60f), 4)),
                // Dos notas que suben (do y sol).
                Sound.Approved => Then(Tone(0.14f, 523f, 0.45f, t => Decay(t, 14f)), Tone(0.26f, 784f, 0.45f, t => Decay(t, 9f))),
                // Dos notas que bajan.
                Sound.Repeat => Then(Tone(0.14f, 440f, 0.45f, t => Decay(t, 14f)), Tone(0.26f, 311f, 0.45f, t => Decay(t, 9f))),
                // Clic de foto: dos golpecitos muy juntos.
                Sound.PhotoClick => Then(Noise(0.02f, 0.45f, t => Decay(t, 120f), 1), Noise(0.05f, 0.35f, t => Decay(t, 80f), 1)),
                // Puerta lejos: golpe grave, apagado y suave.
                Sound.DoorFar => Mix(Tone(0.35f, 70f, 0.5f, t => Decay(t, 9f)), Noise(0.3f, 0.12f, t => Decay(t, 12f), 12)),
                // Caja registradora apagada: el cajón (ruido) y una campanita lejana.
                Sound.Register => Mix(Noise(0.08f, 0.35f, t => Decay(t, 40f), 3), Then(Silence(0.05f), Tone(0.3f, 1320f, 0.18f, t => Decay(t, 12f)))),
                // Madera firme: tono medio que se apaga rápido.
                Sound.WoodKnock => Mix(Tone(0.12f, 220f, 0.7f, t => Decay(t, 35f)), Tone(0.08f, 660f, 0.25f, t => Decay(t, 50f))),
                _ => Silence(0.01f),
            };

            AudioClip clip = AudioClip.Create("Sonido " + sound, samples.Length, 1, Rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static float Decay(float t, float speed) => Mathf.Exp(-t * speed);

        private static float[] Tone(float seconds, float frequency, float volume, System.Func<float, float> shape)
        {
            var samples = new float[(int)(seconds * Rate)];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)Rate;
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * volume * shape(t) * Edge(i, samples.Length);
            }

            return samples;
        }

        /// <summary>Ruido suavizado (más suave cuanto más grande <paramref name="smooth"/>). Siempre el mismo: la semilla es fija.</summary>
        private static float[] Noise(float seconds, float volume, System.Func<float, float> shape, int smooth)
        {
            uint state = 7u;
            var samples = new float[(int)(seconds * Rate)];
            float last = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)Rate;
                state = (state * 1664525u) + 1013904223u;
                float white = (state >> 8) / 8388608f - 1f;
                last += (white - last) / Mathf.Max(1, smooth);
                samples[i] = last * volume * shape(t) * Edge(i, samples.Length) * Mathf.Sqrt(smooth);
            }

            return samples;
        }

        private static float[] Silence(float seconds) => new float[(int)(seconds * Rate)];

        /// <summary>Entra y sale en 2 ms, para que no haga clic al empezar ni al terminar.</summary>
        private static float Edge(int i, int length)
        {
            const int Fade = Rate / 500;
            return Mathf.Clamp01(Mathf.Min(i, length - 1 - i) / (float)Fade);
        }

        private static float[] Mix(float[] a, float[] b)
        {
            var samples = new float[Mathf.Max(a.Length, b.Length)];
            for (int i = 0; i < samples.Length; i++)
            {
                samples[i] = Mathf.Clamp((i < a.Length ? a[i] : 0f) + (i < b.Length ? b[i] : 0f), -1f, 1f);
            }

            return samples;
        }

        private static float[] Then(float[] a, float[] b)
        {
            var samples = new float[a.Length + b.Length];
            a.CopyTo(samples, 0);
            b.CopyTo(samples, a.Length);
            return samples;
        }
    }
}
