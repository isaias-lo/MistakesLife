"""Piezas comunes de los lugares del mapa del barrio (mapa.<forma>) y de las salidas (salida.<forma>).

Cada lugar del mapa va sobre una manzana de unos 3,3 x 3,1 (MapBuilder.cs): estas ayudas arman
lo que se repite entre lugares (la vereda, los árboles, los techos, las ventanas, las bancas) para
que todos los lugares se vean del mismo barrio. Todo en coordenadas de Unity, como comun.py.
"""

import math

import bmesh
import bpy
import numpy as np

import comun as c

# Colores de la maqueta, sobrios y de verdad (nada de colores de juguete).
VEREDA = (0.74, 0.72, 0.68)
SOLERA = (0.62, 0.61, 0.58)
PASTO = (0.47, 0.58, 0.38)
TRONCO = (0.36, 0.27, 0.19)
HOJAS = ((0.27, 0.45, 0.28), (0.32, 0.50, 0.30), (0.24, 0.41, 0.26))
VIDRIO = (0.46, 0.58, 0.68)
MARCO = (0.93, 0.93, 0.91)
MADERA = (0.46, 0.32, 0.21)
FIERRO = (0.20, 0.21, 0.22)

def mat(nombre, color, aspereza=0.8):
    """Un material por nombre: así varios objetos comparten el mismo (menos peso en el celular)."""
    existente = bpy.data.materials.get(nombre)
    if existente is not None:
        return existente
    return c.material(nombre, color, aspereza=aspereza)


def malla(nombre, vertices, caras, material, suave=False):
    """Malla libre con vértices en coordenadas de Unity (las normales se arreglan solas)."""
    bm = bmesh.new()
    vs = [bm.verts.new(c.unity(*v)) for v in vertices]
    for cara in caras:
        bm.faces.new([vs[i] for i in cara])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    m = bpy.data.meshes.new(nombre)
    bm.to_mesh(m)
    bm.free()
    obj = bpy.data.objects.new(nombre, m)
    bpy.context.scene.collection.objects.link(obj)
    obj.data.materials.append(material)
    if suave:
        obj.data.shade_smooth()
    return obj


def esfera(nombre, centro, radio, material, escala=(1.0, 1.0, 1.0), detalle=2):
    """Esfera de pocos polígonos (icosfera), estirable con `escala` en ejes de Unity."""
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=detalle, radius=radio)
    sx, sy, sz = escala
    for v in bm.verts:
        v.co.x *= sx
        v.co.y *= sz
        v.co.z *= sy
    m = bpy.data.meshes.new(nombre)
    bm.to_mesh(m)
    bm.free()
    obj = bpy.data.objects.new(nombre, m)
    bpy.context.scene.collection.objects.link(obj)
    obj.data.materials.append(material)
    obj.location = c.unity(*centro)
    obj.data.shade_smooth()
    return obj


def _por_color(color, aspereza):
    """Material compartido por color: dos piezas del mismo color usan el mismo material."""
    return mat("color_%02x%02x%02x_%d" % (int(color[0] * 255), int(color[1] * 255), int(color[2] * 255), int(aspereza * 10)), color, aspereza)


def caja(nombre, centro, tamano, color_o_mat, bisel=0.0, aspereza=0.8):
    m = color_o_mat if isinstance(color_o_mat, bpy.types.Material) else _por_color(color_o_mat, aspereza)
    return c.caja(nombre, centro, tamano, m, bisel=bisel)


def cilindro(nombre, centro, radio, largo, color_o_mat, eje="y", lados=10):
    m = color_o_mat if isinstance(color_o_mat, bpy.types.Material) else _por_color(color_o_mat, 0.6)
    return c.cilindro(nombre, centro, radio, largo, m, eje=eje, lados=lados)


# ---------------------------------------------------------------- piezas del barrio

def vereda(ancho=3.3, fondo=3.1):
    """La vereda de la manzana con su solera (el borde de cemento que da a la calle)."""
    caja("Vereda", (0, 0.02, 0), (ancho, 0.04, fondo), VEREDA)
    for z in (-fondo / 2 + 0.03, fondo / 2 - 0.03):
        caja("Solera", (0, 0.045, z), (ancho, 0.05, 0.06), SOLERA)
    for x in (-ancho / 2 + 0.03, ancho / 2 - 0.03):
        caja("Solera", (x, 0.045, 0), (0.06, 0.05, fondo), SOLERA)


def pasto(centro, tamano, color=PASTO):
    caja("Pasto", centro, tamano, _por_color(color, 0.95))


def arbol(pos, alto=1.0, semilla=1):
    """Un árbol de calle: tronco que se afina y una copa de tres masas, siempre igual con la misma semilla."""
    rng = np.random.default_rng(semilla)
    x, y, z = pos
    tronco = c.caja("Tronco", (x, y + alto * 0.28, z), (0.08 * alto, alto * 0.56, 0.08 * alto), mat("Tronco", TRONCO), ahusado=1.4)
    tronco.modifiers.clear()
    hojas = mat("Hojas %d" % (semilla % 3), HOJAS[semilla % 3], 0.9)
    for i in range(3):
        ang = rng.random() * math.tau
        r = 0.14 * alto
        esfera("Copa", (x + math.cos(ang) * r, y + alto * (0.72 + rng.random() * 0.18), z + math.sin(ang) * r),
               alto * (0.28 + rng.random() * 0.08), hojas, escala=(1.0, 0.85, 1.0), detalle=2)


def techo_dos_aguas(nombre, centro, largo, ancho, alto, color, alero=0.08, eje="x"):
    """Techo de dos aguas: la cumbrera corre a lo largo de `eje` ("x" o "z"). `centro` es la base del techo."""
    x, y, z = centro
    if eje == "x":
        lx, lz = largo / 2 + alero, ancho / 2 + alero
        vs = [(x - lx, y, z - lz), (x + lx, y, z - lz), (x + lx, y, z + lz), (x - lx, y, z + lz),
              (x - lx, y + alto, z), (x + lx, y + alto, z)]
        caras = [(0, 1, 5, 4), (3, 2, 5, 4), (0, 4, 3), (1, 5, 2), (0, 1, 2, 3)]
    else:
        lx, lz = ancho / 2 + alero, largo / 2 + alero
        vs = [(x - lx, y, z - lz), (x + lx, y, z - lz), (x + lx, y, z + lz), (x - lx, y, z + lz),
              (x, y + alto, z - lz), (x, y + alto, z + lz)]
        caras = [(0, 4, 5, 3), (1, 4, 5, 2), (0, 1, 4), (3, 2, 5), (0, 1, 2, 3)]
    return malla(nombre, vs, caras, _por_color(color, 0.85))


def ventana(centro, ancho, alto, frente="-z", vidrio=VIDRIO, marco=MARCO):
    """Ventana con marco, pegada a una fachada que mira hacia `frente` ("-z", "+z", "-x", "+x")."""
    x, y, z = centro
    g = 0.03
    if frente in ("-z", "+z"):
        s = -1 if frente == "-z" else 1
        caja("Marco", (x, y, z + s * 0.01), (ancho + 0.06, alto + 0.06, g), marco, aspereza=0.5)
        caja("Vidrio", (x, y, z + s * 0.025), (ancho, alto, g), _por_color(vidrio, 0.15))
    else:
        s = -1 if frente == "-x" else 1
        caja("Marco", (x + s * 0.01, y, z), (g, alto + 0.06, ancho + 0.06), marco, aspereza=0.5)
        caja("Vidrio", (x + s * 0.025, y, z), (g, alto, ancho), _por_color(vidrio, 0.15))


def banca(pos, largo=0.5, giro=0):
    """Banca de plaza: tablas de madera sobre patas de fierro."""
    x, y, z = pos
    partes = [
        caja("Asiento", (x, y + 0.13, z), (largo, 0.025, 0.14), MADERA),
        caja("Respaldo", (x, y + 0.22, z + 0.07), (largo, 0.08, 0.02), MADERA),
    ]
    for dx in (-largo / 2 + 0.05, largo / 2 - 0.05):
        partes.append(caja("Pata de banca", (x + dx, y + 0.065, z), (0.025, 0.13, 0.14), FIERRO))
    _girar_grupo(partes, (x, z), giro)
    return partes


def farol(pos, alto=0.9):
    x, y, z = pos
    cilindro("Poste del farol", (x, y + alto / 2, z), 0.018, alto, FIERRO, lados=6)
    caja("Farol", (x, y + alto + 0.03, z), (0.07, 0.06, 0.07), mat("Luz del farol", (0.98, 0.92, 0.72), 0.4))


def reja(desde, hasta, alto=0.25, paso=0.06, color=FIERRO):
    """Reja de barrotes entre dos puntos del piso (las rejas de las casas de Santiago)."""
    x0, y0, z0 = desde
    x1, _, z1 = hasta
    largo = math.hypot(x1 - x0, z1 - z0)
    n = max(2, int(largo / paso))
    partes = []
    for i in range(n + 1):
        t = i / n
        partes.append(caja("Barrote", (x0 + (x1 - x0) * t, y0 + alto / 2, z0 + (z1 - z0) * t), (0.012, alto, 0.012), color))
    medio = ((x0 + x1) / 2, (z0 + z1) / 2)
    angulo = -math.degrees(math.atan2(z1 - z0, x1 - x0))
    for h in (0.03, alto - 0.01):
        barra = caja("Barra de la reja", (medio[0], y0 + h, medio[1]), (largo, 0.015, 0.015), color)
        c.girar_y(barra, angulo)
        partes.append(barra)
    return partes


def _girar_grupo(partes, eje, grados):
    if not grados:
        return
    ex, ez = eje
    a = math.radians(grados)
    for p in partes:
        ux, uy, uz = -p.location[0], p.location[2], -p.location[1]
        dx, dz = ux - ex, uz - ez
        nx = ex + dx * math.cos(a) + dz * math.sin(a)
        nz = ez - dx * math.sin(a) + dz * math.cos(a)
        p.location = c.unity(nx, uy, nz)
        c.girar_y(p, grados)


girar_grupo = _girar_grupo


def auto(pos, color, giro=0):
    """Un auto estacionado (sedán chico, de los que se ven en cualquier calle)."""
    x, y, z = pos
    pintura = mat("Pintura %s" % (color,), color, 0.35)
    partes = [
        c.caja("Carrocería", (x, y + 0.09, z), (0.42, 0.1, 0.19), pintura, bisel=0.02),
        c.caja("Cabina", (x - 0.02, y + 0.175, z), (0.22, 0.08, 0.17), pintura, bisel=0.02),
        caja("Vidrios del auto", (x - 0.02, y + 0.175, z), (0.2, 0.065, 0.175), (0.20, 0.25, 0.30), aspereza=0.2),
    ]
    for dx in (-0.13, 0.13):
        for dz in (-0.09, 0.09):
            partes.append(cilindro("Rueda", (x + dx, y + 0.045, z + dz), 0.045, 0.03, (0.08, 0.08, 0.08), eje="z", lados=10))
    _girar_grupo(partes, (x, z), giro)
    return partes


def maceta(pos, color_hojas=HOJAS[1]):
    x, y, z = pos
    cilindro("Maceta", (x, y + 0.05, z), 0.05, 0.1, (0.62, 0.36, 0.26))
    esfera("Planta", (x, y + 0.14, z), 0.07, mat("Planta", color_hojas, 0.9), detalle=1)
