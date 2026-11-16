#nullable enable

using UnityEngine;

namespace MistakesLife.Unity
{
    /// <summary>
    /// La vibración que acompaña a cada sonido (docs/archivo/diseno-app.md, versión 4). En Android es un
    /// toque muy corto (15 milisegundos); en el editor y en otros equipos no hace nada.
    /// </summary>
    public static class Haptics
    {
        private const long LightMilliseconds = 15;

#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject? _vibrator;
        private static bool _failed;
#endif

        /// <summary>Un toque leve. Si el equipo no puede, no pasa nada.</summary>
        public static void Light()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_failed)
            {
                return;
            }

            try
            {
                AndroidJavaObject? vibrator = _vibrator;
                if (vibrator == null)
                {
                    using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                    using AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                    vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                    _vibrator = vibrator;
                }

                vibrator.Call("vibrate", LightMilliseconds);
            }
            catch (System.Exception)
            {
                _failed = true;
            }
#endif
        }

        /// <summary>
        /// La vibración larga de Unity. Casi no se usa (es fuerte), pero estar escrita aquí hace
        /// que Unity pida el permiso de vibrar en Android, que también necesita <see cref="Light"/>.
        /// </summary>
        public static void Strong()
        {
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }
    }
}
