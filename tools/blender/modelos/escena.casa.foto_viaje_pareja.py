"""El viaje con tu pareja (escena.casa.foto_viaje_pareja): una foto de playa con cielo, mar y arena,
y ustedes dos chiquitos en la orilla, al lado de la foto de la pareja.
"""

import fotos as f


def imagen():
    pix = f.foto(200, 260, (0.62, 0.80, 0.94), (0.92, 0.83, 0.62), horizonte=0.45, semilla=41)
    pix[90:140, :, :3] = (0.20, 0.50, 0.72)
    pix[136:142, :, :3] = (0.95, 0.96, 0.96)
    f._elipse(pix, 0.15, 0.8, 0.06, 0.05, (1.0, 0.96, 0.8))
    f.persona(pix, 0.42, 0.9, 0.36, (0.25, 0.55, 0.75), f.PIELES[1], f.PELOS[0])
    f.persona(pix, 0.5, 0.9, 0.34, (0.95, 0.55, 0.35), f.PIELES[0], f.PELOS[1], pelo_largo=True)
    return pix


def armar():
    f.marco_fondo("foto_viaje_pareja", imagen(), (-0.55, -0.1, 0.0), 0.36, 0.28, madera=(0.55, 0.40, 0.26), paspartu=False)
