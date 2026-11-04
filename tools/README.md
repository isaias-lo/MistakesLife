# Herramientas

Programas de apoyo que **no** van dentro del juego. Todos usan el núcleo (`core/`).

| Herramienta | Para qué | Cuándo llega |
|---|---|---|
| Consola de juego (`MistakesLife.Console`) | Jugar una vida completa en texto: `dotnet run --project tools/MistakesLife.Console`. Con `g` se guarda en `partida.json`; para seguirla: `dotnet run --project tools/MistakesLife.Console -- --cargar partida.json` | Lista |
| Simulador de vidas (`MistakesLife.Simulator`) | Correr miles de vidas automáticas y ver estadísticas: `dotnet run --project tools/MistakesLife.Simulator -- 1000 25 --out informe.md` (vidas, edad final) | Lista |
| Exportador a Unity (`MistakesLife.UnityExport`) | Copiar el núcleo compilado y los paquetes (en un solo archivo) dentro de `unity/`: `dotnet run --project tools/MistakesLife.UnityExport` | Lista |
| Validador de paquetes | Revisar que cada paquete esté completo antes de publicarlo | Más adelante |
| Conversor de planillas | Pasar planillas (Sheets/Excel) a JSON | Más adelante |
