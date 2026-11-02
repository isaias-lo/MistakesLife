# MistakesLife

Un simulador de vida para celulares que busca ser una **réplica de la vida real**: se vive semana a semana, desde que naces, y las decisiones (estudiar, copiar en una prueba, migrar, emprender) dejan consecuencias que pueden aparecer años después.

> Estado: **Fase 1, núcleo en texto.** Todavía no hay gráficos. Se está construyendo la simulación para comprobar que vivir por semanas es entretenido y que las decisiones pesan.

## Cómo está organizado

| Carpeta | Qué hay |
|---|---|
| [`core/`](core) | El "cerebro" del juego: la simulación, en C# puro, con sus pruebas |
| [`packs/`](packs) | Contenido en JSON: países, ciudades, carreras, eventos |
| [`tools/`](tools) | Herramientas: jugar en texto, simular miles de vidas, validar paquetes |
| [`unity/`](unity) | El proyecto de Unity (gráficos), que llega en la fase 2 |
| [`docs/`](docs) | Arquitectura y hoja de ruta |

La regla de oro: **las reglas viven en el núcleo, el contenido en los paquetes y Unity solo dibuja.** Así se pueden sumar países y carreras sin reescribir el juego.

## Probarlo en tu computador

1. Instala el [SDK de .NET 8](https://dotnet.microsoft.com/download/dotnet/8.0).
2. En una terminal, dentro de esta carpeta:

```bash
dotnet test MistakesLife.sln
```

Si todo está bien verás `Passed!`. GitHub corre esas mismas pruebas solo en cada pull request.

## Jugar una vida en texto

```bash
dotnet run --project tools/MistakesLife.Console
```

Naces en Puente Alto y vives semana a semana hasta los 25 años. Enter avanza hasta que pase algo, `a` avanza un año, `e` muestra tu estado, `d` tu diario y `q` sale. Al empezar verás un **número de partida**: con `dotnet run --project tools/MistakesLife.Console -- 2026` repites exactamente la misma vida.

## Revisar el equilibrio con el simulador

```bash
dotnet run --project tools/MistakesLife.Simulator -- 1000 25
```

Juega 1000 vidas automáticas hasta los 25 años (cada personaje decide según su personalidad y su hogar) y muestra un informe: cuántas vidas viven cada evento, cuáles nunca aparecen, cómo terminan las vidas y qué pasa con las semillas. Úsalo después de cambiar contenido. Con `--out informe.md` lo guarda en un archivo.

## Documentos

- [Documento de diseño](https://claude.ai/artifact/TLyFxfHXuytpZFXHUHY3B9)
- [Documento técnico](https://claude.ai/artifact/Rv74yh2pBA6kTHmLLQ7nSL)
- [Reglas para agentes de IA](CLAUDE.md)
