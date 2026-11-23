"""Te casaste (escena.casa.foto_matrimonio): la foto del matrimonio a la salida de la ceremonia,
tú de oscuro y tu pareja de blanco, en un marco claro con paspartú.
"""

import fotos as f


def imagen():
    pix = f.foto(300, 240, (0.86, 0.88, 0.90), (0.80, 0.76, 0.70), horizonte=0.7, semilla=31)
    # La entrada de la iglesia o del registro civil detrás.
    pix[30:200, 40:200, :3] = (0.90, 0.87, 0.82)
    pix[60:210, 85:155, :3] = (0.45, 0.32, 0.24)
    f.persona(pix, 0.38, 1.05, 0.8, (0.18, 0.20, 0.26), f.PIELES[1], f.PELOS[0])
    f.persona(pix, 0.63, 1.05, 0.76, (0.97, 0.97, 0.96), f.PIELES[0], f.PELOS[1], pelo_largo=True, ancho=1.25)
    f._elipse(pix, 0.72, 0.52, 0.04, 0.06, (0.95, 0.75, 0.80))
    return pix


def armar():
    f.marco_fondo("foto_matrimonio", imagen(), (-0.6, -0.05, 0.0), 0.36, 0.44, madera=(0.86, 0.82, 0.74))
