"""Ayudas para papeles: hojas impresas, sobres, sellos y números dibujados en la textura.

Las texturas se dibujan como arreglos (alto, ancho, 4) con la fila 0 arriba, como se lee un papel.
Las posiciones dentro de la hoja van en fracciones (0 a 1), para que no dependan de la resolución.
Todo lo "escrito" son renglones y bloques: no hay letras reales, igual que un papel visto de lejos.
"""

import math

import numpy as np

import comun as c

MESA = 0.44  # cara de arriba de la mesa (ancla.mesa)
PAPEL = (0.96, 0.95, 0.91)
TINTA = (0.22, 0.22, 0.26)
AZUL_LAPIZ = (0.16, 0.24, 0.55)
ROJO = (0.78, 0.16, 0.14)

# Números de 3 x 5 cuadritos para notas, puntajes y fechas.
_NUMEROS = {
    "0": ["111", "101", "101", "101", "111"],
    "1": ["010", "110", "010", "010", "111"],
    "2": ["111", "001", "111", "100", "111"],
    "3": ["111", "001", "111", "001", "111"],
    "4": ["101", "101", "111", "001", "001"],
    "5": ["111", "100", "111", "001", "111"],
    "6": ["111", "100", "111", "101", "111"],
    "7": ["111", "001", "010", "010", "010"],
    "8": ["111", "101", "111", "101", "111"],
    "9": ["111", "101", "111", "001", "111"],
    ",": ["000", "000", "000", "010", "100"],
}


def hoja(alto, ancho, fondo=PAPEL, semilla=1, grano=0.015):
    """Hoja en blanco con un poco de grano de papel."""
    pix = np.ones((alto, ancho, 4))
    pix[..., :3] = fondo
    if grano > 0:
        rng = np.random.default_rng(semilla)
        pix[..., :3] *= 1 - grano * rng.random((alto, ancho, 1))
    return pix


def _px(pix, arriba, abajo, izq, der):
    alto, ancho = pix.shape[:2]
    return int(arriba * alto), max(int(abajo * alto), int(arriba * alto) + 1), int(izq * ancho), max(int(der * ancho), int(izq * ancho) + 1)


def rect(pix, arriba, abajo, izq, der, color):
    """Rectángulo lleno, en fracciones de la hoja."""
    y0, y1, x0, x1 = _px(pix, arriba, abajo, izq, der)
    pix[y0:y1, x0:x1, :3] = color


def borde(pix, arriba, abajo, izq, der, color, grueso=2):
    y0, y1, x0, x1 = _px(pix, arriba, abajo, izq, der)
    pix[y0:y0 + grueso, x0:x1, :3] = color
    pix[y1 - grueso:y1, x0:x1, :3] = color
    pix[y0:y1, x0:x0 + grueso, :3] = color
    pix[y0:y1, x1 - grueso:x1, :3] = color


def renglones(pix, arriba, abajo, izq, der, color=TINTA, semilla=1, paso=0.035, grueso=0.4, mano=False):
    """Renglones de texto: palabras como trazos cortos. `mano` las hace un poco chuecas (escritas a mano)."""
    alto, ancho = pix.shape[:2]
    rng = np.random.default_rng(semilla)
    alto_linea = max(1, int(paso * alto * grueso))
    y = arriba
    while y + paso <= abajo + 1e-6:
        fin = der - rng.random() * (der - izq) * (0.45 if rng.random() < 0.25 else 0.08)
        x = izq
        yy = int(y * alto)
        while x < fin:
            palabra = (0.03 + rng.random() * 0.09) * (der - izq) * 2
            x1 = min(x + palabra, fin)
            desvio = int(rng.integers(-1, 2)) if mano else 0
            pix[yy + desvio:yy + desvio + alto_linea, int(x * ancho):int(x1 * ancho), :3] = color
            x = x1 + 0.02 * (der - izq) * 2
        y += paso


def numero(pix, texto, arriba, izq, alto_frac, color, grueso=1.0):
    """Escribe un número con los cuadritos de 3 x 5 (la altura en fracción de la hoja)."""
    alto, ancho = pix.shape[:2]
    celda = max(1, int(alto_frac * alto / 5))
    y0, x = int(arriba * alto), int(izq * ancho)
    for car in texto:
        patron = _NUMEROS.get(car)
        if patron is None:
            x += celda * 2
            continue
        for fila, linea in enumerate(patron):
            for col, v in enumerate(linea):
                if v == "1":
                    pix[y0 + fila * celda:y0 + (fila + 1) * celda, x + col * celda:x + col * celda + max(1, int(celda * grueso)), :3] = color
        x += celda * 4


def sello(pix, cy, cx, radio, color, semilla=1, opaco=0.8):
    """Timbre redondo de goma: anillo doble con renglones adentro, con tinta pareja a medias."""
    alto, ancho = pix.shape[:2]
    rng = np.random.default_rng(semilla)
    yy, xx = np.indices((alto, ancho))
    r = np.hypot((yy - cy * alto), (xx - cx * ancho)) / (radio * ancho)
    marca = ((r > 0.86) & (r < 1.0)) | ((r > 0.66) & (r < 0.74)) | ((r < 0.5) & (np.abs(yy - cy * alto) % max(3, int(radio * ancho * 0.25)) < 2))
    marca &= rng.random((alto, ancho)) < 0.85
    a = opaco * marca[..., None]
    pix[..., :3] = pix[..., :3] * (1 - a) + np.array(color) * a


def circulo_a_mano(pix, cy, cx, ry, rx, color, grueso=0.12):
    """Un círculo hecho con plumón alrededor de algo (un aviso marcado, una fecha)."""
    alto, ancho = pix.shape[:2]
    yy, xx = np.indices((alto, ancho))
    r = np.hypot((yy - cy * alto) / (ry * alto), (xx - cx * ancho) / (rx * ancho))
    pix[(r > 1 - grueso) & (r < 1), :3] = color


def firma(pix, cy, cx, ancho_frac, color=AZUL_LAPIZ, semilla=1):
    """Una firma: una línea ondulada con un par de vueltas."""
    alto, ancho = pix.shape[:2]
    rng = np.random.default_rng(semilla)
    n = 400
    t = np.linspace(0, 1, n)
    xs = cx + (t - 0.5) * ancho_frac
    ys = cy + 0.025 * np.sin(t * 9 + rng.random() * 3) * (1 - t * 0.5) + 0.01 * np.sin(t * 31)
    for x, y in zip(xs, ys):
        px, py = int(x * ancho), int(y * alto)
        pix[py:py + 2, px:px + 2, :3] = color


def imagen(nombre, pix):
    """Convierte la hoja (fila 0 arriba) en imagen de Blender."""
    return c.imagen(nombre, pix[::-1])


def mat_hoja(nombre, pix, aspereza=0.9):
    return c.material(nombre, (1, 1, 1), aspereza=aspereza, textura=imagen(nombre, pix))


def uv_plano_lado(obj):
    """UV de 0 a 1 para láminas pegadas en un muro que mira hacia +x (muro lateral, muro de la puerta)."""
    malla = obj.data
    if not malla.uv_layers:
        malla.uv_layers.new()
    capa = malla.uv_layers.active.data
    ys = [v.co.y for v in malla.vertices]
    zs = [v.co.z for v in malla.vertices]
    y0, y1, z0, z1 = min(ys), max(ys), min(zs), max(zs)
    for poligono in malla.polygons:
        for i in poligono.loop_indices:
            v = malla.vertices[malla.loops[i].vertex_index].co
            capa[i].uv = ((y1 - v.y) / (y1 - y0 or 1), (v.z - z0) / (z1 - z0 or 1))


def en_mesa(nombre, mat, x, z, ancho, largo, abajo=MESA, grueso=0.002, giro=0.0):
    """Hoja acostada: `ancho` en x de Unity, `largo` en z; su cara de abajo queda en `abajo`."""
    o = c.caja(nombre, (x, abajo + grueso / 2, z), (ancho, grueso, largo), mat)
    c.uv_plano_arriba(o)
    if giro:
        c.girar_y(o, giro)
    return o


def en_muro(nombre, mat, y, z, alto, ancho, x=0.025, grueso=0.002, inclinado=0.0):
    """Hoja pegada en un muro que mira hacia +x: `alto` en y, `ancho` en z."""
    o = c.caja(nombre, (x + grueso / 2, y, z), (grueso, alto, ancho), mat)
    uv_plano_lado(o)
    if inclinado:
        c.girar_x(o, inclinado)
    return o


def girar_punto(pos, eje, grados):
    """Gira una posición de Unity alrededor de (eje_x, eje_z) como Quaternion.Euler(0, grados, 0)."""
    x, y, z = pos
    ex, ez = eje
    a = math.radians(grados)
    dx, dz = x - ex, z - ez
    return (ex + dx * math.cos(a) + dz * math.sin(a), y, ez - dx * math.sin(a) + dz * math.cos(a))


def lapiz(x, z, giro, color=(0.93, 0.74, 0.22), abajo=MESA, largo=0.15, nombre="Lapiz"):
    """Lápiz grafito hexagonal acostado, con punta y goma, girado `giro` grados en la mesa."""
    r = 0.0045
    y = abajo + r
    madera = c.material("lapiz_" + nombre, color, aspereza=0.5)
    punta = c.material("punta_lapiz", (0.85, 0.70, 0.52), aspereza=0.8)
    goma = c.material("goma_lapiz", (0.85, 0.45, 0.48), aspereza=0.8)
    cuerpo = c.cilindro(nombre, (x, y, z), r, largo, madera, eje="x", lados=6)
    c.girar_y(cuerpo, giro)
    p = c.cilindro("Punta", girar_punto((x + largo / 2 + 0.008, y, z), (x, z), giro), 0.003, 0.016, punta, eje="x", lados=6)
    c.girar_y(p, giro)
    g = c.cilindro("Goma", girar_punto((x - largo / 2 - 0.006, y, z), (x, z), giro), 0.0046, 0.012, goma, eje="x", lados=8)
    c.girar_y(g, giro)


def sobre(alto, ancho, color=(0.97, 0.96, 0.93), semilla=1, abierto=False):
    """Textura de un sobre visto por delante: ventana o renglones de la dirección, y la estampilla."""
    pix = hoja(alto, ancho, color, semilla)
    renglones(pix, 0.5, 0.75, 0.42, 0.85, TINTA, semilla, paso=0.08, grueso=0.35)
    rect(pix, 0.08, 0.28, 0.06, 0.4, tuple(v * 0.97 for v in color))
    renglones(pix, 0.1, 0.24, 0.08, 0.36, (0.45, 0.45, 0.5), semilla + 1, paso=0.06, grueso=0.4)
    if abierto:
        rect(pix, 0.0, 0.03, 0.0, 1.0, tuple(v * 0.85 for v in color))
    return pix


def cinta_cerrada(nombre, centro, rx, rz, ancho, mat, abajo=MESA, partes=48, grueso=0.002):
    """Un cordón plano acostado formando un óvalo (el cordón de una credencial), en una sola pieza."""
    import bmesh
    import bpy

    cx, cz = centro
    bm = bmesh.new()
    anillos = []
    for k in range(partes):
        a = 2 * math.pi * k / partes
        fila = []
        for (dr, dy) in ((-ancho / 2, 0), (ancho / 2, 0), (ancho / 2, grueso), (-ancho / 2, grueso)):
            x = cx + (rx + dr) * math.cos(a)
            z = cz + (rz + dr) * math.sin(a)
            fila.append(bm.verts.new(c.unity(x, abajo + dy, z)))
        anillos.append(fila)
    for k in range(partes):
        f0, f1 = anillos[k], anillos[(k + 1) % partes]
        for j in range(4):
            bm.faces.new((f0[j], f0[(j + 1) % 4], f1[(j + 1) % 4], f1[j]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    malla = bpy.data.meshes.new(nombre)
    bm.to_mesh(malla)
    bm.free()
    obj = bpy.data.objects.new(nombre, malla)
    bpy.context.scene.collection.objects.link(obj)
    obj.data.materials.append(mat)
    return obj
