"""La foto con tu pareja (escena.casa.foto_pareja): los dos juntos en un parque al atardecer,
en un marco de madera con paspartú, colgada en el muro del fondo.
"""

import fotos as f


def imagen(apagada=False):
    pix = f.foto(240, 300, (0.98, 0.78, 0.62), (0.45, 0.55, 0.35), horizonte=0.66, semilla=21)
    f._elipse(pix, 0.66, 0.15, 0.12, 0.2, (0.38, 0.50, 0.32))
    f._elipse(pix, 0.64, 0.88, 0.14, 0.18, (0.36, 0.48, 0.30))
    f.persona(pix, 0.38, 1.08, 0.82, (0.30, 0.38, 0.55), f.PIELES[1], f.PELOS[0])
    f.persona(pix, 0.62, 1.08, 0.78, (0.75, 0.35, 0.38), f.PIELES[0], f.PELOS[1], pelo_largo=True)
    if apagada:
        gris = pix[..., :3].mean(axis=2, keepdims=True)
        pix[..., :3] = pix[..., :3] * 0.45 + gris * 0.55
    return pix


def armar():
    f.marco_fondo("foto_pareja", imagen(), (0.0, 0.0, 0.0), 0.5, 0.4)
