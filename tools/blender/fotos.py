"""Ayudas para fotos impresas y sus marcos: fondos, personas pintadas con formas suaves y marcos de madera.

Las personas de las fotos son siluetas suaves (cabeza, pelo, hombros y ropa), con la proporción
de cabeza algo más grande que eligió Isaias para la gente (opción B). No son caras reales.
"""

import numpy as np

import comun as c
import papeles as p

PIELES = [(0.93, 0.78, 0.66), (0.82, 0.64, 0.50), (0.62, 0.45, 0.33)]
PELOS = [(0.15, 0.11, 0.09), (0.35, 0.24, 0.15), (0.55, 0.55, 0.55)]


def foto(alto, ancho, cielo, suelo, horizonte=0.62, semilla=1):
    """Fondo de foto: degradé de cielo arriba y suelo abajo, con un poco de grano de foto."""
    pix = np.ones((alto, ancho, 4))
    h = int(horizonte * alto)
    t = np.linspace(0, 1, h)[:, None, None]
    pix[:h, :, :3] = np.array(cielo) * (0.88 + 0.12 * t)
    pix[h:, :, :3] = suelo
    rng = np.random.default_rng(semilla)
    pix[..., :3] *= 1 - 0.04 * rng.random((alto, ancho, 1))
    return pix


def _elipse(pix, cy, cx, ry, rx, color, mezcla=1.0):
    alto, ancho = pix.shape[:2]
    yy, xx = np.indices((alto, ancho))
    d = ((yy - cy * alto) / (ry * alto)) ** 2 + ((xx - cx * ancho) / (rx * ancho)) ** 2
    borde = np.clip((1.0 - d) * 8, 0, 1)[..., None] * mezcla
    pix[..., :3] = pix[..., :3] * (1 - borde) + np.array(color) * borde


def persona(pix, cx, pie, alto, ropa, piel=PIELES[0], pelo=PELOS[0], pelo_largo=False, ancho=1.0):
    """Una persona de pie, de frente: `pie` es la fracción donde quedan los pies (o el borde de abajo)."""
    alto_img, ancho_img = pix.shape[:2]
    aspecto = alto_img / ancho_img
    cabeza = alto * 0.16
    cy_cabeza = pie - alto + cabeza
    if pelo_largo:  # el pelo largo va detrás: lo tapan los hombros y el cuello
        _elipse(pix, cy_cabeza + cabeza * 0.5, cx, cabeza * 1.3, cabeza * 1.0 * aspecto, pelo)
    # Cuerpo: hombros redondeados que bajan hasta el borde.
    _elipse(pix, pie - alto * 0.36, cx, alto * 0.42, cabeza * 1.25 * aspecto * ancho, ropa)
    _elipse(pix, cy_cabeza + cabeza * 1.15, cx, cabeza * 0.35, cabeza * 0.32 * aspecto, piel)
    _elipse(pix, cy_cabeza, cx, cabeza, cabeza * 0.8 * aspecto, piel)
    _elipse(pix, cy_cabeza - cabeza * 0.62, cx, cabeza * 0.5, cabeza * 0.86 * aspecto, pelo)
    # Sombra suave bajo el mentón y ojos apenas marcados, como se ven en una foto de lejos.
    _elipse(pix, cy_cabeza + cabeza * 0.9, cx, cabeza * 0.12, cabeza * 0.5 * aspecto, tuple(v * 0.8 for v in piel), 0.6)
    for lado in (-1, 1):
        _elipse(pix, cy_cabeza + cabeza * 0.05, cx + lado * cabeza * 0.3 * aspecto, cabeza * 0.07, cabeza * 0.1 * aspecto, (0.2, 0.15, 0.12), 0.7)
    _elipse(pix, cy_cabeza + cabeza * 0.5, cx, cabeza * 0.05, cabeza * 0.22 * aspecto, tuple(v * 0.75 for v in piel), 0.6)


def guagua(pix, cy, cx, tam, piel=PIELES[0]):
    """Una guagua envuelta en su ropón blanco, en brazos."""
    alto, ancho = pix.shape[:2]
    aspecto = alto / ancho
    _elipse(pix, cy, cx, tam * 0.55, tam * aspecto, (0.98, 0.98, 0.97))
    _elipse(pix, cy - tam * 0.1, cx - tam * 0.7 * aspecto, tam * 0.3, tam * 0.3 * aspecto, piel)


def revelar(pix, desenfoque=2):
    """Que parezca una foto impresa: bordes suaves, viñeta en las esquinas y un tono cálido."""
    alto, ancho = pix.shape[:2]
    img = pix[..., :3]
    for _ in range(desenfoque):
        img = (img + np.roll(img, 1, 0) + np.roll(img, -1, 0) + np.roll(img, 1, 1) + np.roll(img, -1, 1)) / 5
    yy, xx = np.indices((alto, ancho))
    r = np.hypot((yy / alto - 0.5) * 2, (xx / ancho - 0.5) * 2)
    img = img * (1 - 0.18 * np.clip(r - 0.6, 0, 1))[..., None]
    img = img * np.array((1.02, 1.0, 0.95))
    pix[..., :3] = np.clip(img, 0, 1)
    return pix


def material_foto(nombre, pix):
    pix = revelar(pix)
    return c.material(nombre, (1, 1, 1), aspereza=0.35, textura=p.imagen(nombre, pix))


def marco_fondo(nombre, pix, centro, ancho, alto, madera=(0.30, 0.20, 0.13), paspartu=True):
    """Foto enmarcada en el muro del fondo (se ve desde -z, como Picture en SceneObjectBuilder.cs)."""
    x, y, z = centro
    c.caja("Marco", (x, y, z), (ancho, alto, 0.03), c.material("marco_" + nombre, madera, aspereza=0.45), bisel=0.004)
    if paspartu:
        c.caja("Paspartu", (x, y, z - 0.016), (ancho * 0.88, alto * 0.86, 0.003), c.material("paspartu_" + nombre, (0.93, 0.91, 0.87), aspereza=0.9))
    escala = 0.72 if paspartu else 0.86
    lamina = c.caja("Foto", (x, y, z - 0.019), (ancho * escala, alto * escala, 0.002), material_foto(nombre, pix))
    c.uv_plano_frente(lamina)
    return lamina


def marco_lado(nombre, pix, y, z, alto, ancho, madera=(0.30, 0.20, 0.13), x=0.025):
    """Foto enmarcada en un muro que mira hacia +x (muro lateral)."""
    c.caja("Marco", (x + 0.015, y, z), (0.03, alto, ancho), c.material("marco_" + nombre, madera, aspereza=0.45), bisel=0.004)
    c.caja("Paspartu", (x + 0.031, y, z), (0.003, alto * 0.86, ancho * 0.88), c.material("paspartu_" + nombre, (0.93, 0.91, 0.87), aspereza=0.9))
    lamina = c.caja("Foto", (x + 0.034, y, z), (0.002, alto * 0.72, ancho * 0.72), material_foto(nombre, pix))
    p.uv_plano_lado(lamina)


def cinta_luto(nombre, centro, largo, giro_x=None, giro_z=None):
    """La cinta negra de luto cruzada en la esquina de un marco."""
    o = c.caja(nombre, centro, largo, c.material("cinta_luto", (0.05, 0.05, 0.06), aspereza=0.5))
    if giro_x is not None:
        c.girar_x(o, giro_x)
    if giro_z is not None:
        c.girar_z(o, giro_z)
    return o
