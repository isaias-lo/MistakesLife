"""La calle del barrio (NeighborhoodBuilder.cs): casas por nivel del hogar, colegio, almacén, plaza, paradero,
árboles, autos y escombros del terremoto.

Medidas en coordenadas de Unity desde el ancla de cada cosa, igual que la forma simple de reserva.
Las casas del norte miran hacia -z (la calle); el colegio y el almacén, al sur, miran hacia +z.

La pintura de las casas y de los autos va en materiales que se llaman "pintura_...": Unity les
pone el color de cada casa o de cada auto (Models.Paint), así un solo modelo sirve para todo el barrio.
Sin amarillo: ese color es solo de la marca del jugador.
"""

import math

import bmesh
import bpy
import numpy as np

import comun as c
import oficios as of
import papeles as p


def _mat(nombre, color, aspereza=0.6):
    return c.material(nombre, color, aspereza=aspereza)


def _con_textura(nombre, hacer_imagen, aspereza=0.8):
    """Material con textura, uno solo por nombre: los árboles de la plaza comparten el follaje."""
    existente = bpy.data.materials.get(nombre)
    if existente is not None:
        return existente
    return c.material(nombre, (1, 1, 1), aspereza=aspereza, textura=hacer_imagen())


def _textura(nombre, pix):
    return c.imagen(nombre, pix[::-1])


def _cono(nombre, centro, radio_abajo, radio_arriba, largo, mat, lados=12):
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=lados, radius1=radio_abajo, radius2=radio_arriba, depth=largo)
    malla = bpy.data.meshes.new(nombre)
    bm.to_mesh(malla)
    bm.free()
    obj = bpy.data.objects.new(nombre, malla)
    bpy.context.scene.collection.objects.link(obj)
    obj.data.materials.append(mat)
    obj.location = c.unity(*centro)
    obj.data.shade_smooth()
    return obj


# ------------------------------------------------------------------ texturas

def textura_zinc(nombre="zinc"):
    """Plancha de zinc ondulada: franjas claras y oscuras que corren de adelante hacia atrás."""
    alto, ancho = 64, 256
    x = np.arange(ancho)
    onda = 0.5 + 0.5 * np.sin(x / ancho * 2 * math.pi * 40)
    pix = np.ones((alto, ancho, 4))
    base = np.array((0.60, 0.62, 0.63))
    pix[..., :3] = base * (0.82 + 0.22 * onda)[None, :, None]
    pix[..., :3] *= 1 - 0.08 * c._ruido(alto, ancho, 16, 3)[..., None]
    return _textura(nombre, pix)


def textura_tejas(nombre="tejas"):
    """Teja de arcilla en hileras, con el borde de cada teja más oscuro y tonos que varían."""
    alto, ancho = 256, 256
    rng = np.random.default_rng(9)
    pix = np.ones((alto, ancho, 4))
    fila, largo = 24, 32
    for f in range(alto // fila + 1):
        corrido = (largo // 2) * (f % 2)
        for k in range(-1, ancho // largo + 1):
            tono = 0.86 + rng.random() * 0.2
            x0, x1 = max(k * largo + corrido, 0), max(min(k * largo + corrido + largo, ancho), 0)
            y0, y1 = f * fila, min((f + 1) * fila, alto)
            if x1 <= x0 or y1 <= y0:
                continue
            curva = np.sin(np.linspace(0, math.pi, x1 - x0))[None, :, None]
            sombra = np.linspace(0.75, 1.0, y1 - y0)[:, None, None]
            pix[y0:y1, x0:x1, :3] = np.array((0.66, 0.30, 0.20)) * tono * (0.8 + 0.2 * curva) * sombra
    return _textura(nombre, pix)


def textura_pasto(nombre="pasto", color=(0.36, 0.52, 0.30), tamano=256):
    pix = np.ones((tamano, tamano, 4))
    manchas = c._ruido(tamano, tamano, 40, 21) * 0.18 + c._ruido(tamano, tamano, 2, 22) * 0.14
    pix[..., :3] = np.array(color) * (0.88 + manchas[..., None])
    return _textura(nombre, pix)


def textura_cemento(nombre="cemento", color=(0.70, 0.69, 0.66), juntas=None):
    tamano = 256
    pix = np.ones((tamano, tamano, 4))
    manchas = c._ruido(tamano, tamano, 48, 31) * 0.10 + c._ruido(tamano, tamano, 2, 32) * 0.06
    pix[..., :3] = np.array(color) * (0.92 + manchas[..., None])
    if juntas:
        pix[::juntas, :, :3] *= 0.8
        pix[:, ::juntas, :3] *= 0.8
    return _textura(nombre, pix)


def textura_maicillo(nombre="maicillo"):
    """El maicillo de las plazas de Santiago: tierra clara con piedrecitas."""
    tamano = 256
    rng = np.random.default_rng(41)
    pix = np.ones((tamano, tamano, 4))
    pix[..., :3] = np.array((0.80, 0.70, 0.55)) * (0.9 + c._ruido(tamano, tamano, 24, 42)[..., None] * 0.12)
    piedras = rng.random((tamano, tamano)) < 0.04
    pix[piedras, :3] *= 0.75
    return _textura(nombre, pix)


def textura_follaje(nombre="follaje", color=(0.27, 0.45, 0.25)):
    tamano = 256
    pix = np.ones((tamano, tamano, 4))
    manchas = c._ruido(tamano, tamano, 6, 51) * 0.35 + c._ruido(tamano, tamano, 30, 52) * 0.2
    pix[..., :3] = np.array(color) * (0.7 + manchas[..., None])
    return _textura(nombre, pix)


# ------------------------------------------------------------------ piezas de casa

def _pintura():
    """La pintura del muro: Unity le pone el color de cada casa (Models.Paint)."""
    return _mat("pintura_casa", (0.93, 0.80, 0.60), 0.85)


def _ventana(x, y, z, ancho, alto, frente=-1, rejas=True):
    """Ventana de aluminio con vidrio y, si se pide, protecciones de fierro como las de Chile."""
    marco = _mat("aluminio", (0.78, 0.78, 0.76), 0.4)
    vidrio = _mat("vidrio_ventana", (0.32, 0.42, 0.48), 0.1)
    fierro = _mat("fierro_negro", (0.13, 0.13, 0.14), 0.5)
    c.caja("Marco ventana", (x, y, z + frente * 0.01), (ancho + 0.08, alto + 0.08, 0.04), marco)
    c.caja("Vidrio", (x, y, z + frente * 0.035), (ancho, alto, 0.01), vidrio)
    c.caja("Travesano", (x, y, z + frente * 0.04), (0.03, alto, 0.012), marco)
    c.caja("Alfeizar", (x, y - alto / 2 - 0.05, z + frente * 0.05), (ancho + 0.16, 0.05, 0.1), _mat("alfeizar", (0.82, 0.80, 0.76)))
    if rejas:
        n = max(3, int(ancho / 0.12))
        for i in range(n + 1):
            bx = x - ancho / 2 + i * ancho / n
            c.cilindro("Proteccion", (bx, y, z + frente * 0.09), 0.008, alto + 0.06, fierro, lados=6)
        for dy in (-alto / 2, alto / 2):
            c.caja("Proteccion", (x, y + dy, z + frente * 0.09), (ancho + 0.04, 0.025, 0.012), fierro)


def _puerta(x, z, ancho=0.85, alto=2.0, frente=-1, color=(0.36, 0.24, 0.16)):
    c.caja("Marco puerta", (x, alto / 2 + 0.02, z + frente * 0.01), (ancho + 0.1, alto + 0.06, 0.04), _mat("marco_puerta", (0.80, 0.79, 0.76)))
    c.caja("Puerta", (x, alto / 2, z + frente * 0.03), (ancho, alto, 0.04), _mat("puerta_casa", color, 0.55))
    for k in range(2):
        c.caja("Panel puerta", (x, 0.55 + k * 0.85, z + frente * 0.052), (ancho * 0.7, 0.6, 0.006), _mat("panel_puerta", tuple(v * 0.85 for v in color), 0.55))
    c.cilindro("Manilla", (x + ancho * 0.36, 1.0, z + frente * 0.07), 0.02, 0.04, _mat("manilla", (0.70, 0.66, 0.55), 0.3), eje="z")
    c.caja("Peldano", (x, 0.04, z + frente * 0.2), (ancho + 0.3, 0.08, 0.36), _mat("cemento_peldano", (0.66, 0.65, 0.62), 0.9))


def reja(z, ancho, alto=1.1, porton=0.0, color=(0.20, 0.20, 0.22)):
    """Reja de fierro con pilares de cemento a los lados y portón; `porton` es su x."""
    fierro = _mat("fierro_reja", color, 0.5)
    pilar = _mat("pilar_reja", (0.72, 0.71, 0.68), 0.9)
    for lado in (-1, 1):
        c.caja("Pilar", (lado * ancho / 2, alto / 2 + 0.05, z), (0.22, alto + 0.1, 0.22), pilar, bisel=0.01)
    for y in (0.12, alto - 0.05):
        c.caja("Barra reja", (0, y, z), (ancho - 0.2, 0.04, 0.04), fierro)
    n = int((ancho - 0.2) / 0.12)
    for i in range(n + 1):
        x = -(ancho - 0.2) / 2 + i * (ancho - 0.2) / n
        c.caja("Barrote", (x, alto / 2 + 0.03, z), (0.018, alto - 0.04, 0.018), fierro)
        c.caja("Punta", (x, alto + 0.02, z), (0.03, 0.05, 0.03), fierro, ahusado=1.8)
    # Portón: un marco más grueso y la chapa.
    for dx in (-0.5, 0.5):
        c.caja("Marco porton", (porton + dx, alto / 2 + 0.03, z), (0.05, alto - 0.04, 0.05), fierro)
    c.caja("Chapa porton", (porton + 0.42, 0.95, z - 0.04), (0.08, 0.1, 0.04), fierro)


def _techo_zinc(ancho, fondo, alto):
    """Techo casi plano de zinc, con su caída hacia atrás y la tapa del borde."""
    zinc = c.material("zinc", (1, 1, 1), aspereza=0.45, textura=textura_zinc())
    t = c.caja("Techo de zinc", (0, alto + 0.1, 0), (ancho + 0.4, 0.04, fondo + 0.5), zinc)
    c.uv_plano_arriba(t)
    c.girar_x(t, 3.5)
    tapa = _mat("tapa_techo", (0.86, 0.85, 0.82), 0.8)
    c.caja("Tapa del techo", (0, alto + 0.05, -(fondo + 0.5) / 2), (ancho + 0.44, 0.16, 0.03), tapa)


def _frontones(ancho, fondo, alto):
    pendiente = math.radians(32)
    cumbre = alto + (fondo / 2) * math.tan(pendiente)
    pintura = _pintura()
    bm = bmesh.new()
    for lado in (-1, 1):
        x0, x1 = lado * ancho / 2 - lado * 0.01, lado * ancho / 2 - lado * 0.21
        tri = [(-fondo / 2, alto), (fondo / 2, alto), (0, cumbre)]
        a = [bm.verts.new(c.unity(x0, y, z)) for z, y in tri]
        b = [bm.verts.new(c.unity(x1, y, z)) for z, y in tri]
        bm.faces.new(a)
        bm.faces.new(list(reversed(b)))
        for i in range(3):
            bm.faces.new((a[i], a[(i + 1) % 3], b[(i + 1) % 3], b[i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    malla = bpy.data.meshes.new("Frontones")
    bm.to_mesh(malla)
    bm.free()
    obj = bpy.data.objects.new("Frontones", malla)
    bpy.context.scene.collection.objects.link(obj)
    obj.data.materials.append(pintura)
    return cumbre


def _techo_dos_aguas(ancho, fondo, alto):
    pendiente = math.radians(32)
    vuelo = 0.3
    medio = fondo / 2 + vuelo
    largo = medio / math.cos(pendiente)
    tejas = c.material("tejas", (1, 1, 1), aspereza=0.75, textura=textura_tejas())
    cumbre = _frontones(ancho, fondo, alto)
    for lado in (-1, 1):
        # Cada agua va de la cumbrera hasta el vuelo del alero, apoyada sobre el muro.
        zc = lado * medio / 2
        yc = cumbre - (medio / 2) * math.tan(pendiente) + 0.05
        t = c.caja("Agua del techo", (0, yc, zc), (ancho + 0.35, 0.06, largo), tejas)
        c.uv_plano_arriba(t)
        c.girar_x(t, lado * 32)
    c.caja("Cumbrera", (0, cumbre + 0.08, 0), (ancho + 0.38, 0.08, 0.16), _mat("cumbrera", (0.55, 0.25, 0.17), 0.75), bisel=0.02)


def _cuerpo(ancho, fondo, alto):
    """El muro pintado, con zócalo gris abajo y la losa del radier."""
    c.caja("Casa", (0, alto / 2, 0), (ancho, alto, fondo), _pintura(), bisel=0.02)
    zocalo = _mat("zocalo_casa", (0.52, 0.51, 0.49), 0.9)
    c.caja("Zocalo", (0, 0.15, 0), (ancho + 0.02, 0.3, fondo + 0.02), zocalo)


def _patio(ancho, desde, hasta, pasto=False):
    """El espacio entre la reja y la casa: radier de cemento o antejardín con pasto y un camino."""
    largo = hasta - desde
    zc = (desde + hasta) / 2
    if pasto:
        verde = c.material("pasto_antejardin", (1, 1, 1), aspereza=0.95, textura=textura_pasto("pasto_antejardin"))
        o = c.caja("Antejardin", (0, 0.02, zc), (ancho, 0.04, largo), verde)
        c.uv_plano_arriba(o)
        c.caja("Camino", (-0.8, 0.045, zc), (0.9, 0.02, largo), _mat("baldosa_camino", (0.72, 0.70, 0.66), 0.85))
        hoja = _mat("arbusto", (0.24, 0.42, 0.24), 0.8)
        for i, x in enumerate((0.6, 1.2, 1.8)):
            c.esfera("Arbusto", (x, 0.28, hasta - 0.3), 0.3, hoja, escala=(1.1, 0.9, 1.0), lados=12)
    else:
        cem = c.material("radier", (1, 1, 1), aspereza=0.95, textura=textura_cemento("radier", juntas=64))
        o = c.caja("Radier", (0, 0.015, zc), (ancho, 0.03, largo), cem)
        c.uv_plano_arriba(o)


def casa_humilde():
    """Casa de un piso de 3,6 m de frente, techo de zinc, protecciones en la ventana y reja."""
    ancho, fondo, alto = 3.6, 4.0, 2.5
    _cuerpo(ancho, fondo, alto)
    _puerta(-0.8, -fondo / 2, color=(0.40, 0.42, 0.45))
    _ventana(0.8, 1.4, -fondo / 2, 1.1, 0.8)
    _techo_zinc(ancho, fondo, alto)
    _patio(ancho + 1.0, -fondo / 2 - 1.55, -fondo / 2)
    reja(-fondo / 2 - 1.6, ancho + 1.2, porton=-0.8)
    # El medidor de luz junto a la puerta, y la bajada del agua lluvia.
    c.caja("Medidor de luz", (-1.45, 1.6, -fondo / 2 - 0.08), (0.3, 0.4, 0.14), _mat("medidor", (0.75, 0.76, 0.74), 0.5), bisel=0.01)
    c.cilindro("Bajada de agua", (ancho / 2 - 0.08, alto / 2, -fondo / 2 - 0.06), 0.035, alto, _mat("pvc_gris", (0.55, 0.56, 0.57), 0.5), lados=8)


def casa_media():
    """Casa de un piso de 4,4 m de frente, techo de tejas a dos aguas, dos ventanas y reja."""
    ancho, fondo, alto = 4.4, 4.0, 2.5
    _cuerpo(ancho, fondo, alto)
    _techo_dos_aguas(ancho, fondo, alto)
    _puerta(-0.8, -fondo / 2)
    _ventana(0.9, 1.4, -fondo / 2, 1.3, 0.9, rejas=True)
    _ventana(-1.75, 1.45, -fondo / 2, 0.6, 0.6, rejas=False)
    _patio(ancho + 1.0, -fondo / 2 - 1.55, -fondo / 2)
    reja(-fondo / 2 - 1.6, ancho + 1.2, porton=-0.8)
    # Un macetero en la entrada.
    c.cilindro("Macetero", (0.2, 0.18, -fondo / 2 - 0.4), 0.16, 0.36, _mat("greda", (0.62, 0.36, 0.24), 0.8), lados=16)
    c.esfera("Planta", (0.2, 0.52, -fondo / 2 - 0.4), 0.22, _mat("planta_entrada", (0.26, 0.44, 0.24), 0.8), lados=12)


def casa_acomodada():
    """Casa de dos pisos de 4,4 m de frente, techo de tejas, antejardín con pasto y reja."""
    ancho, fondo, alto = 4.4, 4.0, 4.6
    _cuerpo(ancho, fondo, alto)
    _techo_dos_aguas(ancho, fondo, alto)
    c.caja("Cornisa entre pisos", (0, 2.45, -fondo / 2 - 0.03), (ancho + 0.04, 0.12, 0.08), _mat("cornisa", (0.90, 0.89, 0.86), 0.7))
    _puerta(-0.8, -fondo / 2, ancho=0.95, alto=2.1, color=(0.30, 0.20, 0.13))
    _ventana(0.95, 1.3, -fondo / 2, 1.6, 1.2, rejas=False)
    for x in (-0.9, 0.95):
        _ventana(x, 3.55, -fondo / 2, 1.1, 1.0, rejas=False)
    # Marquesina sobre la puerta y el farol de la entrada.
    c.caja("Marquesina", (-0.8, 2.3, -fondo / 2 - 0.35), (1.4, 0.06, 0.7), _mat("marquesina", (0.85, 0.84, 0.81), 0.6), bisel=0.01)
    c.caja("Farol", (-1.4, 1.9, -fondo / 2 - 0.06), (0.12, 0.2, 0.1), _mat("farol", (0.16, 0.16, 0.17), 0.4), bisel=0.01)
    _patio(ancho + 1.0, -fondo / 2 - 1.55, -fondo / 2, pasto=True)
    reja(-fondo / 2 - 1.6, ancho + 1.2, porton=-0.8, color=(0.16, 0.16, 0.17))


# ------------------------------------------------------------------ árbol

def arbol(x=0.0, z=0.0, semilla=1, alto=1.0, mostrar_raiz=True):
    """Árbol de vereda (como los plátanos orientales de Santiago): tronco que se afina, ramas y copa."""
    rng = np.random.default_rng(semilla)
    corteza = _mat("corteza", (0.38, 0.30, 0.23), 0.9)
    follaje = [
        _con_textura("follaje", lambda: textura_follaje("follaje")),
        _con_textura("follaje_claro", lambda: textura_follaje("follaje_claro", (0.33, 0.52, 0.28))),
    ]
    _cono("Tronco", (x, 0.85 * alto, z), 0.14, 0.09, 1.7 * alto, corteza, lados=10)
    for k in range(3):
        ang = k * 120 + rng.random() * 40
        rama = c.cilindro("Rama", (x + 0.22 * math.cos(math.radians(ang)), 1.85 * alto, z + 0.22 * math.sin(math.radians(ang))), 0.045, 0.7 * alto, corteza, lados=6)
        rama.rotation_euler = (math.radians(28) * math.sin(math.radians(ang)), math.radians(28) * math.cos(math.radians(ang)) * -1, 0)
    centros = [(0, 2.45, 0, 1.0)] + [
        (0.65 * math.cos(a), 2.2 + rng.random() * 0.5, 0.65 * math.sin(a), 0.7 + rng.random() * 0.15)
        for a in np.linspace(0, 2 * math.pi, 6, endpoint=False) + rng.random()
    ] + [(0.0, 2.95, 0.1, 0.7)]
    for i, (dx, dy, dz, r) in enumerate(centros):
        o = c.esfera("Copa", (x + dx, dy * alto, z + dz), r * alto, follaje[i % 2], lados=16)
        # Una copa irregular: los puntos de cada esfera se corren un poco, siempre igual.
        for v in o.data.vertices:
            n = v.co.normalized()
            v.co += n * (0.12 * math.sin(v.co.x * 7 + semilla) * math.cos(v.co.y * 6) + 0.06 * math.sin(v.co.z * 11)) * alto
        c.uv_cubo(o, 0.6)
    if mostrar_raiz:
        tierra = _mat("tierra_arbol", (0.36, 0.28, 0.20), 1.0)
        c.caja("Taza del arbol", (x, 0.085, z), (0.8, 0.01, 0.8), tierra)


# ------------------------------------------------------------------ auto

def _patente():
    """Patente chilena blanca: dos pares de letras (bloques) y dos números."""
    pat = p.hoja(32, 96, (0.95, 0.95, 0.93), grano=0)
    p.borde(pat, 0, 1, 0, 1, (0.2, 0.2, 0.22), grueso=2)
    p.rect(pat, 0.3, 0.75, 0.1, 0.3, (0.15, 0.15, 0.18))
    p.rect(pat, 0.3, 0.75, 0.35, 0.55, (0.15, 0.15, 0.18))
    p.numero(pat, "12", 0.25, 0.62, 0.55, (0.15, 0.15, 0.18))
    return p.imagen("patente", pat)


def auto():
    """Auto chico de cinco puertas (3,6 m de largo): carrocería pintada, vidrios, ruedas y patente."""
    pintura = _mat("pintura_auto", (0.75, 0.20, 0.20), 0.3)
    vidrio = _mat("vidrio_auto", (0.12, 0.15, 0.18), 0.08)
    goma = _mat("neumatico", (0.08, 0.08, 0.09), 0.85)
    llanta = _mat("llanta", (0.62, 0.63, 0.65), 0.3)
    negro = _mat("plastico_auto", (0.12, 0.12, 0.13), 0.6)
    c.caja("Carroceria", (0, 0.58, 0), (3.6, 0.58, 1.62), pintura, bisel=0.12, segmentos=3)
    cabina = c.caja("Vidrios", (-0.25, 1.1, 0), (2.1, 0.52, 1.5), vidrio, bisel=0.06, ahusado=0.8)
    c.girar_z(cabina, 180)
    c.caja("Techo del auto", (-0.25, 1.37, 0), (1.72, 0.04, 1.22), pintura, bisel=0.02)
    for x in (-0.25, 0.72, -1.15):
        c.caja("Pilar", (x, 1.1, 0), (0.07 if x != 0.72 else 0.06, 0.5, 1.42), pintura)
    for x in (-1.25, 1.2):
        for z in (-0.72, 0.72):
            c.cilindro("Rueda", (x, 0.31, z), 0.31, 0.2, goma, eje="z", lados=20)
            c.cilindro("Llanta", (x, 0.31, z + (0.1 if z > 0 else -0.1)), 0.18, 0.02, llanta, eje="z", lados=16)
    for z in (-0.82, 0.82):
        c.caja("Moldura", (0, 0.4, z), (3.3, 0.12, 0.02), negro)
        c.caja("Espejo", (0.62, 0.98, z * 1.04), (0.12, 0.08, 0.06), pintura, bisel=0.02)
        for x in (0.25, -0.6):
            c.caja("Manilla", (x, 0.78, z * 1.01), (0.14, 0.03, 0.02), negro)
    for z in (-0.55, 0.55):
        c.caja("Foco", (1.79, 0.68, z), (0.03, 0.12, 0.28), _mat("foco", (0.92, 0.92, 0.88), 0.15), bisel=0.01)
        c.caja("Luz trasera", (-1.79, 0.72, z), (0.03, 0.16, 0.24), _mat("luz_trasera", (0.62, 0.08, 0.08), 0.2), bisel=0.01)
    for x in (1.81, -1.81):
        c.caja("Parachoque", (x, 0.38, 0), (0.06, 0.16, 1.6), negro, bisel=0.02)
        o = c.caja("Patente", (x * 1.01, 0.5, 0), (0.01, 0.12, 0.36), _con_textura("patente", _patente, 0.5))
        p.uv_plano_lado(o)


# ------------------------------------------------------------------ colegio

def colegio():
    """Escuela municipal de un piso: muro crema con franja azul, ventanas en fila, letrero y patio."""
    crema = _mat("muro_colegio", (0.92, 0.86, 0.70), 0.85)
    azul = _mat("franja_colegio", (0.25, 0.40, 0.65), 0.6)
    c.caja("Colegio", (0, 1.6, 0), (9.0, 3.2, 5.0), crema, bisel=0.02)
    c.caja("Zocalo", (0, 0.2, 0), (9.02, 0.4, 5.02), _mat("zocalo_colegio", (0.45, 0.47, 0.52), 0.85))
    c.caja("Franja del colegio", (0, 2.6, 2.52), (9.0, 0.5, 0.06), azul)
    c.caja("Losa", (0, 3.25, 0), (9.2, 0.1, 5.2), _mat("losa", (0.66, 0.66, 0.64), 0.9))
    for x in (-3.6, -2.4, -1.2, 1.2, 2.4, 3.6):
        _ventana(x, 1.35, 2.5, 0.9, 1.1, frente=1, rejas=True)
    # Atrás (lo que se ve desde la casa): las ventanas de las salas, la franja y la puerta de servicio.
    for x in (-3.6, -2.4, -1.2, 0.0, 1.2, 2.4):
        _ventana(x, 1.35, -2.5, 0.9, 1.1, frente=-1, rejas=True)
    c.caja("Franja trasera", (0, 2.6, -2.52), (9.0, 0.5, 0.06), azul)
    _puerta(3.7, -2.5, color=(0.25, 0.40, 0.65))
    # Puerta doble al medio con su marquesina y el letrero (renglones, sin letras reales).
    puerta = _mat("puerta_colegio", (0.25, 0.40, 0.65), 0.55)
    for dx in (-0.4, 0.4):
        c.caja("Puerta", (dx, 1.05, 2.53), (0.76, 2.1, 0.05), puerta)
        c.caja("Vidrio puerta", (dx, 1.45, 2.56), (0.5, 0.7, 0.01), _mat("vidrio_ventana", (0.32, 0.42, 0.48), 0.1))
    c.caja("Marquesina", (0, 2.25, 3.0), (2.2, 0.08, 1.0), _mat("marquesina_colegio", (0.80, 0.80, 0.78), 0.6))
    for dx in (-1.0, 1.0):
        c.cilindro("Pilar", (dx, 1.1, 3.4), 0.05, 2.2, _mat("fierro_blanco", (0.86, 0.86, 0.84), 0.5), lados=8)
    cartel = p.hoja(64, 256, (0.95, 0.95, 0.93), grano=0)
    p.rect(cartel, 0.0, 1.0, 0.0, 0.18, (0.25, 0.40, 0.65))
    p.renglones(cartel, 0.25, 0.7, 0.24, 0.94, (0.20, 0.30, 0.50), 4, paso=0.22, grueso=0.6)
    o = c.caja("Letrero del colegio", (0, 2.95, 2.54), (3.0, 0.45, 0.03), c.material("letrero_colegio", (1, 1, 1), 0.6, textura=p.imagen("letrero_colegio", cartel)))
    c.uv_plano_frente(o)
    # Patio de cemento con las líneas de la multicancha.
    lineas = p.hoja(128, 512, (0.72, 0.70, 0.66), semilla=3, grano=0.04)
    blanco = (0.94, 0.94, 0.92)
    p.borde(lineas, 0.06, 0.94, 0.03, 0.97, blanco, grueso=3)
    p.rect(lineas, 0.06, 0.94, 0.497, 0.503, blanco)
    p.circulo_a_mano(lineas, 0.5, 0.5, 0.3, 0.075, blanco, grueso=0.08)
    o = c.caja("Patio", (0, 0.02, 3.6), (9.0, 0.04, 2.2), c.material("patio_colegio", (1, 1, 1), 0.9, textura=p.imagen("patio_colegio", lineas)))
    c.uv_plano_arriba(o)
    # Mástil con la bandera de Chile, en la esquina del patio.
    c.cilindro("Mastil", (-4.2, 1.6, 2.8), 0.03, 3.2, _mat("fierro_blanco", (0.86, 0.86, 0.84), 0.5), lados=8)
    bandera = np.ones((60, 90, 4))
    bandera[:30, :, :3] = (0.96, 0.96, 0.95)
    bandera[30:, :, :3] = (0.80, 0.12, 0.15)
    bandera[:30, :30, :3] = (0.10, 0.22, 0.55)
    yy, xx = np.indices((30, 30))
    bandera[:30, :30][np.hypot(yy - 15, xx - 15) < 5, :3] = (0.96, 0.96, 0.95)
    o = c.caja("Bandera", (-3.75, 2.85, 2.8), (0.9, 0.6, 0.01), c.material("bandera", (1, 1, 1), 0.7, textura=_textura("bandera", bandera)))
    c.uv_plano_frente(o)


# ------------------------------------------------------------------ almacén

def almacen():
    """Almacén de barrio: fachada blanca, letrero rojo, cortina metálica a medio subir y bebidas afuera."""
    blanco = _mat("muro_almacen", (0.96, 0.95, 0.90), 0.85)
    c.caja("Almacen", (0, 1.3, 0), (4.0, 2.6, 4.0), blanco, bisel=0.02)
    c.caja("Zocalo", (0, 0.18, 0), (4.02, 0.36, 4.02), _mat("zocalo_almacen", (0.70, 0.20, 0.18), 0.8))
    cartel = p.hoja(64, 384, (0.78, 0.20, 0.20), grano=0)
    p.renglones(cartel, 0.3, 0.7, 0.1, 0.9, (0.97, 0.96, 0.93), 8, paso=0.4, grueso=0.7)
    o = c.caja("Letrero del almacen", (0, 2.2, 2.02), (3.6, 0.5, 0.06), c.material("letrero_almacen", (1, 1, 1), 0.5, textura=p.imagen("letrero_almacen", cartel)))
    c.uv_plano_frente(o)
    # La puerta abierta: interior oscuro con estantes y productos de colores.
    c.caja("Interior", (-0.6, 1.0, 2.0), (1.6, 1.9, 0.02), _mat("interior_almacen", (0.18, 0.16, 0.14), 0.9))
    colores = [(0.75, 0.25, 0.22), (0.22, 0.45, 0.30), (0.85, 0.85, 0.82), (0.25, 0.35, 0.60), (0.80, 0.50, 0.25)]
    for fila, y in enumerate((0.5, 0.95, 1.4)):
        c.caja("Repisa", (-0.6, y - 0.12, 2.02), (1.5, 0.03, 0.04), _mat("repisa", (0.55, 0.50, 0.42), 0.7))
        for k in range(9):
            col = colores[(k + fila * 2) % len(colores)]
            c.caja("Producto", (-1.28 + k * 0.17, y, 2.03), (0.12, 0.2, 0.02), _mat("producto_%d" % ((k + fila * 2) % 5), col, 0.6))
    # Cortina metálica enrollada arriba de la puerta, con la parte que sigue bajada.
    cortina = _mat("cortina_metalica", (0.62, 0.63, 0.64), 0.4)
    c.cilindro("Rollo de la cortina", (-0.6, 1.98, 2.12), 0.1, 1.7, cortina, eje="x", lados=12)
    for k in range(3):
        c.caja("Lama", (-0.6, 1.82 - k * 0.06, 2.06), (1.62, 0.055, 0.02), cortina)
    # Ventana con protecciones y avisos de ofertas pegados.
    _ventana(1.1, 1.25, 2.0, 1.2, 1.0, frente=1, rejas=True)
    for i, (dx, dy) in enumerate(((-0.3, 0.15), (0.25, -0.2))):
        aviso = p.hoja(64, 64, (0.97, 0.96, 0.90), semilla=i, grano=0)
        p.numero(aviso, "990" if i == 0 else "1500", 0.35, 0.08, 0.3, (0.78, 0.16, 0.14))
        o = c.caja("Aviso", (1.1 + dx, 1.25 + dy, 2.045), (0.32, 0.32, 0.003), c.material("aviso_oferta_%d" % i, (1, 1, 1), 0.9, textura=p.imagen("aviso_oferta_%d" % i, aviso)))
        c.uv_plano_frente(o)
    # Cajones de bebidas retornables apilados junto a la puerta.
    for i, (dx, dy) in enumerate(((-1.75, 0.15), (-1.75, 0.45), (-1.4, 0.15))):
        caja = _mat("cajon_bebidas", (0.20, 0.42, 0.28), 0.6)
        c.caja("Cajon de bebidas", (dx, dy, 2.35), (0.34, 0.28, 0.44), caja, bisel=0.01)
        for bx in (-0.08, 0.08):
            for bz in (-0.12, 0.0, 0.12):
                c.cilindro("Botella", (dx + bx, dy + 0.2, 2.35 + bz), 0.035, 0.16, _mat("botella", (0.25, 0.12, 0.08), 0.2), lados=8)
    # Atrás: la puerta de la casa del almacenero, una ventana y el estanque de agua.
    _puerta(0.9, -2.0, color=(0.40, 0.42, 0.45))
    _ventana(-0.8, 1.4, -2.0, 1.0, 0.8, frente=-1, rejas=True)
    c.caja("Medidor de luz", (1.6, 1.6, -2.08), (0.3, 0.4, 0.14), _mat("medidor", (0.75, 0.76, 0.74), 0.5), bisel=0.01)
    c.cilindro("Estanque", (-1.2, 2.95, -1.2), 0.38, 0.7, _mat("estanque", (0.18, 0.20, 0.22), 0.6), lados=16)
    c.caja("Vereda del local", (0, 0.02, 2.5), (4.0, 0.04, 1.0), c.material("radier", (1, 1, 1), 0.95, textura=textura_cemento("radier", juntas=64)))


# ------------------------------------------------------------------ plaza

def _banca(x, z, giro=0):
    """Banca de plaza: listones de madera sobre patas de fierro fundido."""
    madera = _mat("listones_banca", (0.55, 0.40, 0.28), 0.7)
    fierro = _mat("fierro_banca", (0.18, 0.20, 0.19), 0.5)
    piezas = []
    for k in range(3):
        piezas.append(c.caja("Liston", (x, 0.45, z - 0.15 + k * 0.13), (1.8, 0.04, 0.1), madera, bisel=0.01))
    for k in range(2):
        piezas.append(c.caja("Respaldo", (x, 0.65 + k * 0.15, z + 0.26), (1.8, 0.1, 0.04), madera, bisel=0.01))
    for dx in (-0.75, 0.75):
        piezas.append(c.caja("Pata", (x + dx, 0.22, z), (0.06, 0.44, 0.48), fierro))
        piezas.append(c.caja("Pata respaldo", (x + dx, 0.62, z + 0.25), (0.06, 0.4, 0.05), fierro))
    if giro:
        for o in piezas:
            o.location = c.unity(*p.girar_punto((-o.location.x, o.location.z, -o.location.y), (x, z), giro))
            c.girar_y(o, giro)


def _farol(x, z):
    fierro = _mat("fierro_farol", (0.16, 0.18, 0.17), 0.5)
    c.cilindro("Poste del farol", (x, 1.6, z), 0.05, 3.2, fierro, lados=10)
    c.caja("Farol", (x, 3.3, z), (0.3, 0.3, 0.3), fierro, bisel=0.02, ahusado=0.7)
    c.caja("Vidrio del farol", (x, 3.25, z), (0.24, 0.2, 0.24), _mat("vidrio_farol", (0.85, 0.85, 0.78), 0.2))


def _columpio(x, z):
    """Columpio de dos asientos: marco de fierro pintado y cadenas."""
    marco = _mat("columpio", (0.25, 0.42, 0.62), 0.5)
    for dx in (-1.0, 1.0):
        for dz in (-0.5, 0.5):
            pata = c.cilindro("Pata columpio", (x + dx, 1.0, z + dz * 0.5), 0.04, 2.1, marco, lados=8)
            c.girar_x(pata, dz * 28)
    c.cilindro("Barra columpio", (x, 1.95, z), 0.045, 2.1, marco, eje="x", lados=8)
    cadena = _mat("cadena", (0.60, 0.60, 0.62), 0.3)
    for dx in (-0.45, 0.45):
        for dz in (-0.18, 0.18):
            c.cilindro("Cadena", (x + dx, 1.25, z + dz), 0.008, 1.4, cadena, lados=4)
        c.caja("Asiento", (x + dx, 0.53, z), (0.44, 0.03, 0.18), _mat("asiento_columpio", (0.15, 0.15, 0.16), 0.7), bisel=0.01)


def plaza():
    """Plaza de barrio de 9 x 7 m: pasto, senderos de maicillo, bancas, columpio, farol y tres árboles."""
    verde = c.material("pasto_plaza", (1, 1, 1), aspereza=0.95, textura=textura_pasto("pasto_plaza", (0.40, 0.58, 0.34)))
    o = c.caja("Plaza", (0, 0.02, 0), (9.0, 0.04, 7.0), verde)
    c.uv_plano_arriba(o)
    solera = _mat("solera", (0.74, 0.73, 0.70), 0.9)
    for z in (-3.55, 3.55):
        c.caja("Solera", (0, 0.07, z), (9.1, 0.14, 0.12), solera)
    for x in (-4.55, 4.55):
        c.caja("Solera", (x, 0.07, 0), (0.12, 0.14, 7.1), solera)
    mai = c.material("maicillo", (1, 1, 1), aspereza=1.0, textura=textura_maicillo())
    o = c.caja("Sendero", (0, 0.045, 1.9), (9.0, 0.02, 1.4), mai)
    c.uv_plano_arriba(o)
    o = c.caja("Sendero", (-0.6, 0.046, -1.0), (1.2, 0.02, 5.2), mai)
    c.uv_plano_arriba(o)
    _banca(0, 2.6, giro=180)
    _banca(-3.2, 2.6, giro=180)
    _columpio(-2.6, -1.8)
    _farol(1.0, 1.4)
    c.cilindro("Basurero", (2.2, 0.4, 2.55), 0.2, 0.8, _mat("basurero", (0.22, 0.38, 0.28), 0.6), lados=14)
    for i, (x, z) in enumerate(((-3.0, -0.6), (1.5, -0.6), (2.4, 1.8))):
        arbol(x, z, semilla=10 + i, alto=1.0 + 0.08 * i)


# ------------------------------------------------------------------ paradero

def paradero():
    """Paradero de micro como los de Santiago: estructura gris, techo, panel de vidrio, banca y señal."""
    gris = _mat("estructura_paradero", (0.26, 0.27, 0.29), 0.45)
    rojo = _mat("borde_paradero", (0.85, 0.30, 0.25), 0.5)
    for x in (-0.9, 0.9):
        c.caja("Poste del paradero", (x, 1.1, -0.35), (0.08, 2.2, 0.08), gris)
    c.caja("Techo del paradero", (0, 2.25, 0), (2.4, 0.08, 1.0), gris, bisel=0.01)
    c.caja("Borde del techo", (0, 2.25, 0.5), (2.4, 0.12, 0.03), rojo)
    vidrio = _mat("vidrio_paradero", (0.60, 0.70, 0.74), 0.05)
    c.caja("Panel de vidrio", (0, 1.25, -0.38), (1.72, 1.6, 0.02), vidrio)
    c.caja("Marco panel", (0, 2.07, -0.38), (1.8, 0.05, 0.05), gris)
    c.caja("Marco panel", (0, 0.43, -0.38), (1.8, 0.05, 0.05), gris)
    # Banca de metal y el panel lateral con el mapa de recorridos (dibujado, sin letras reales).
    c.caja("Banca", (0, 0.46, -0.2), (1.5, 0.04, 0.3), gris, bisel=0.01)
    for x in (-0.6, 0.6):
        c.caja("Pata banca", (x, 0.23, -0.2), (0.04, 0.46, 0.25), gris)
    mapa = p.hoja(256, 128, (0.95, 0.95, 0.93), grano=0)
    p.rect(mapa, 0.0, 0.12, 0.0, 1.0, (0.85, 0.30, 0.25))
    p.renglones(mapa, 0.16, 0.42, 0.08, 0.92, (0.25, 0.25, 0.3), 2, paso=0.05, grueso=0.4)
    for k, col in enumerate(((0.85, 0.30, 0.25), (0.22, 0.45, 0.70), (0.25, 0.55, 0.35))):
        p.rect(mapa, 0.5 + k * 0.1, 0.52 + k * 0.1, 0.1, 0.9, col)
    o = c.caja("Mapa de recorridos", (0.9, 1.25, 0.05), (0.04, 1.4, 0.7), c.material("mapa_paradero", (1, 1, 1), 0.5, textura=p.imagen("mapa_paradero", mapa)))
    p.uv_plano_lado(o)
    # Señal de parada con el número de los recorridos, en su poste.
    c.cilindro("Poste de la senal", (-1.15, 1.25, 0.45), 0.03, 2.5, gris, lados=8)
    senal = p.hoja(128, 96, (0.95, 0.95, 0.93), grano=0)
    p.rect(senal, 0.0, 0.25, 0.0, 1.0, (0.85, 0.30, 0.25))
    p.numero(senal, "210", 0.35, 0.12, 0.22, (0.2, 0.2, 0.24))
    p.numero(senal, "104", 0.65, 0.12, 0.22, (0.2, 0.2, 0.24))
    o = c.caja("Senal de parada", (-1.15, 2.3, 0.45), (0.36, 0.48, 0.02), c.material("senal_parada", (1, 1, 1), 0.5, textura=p.imagen("senal_parada", senal)))
    c.uv_plano_frente(o)


# ------------------------------------------------------------------ escombros

def escombros():
    """Lo que cae después de un terremoto: ladrillos sueltos, trozos de hormigón y un pedazo de muro."""
    ladrillo = c.material("ladrillo_escombro", (1, 1, 1), aspereza=0.9, textura=of.textura_ladrillo("ladrillo_escombro"))
    hormigon = _mat("hormigon", (0.62, 0.60, 0.58), 0.95)
    rojo = _mat("ladrillo_suelto", (0.68, 0.36, 0.26), 0.9)
    polvo = _mat("polvo", (0.58, 0.56, 0.53), 1.0)
    for i, (x, z, r) in enumerate(((0.0, 0.0, 0.7), (0.5, 0.2, 0.5), (-0.5, -0.1, 0.45))):
        c.cilindro("Polvo", (x, 0.003, z), r, 0.006, polvo, lados=14)
    rng = np.random.default_rng(7)
    for i in range(16):
        x, z = (rng.random() - 0.5) * 1.4, (rng.random() - 0.5) * 0.9
        y = 0.05 + (0.08 if i > 10 else 0)
        o = c.caja("Ladrillo", (x, y, z), (0.29, 0.07, 0.14), rojo, bisel=0.008)
        o.rotation_euler = (rng.random() * 0.5, rng.random() * 0.5, rng.random() * math.pi)
    for i in range(6):
        x, z = (rng.random() - 0.5) * 1.2, (rng.random() - 0.5) * 0.8
        s = 0.14 + rng.random() * 0.18
        o = c.caja("Hormigon", (x, s / 2, z), (s * 1.4, s, s * 1.1), hormigon, bisel=0.02, ahusado=0.8)
        o.rotation_euler = (rng.random() * 0.6, rng.random() * 0.6, rng.random() * math.pi)
    muro = c.caja("Trozo de muro", (0.7, 0.14, 0.1), (0.9, 0.12, 0.55), ladrillo, bisel=0.01)
    c.uv_cubo(muro, 0.6)
    muro.rotation_euler = (math.radians(12), 0, math.radians(-25))
