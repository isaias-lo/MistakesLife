"""La gente del juego: piezas para armar cualquier persona (acuerdo con Isaias del 28-11-2026).

Una persona se arma con piezas compartidas, así pesa poco:
- gente.cabeza: una sola cabeza base. Su forma cambia con "llaves de forma" (blend shapes en Unity):
  ancha, angosta, menton, nariz_grande, nariz_chica, labios_gruesos y pomulos, de 0 a 100.
- gente.ojos: ojos, iris, párpados y pestañas (llaves ojos_grandes y edad, que baja el párpado).
- gente.cejas_finas, gente.cejas_gruesas, gente.lentes.
- gente.pelo_rapado, gente.pelo_corto, gente.pelo_largo, gente.pelo_mono (el moño).
- gente.barba_bigote, gente.barba_corta, gente.barba_completa.
- gente.cuerpo_hombre, gente.cuerpo_mujer, gente.cuerpo_nino.
El pelo y la barba tienen las mismas llaves que la cabeza: Unity les pone los mismos valores y
calzan con cualquier cara.

Colores: los materiales se llaman piel, pelo, iris, polera, pantalon y zapatos, y Unity les pone el
color de cada persona. La textura de la cara es casi blanca (labios, rubor y párpados un poco más
oscuros): multiplicada por el color de piel da la cara de cualquier tono.

Estilo "rasgos marcados" (estilo A de los bocetos) con la cabeza un poco más grande que la real
(opción B). Medidas: la cabeza mide 0,27 m de alto en un adulto de 1,70 m.
Todo mira hacia -z de Unity (hacia la cámara de la maqueta). La cabeza tiene su centro en el origen;
los cuerpos tienen los pies en el origen y el cuello en CUELLO * estatura / 1,70.
"""

import math

import bmesh
import bpy
import numpy as np

import comun as c

R_CABEZA = 0.124          # radio de la cabeza de un adulto de 1,70 m (en metros)
CUELLO = 1.50 + 0.92 * R_CABEZA  # altura del centro de la cabeza en un adulto de 1,70 m
SEGMENTOS, ANILLOS = 64, 44      # unos 5.500 triángulos para la cabeza

# Perfil de la cabeza (en radios): para cada altura, el ancho y cuánto sale adelante y atrás.
ALTURAS = [-1.0, -0.96, -0.88, -0.72, -0.5, -0.3, -0.1, 0.1, 0.3, 0.5, 0.7, 0.85, 0.95, 1.0]
ANCHO = [0.0, 0.20, 0.34, 0.50, 0.62, 0.70, 0.74, 0.75, 0.76, 0.74, 0.65, 0.50, 0.30, 0.0]
ADELANTE = [0.0, 0.50, 0.74, 0.82, 0.86, 0.87, 0.86, 0.86, 0.89, 0.87, 0.78, 0.60, 0.34, 0.0]
ATRAS = [0.0, 0.12, 0.22, 0.36, 0.56, 0.80, 0.94, 1.00, 1.02, 1.00, 0.90, 0.68, 0.38, 0.0]

BASE = dict(ancho=1.0, mandibula=1.0, menton=0.0, nariz=1.0, nariz_ancho=1.0, labios=1.0, pomulos=1.0)

# Cada llave es cuánto cambian los rasgos al ponerla en 100.
LLAVES = {
    "ancha": dict(ancho=1.08, mandibula=1.10),
    "angosta": dict(ancho=0.92, mandibula=0.86),
    "menton": dict(menton=0.7),
    "nariz_grande": dict(nariz=1.35, nariz_ancho=1.3),
    "nariz_chica": dict(nariz=0.65, nariz_ancho=0.9),
    "labios_gruesos": dict(labios=1.4),
    "pomulos": dict(pomulos=1.7),
}

OJO_X, OJO_Z, OJO_R = 0.29, 0.06, 0.118


def _campana(d2, s):
    return np.exp(-d2 / (2 * s * s))


def _suave(t):
    t = np.clip(t, 0.0, 1.0)
    return t * t * (3 - 2 * t)


def _esfera_base():
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=SEGMENTOS, v_segments=ANILLOS, radius=1.0)
    return bm


_ESFERA = None


def _puntos_esfera():
    global _ESFERA
    if _ESFERA is None:
        bm = _esfera_base()
        _ESFERA = np.array([v.co[:] for v in bm.verts])
        bm.free()
    return _ESFERA


def forma_cabeza(rasgos):
    """Posiciones de la cabeza (en radios, cara hacia -y de Blender) para unos rasgos."""
    R = dict(BASE, **rasgos)
    V = _puntos_esfera()
    Z = np.clip(V[:, 2], -1, 1)
    rr = np.sqrt(np.clip(1 - Z * Z, 1e-9, 1))
    cx, cy = V[:, 0] / rr, V[:, 1] / rr
    w = np.interp(Z, ALTURAS, ANCHO) * R["ancho"]
    baja = np.clip((-Z - 0.2) / 0.8, 0, 1)
    w *= 1 - (1 - R["mandibula"]) * baja
    adelante, atras = np.interp(Z, ALTURAS, ADELANTE), np.interp(Z, ALTURAS, ATRAS)
    frente = cy < 0
    plano = np.where((Z > -0.75) & (Z < 0.75), 0.75, 1.0)  # la cara es más plana que el cráneo
    sy = np.sign(cy) * np.abs(cy) ** np.where(frente, plano, 1.0)
    sx = np.sign(cx) * np.abs(cx) ** np.where(frente, plano, 1.0)
    x = w * sx
    y = np.where(frente, adelante * sy, atras * sy)
    z = Z * 1.08
    fr = np.clip(-cy, 0, 1) ** 2

    def bulto(px, pz, s, alto):
        return _campana((x - px) ** 2 + (z - pz) ** 2, s) * alto * fr

    dy = np.zeros_like(y)
    for lado in (-1, 1):
        dy += bulto(lado * OJO_X, OJO_Z, 0.10, -0.085)                      # cuenca del ojo
        dy += bulto(lado * (OJO_X + 0.03), 0.22, 0.08, 0.035)                 # arco de la ceja
        dy += bulto(lado * 0.44, -0.14, 0.12, 0.045 * R["pomulos"])           # pómulo
        dy += bulto(lado * 0.095 * R["nariz_ancho"], -0.32, 0.045, 0.075 * R["nariz"])  # aleta
        dy += bulto(lado * 0.20, -0.53, 0.05, -0.02)                         # comisura
        dy += bulto(lado * 0.52, -0.62, 0.12, -0.015)                        # bajo el pómulo
    # Nariz: un filo que crece desde entre los ojos hasta la punta.
    t = np.clip((0.10 - z) / 0.42, 0, 1)
    en_nariz = (z < 0.10) & (z > -0.36)
    dy += np.where(en_nariz, _campana((x / (0.07 * R["nariz_ancho"])) ** 2, 1.0) * (0.03 + 0.19 * t ** 1.6) * R["nariz"] * fr, 0)
    dy += bulto(0, -0.375, 0.04, -0.025)
    dy += bulto(0, -0.45, 0.055, 0.04 * R["labios"])
    dy += bulto(0, -0.585, 0.06, 0.045 * R["labios"])
    dy += bulto(0, -0.515, 0.025, -0.02)
    dy += bulto(0, -0.84, 0.10, 0.03 + 0.04 * R["menton"])
    return np.stack([x, y - dy, z], 1)


def _normales(P):
    bm = _esfera_base()
    for v, p in zip(bm.verts, P):
        v.co = p
    bm.normal_update()
    N = np.array([v.normal[:] for v in bm.verts])
    bm.free()
    return N


def _frontal(P):
    return np.clip(-P[:, 1] / np.maximum(1e-6, np.hypot(P[:, 0], P[:, 1])), 0, 1)


# ------------------------------------------------------------------ mallas con llaves de forma

def _a_unity(P, escala):
    """De radios de la cabeza a metros, girada para que la cara mire hacia -z de Unity."""
    Q = P * escala
    Q[:, 0] *= -1
    Q[:, 1] *= -1
    return Q


def _malla(nombre, bm, materiales):
    me = bpy.data.meshes.new(nombre)
    bm.to_mesh(me)
    obj = bpy.data.objects.new(nombre, me)
    bpy.context.scene.collection.objects.link(obj)
    for m in materiales:
        me.materials.append(m)
    me.shade_smooth()
    return obj


def _poner_llaves(obj, posiciones):
    """`posiciones(rasgos)` da las posiciones de los vértices para esos rasgos; una llave por rasgo."""
    obj.shape_key_add(name="Basis")
    for nombre, rasgos in LLAVES.items():
        llave = obj.shape_key_add(name=nombre)
        llave.data.foreach_set("co", posiciones(rasgos).astype(np.float32).ravel())


def _textura_cara():
    """Casi blanca: Unity la multiplica por el color de piel. Labios, rubor, párpados y nariz."""
    n = 256
    v, u = np.indices((n, n)) / n
    x = u * 1.6 - 0.8
    z = (1 - v) * 2.24 - 1.12
    col = np.ones((n, n, 3))

    def mezcla(m, color):
        nonlocal col
        col = col * (1 - m[..., None]) + np.array(color) * m[..., None]

    for lado in (-1, 1):
        mezcla(0.12 * np.exp(-((x - lado * 0.42) ** 2 + (z + 0.22) ** 2) / 0.02), (0.98, 0.84, 0.82))
        mezcla(0.22 * np.exp(-((x - lado * OJO_X) ** 2 / 0.012 + (z - 0.12) ** 2 / 0.002)), (0.80, 0.78, 0.78))
        mezcla(0.6 * np.exp(-((x - lado * 0.05) ** 2 / 0.0006 + (z + 0.355) ** 2 / 0.0002)), (0.45, 0.42, 0.42))
    mezcla(0.08 * np.exp(-(x ** 2 + (z + 0.3) ** 2) / 0.006), (0.98, 0.86, 0.84))
    lx = x / 0.17
    arriba = (z + 0.515) / 0.045
    abajo = -(z + 0.515) / 0.06
    forma = np.where(z > -0.515, lx ** 2 + (arriba - 0.25 * np.abs(np.sin(lx * 3.1))) ** 2, lx ** 2 + abajo ** 2)
    mezcla(np.clip((1 - forma) * 4, 0, 1) * 0.9, (0.92, 0.70, 0.70))
    mezcla(np.clip((1 - (lx ** 2 + ((z + 0.515) / 0.006) ** 2)) * 3, 0, 1) * 0.6, (0.55, 0.42, 0.42))
    pix = np.concatenate([np.clip(col, 0, 1), np.ones((n, n, 1))], 2)
    return c.imagen("cara", pix[::-1].copy())


def _uv_cara(x, z, y):
    return ((x + 0.8) / 1.6, (z + 1.12) / 2.24) if y < 0 else (0.02, 0.5)


def _orejas(bm, ancho):
    """Dos orejas (elipsoides) pegadas a los lados; se mueven con el ancho de la cara."""
    for lado in (-1, 1):
        geo = bmesh.ops.create_uvsphere(bm, u_segments=12, v_segments=8, radius=0.19)
        for v in geo["verts"]:
            x, y, z = v.co
            x, y = x * 0.32, y * 0.55
            a = math.radians(-15 * lado)
            x, y = x * math.cos(a) - y * math.sin(a), x * math.sin(a) + y * math.cos(a)
            v.co = (x + lado * 0.73 * ancho, y + 0.08, z - 0.04)


def cabeza():
    piel = c.material("piel", (1, 1, 1), aspereza=0.5, textura=_textura_cara())

    def construir(rasgos):
        R = dict(BASE, **rasgos)
        bm = _esfera_base()
        P = forma_cabeza(rasgos)
        for v, p in zip(bm.verts, P):
            v.co = p
        _orejas(bm, R["ancho"])
        return bm

    bm = construir({})
    capa = bm.loops.layers.uv.new()
    for cara in bm.faces:
        for lazo in cara.loops:
            x, y, z = lazo.vert.co
            lazo[capa].uv = _uv_cara(x, z, y)
    for v in bm.verts:
        v.co = _a_unity(np.array([v.co[:]]), R_CABEZA)[0]
    obj = _malla("Cabeza", bm, [piel])
    bm.free()

    def posiciones(rasgos):
        b = construir(rasgos)
        P = np.array([v.co[:] for v in b.verts])
        b.free()
        return _a_unity(P, R_CABEZA)

    _poner_llaves(obj, posiciones)
    return obj


# ------------------------------------------------------------------ pelo y barba

def _cascara(nombre, cuanto, crecer, mat, alargar=None):
    """Copia de la cabeza que sale hacia afuera donde `cuanto` (0 a 1) es alto y queda escondida bajo
    la piel donde es 0: el borde del pelo o de la barba sale suave, sin escalones."""

    def posiciones_radios(rasgos):
        P = forma_cabeza(rasgos)
        N = _normales(P)
        base = forma_cabeza({})
        m = cuanto(base)  # la máscara sale siempre de la cara base: así no cambia la malla
        Q = P + N * (crecer * m - 0.03 * (1 - m))[:, None]
        if alargar is not None:
            Q = alargar(Q, m)
        return Q

    bm = _esfera_base()
    bm.verts.index_update()
    for v, p in zip(bm.verts, posiciones_radios({})):
        v.co = p
    # Se borran las caras que quedan siempre escondidas (pesan sin verse).
    m = cuanto(forma_cabeza({}))
    escondidas = [f for f in bm.faces if all(m[v.index] < 0.02 for v in f.verts)]
    bmesh.ops.delete(bm, geom=escondidas, context="FACES_ONLY")
    sueltos = [v for v in bm.verts if not v.link_faces]
    indices_sueltos = {v.index for v in sueltos}
    bmesh.ops.delete(bm, geom=sueltos, context="VERTS")
    for v in bm.verts:
        v.co = _a_unity(np.array([v.co[:]]), R_CABEZA)[0]
    obj = _malla(nombre, bm, [mat])
    bm.free()
    total = len(_puntos_esfera())
    quedan = np.array([i for i in range(total) if i not in indices_sueltos])

    def posiciones(rasgos):
        return _a_unity(posiciones_radios(rasgos)[quedan], R_CABEZA)

    _poner_llaves(obj, posiciones)
    return obj


def _mat_pelo():
    return c.material("pelo", (0.14, 0.10, 0.07), aspereza=0.55)


def pelo(peinado):
    P0 = forma_cabeza({})
    f0 = _frontal(P0)
    z0 = P0[:, 2]
    x0 = np.abs(P0[:, 0])
    frente = 0.56 - 0.05 * _suave((x0 - 0.3) / 0.2)
    if peinado == "rapado":
        linea = np.where(f0 > 0.55, frente + 0.02, 0.10 + (frente - 0.10) * _suave((f0 - 0.4) / 0.15))
        linea = np.where(f0 < 0.4, -0.45 + 0.55 * _suave(f0 / 0.4), linea)
        crecer = 0.015
    elif peinado == "largo":
        linea = np.where(f0 > 0.55, frente, frente - (0.55 - f0) * 3.0)
        crecer = 0.08
    else:  # corto y moño
        lado, atras = 0.10, -0.55
        linea = np.where(f0 > 0.55, frente, np.where(f0 < 0.4, atras + (lado - atras) * _suave(f0 / 0.55), lado + (frente - lado) * _suave((f0 - 0.4) / 0.15)))
        crecer = 0.07 if peinado == "corto" else 0.05
    mascara = _suave((z0 - linea) / 0.06)

    def cuanto(P):
        return mascara

    def alargar(Q, m):
        Q = Q.copy()
        if peinado == "largo":
            caen = (m > 0.5) & (Q[:, 2] < 0.0) & (f0 < 0.55)
            d = np.where(caen, -Q[:, 2], 0)
            Q[:, 2] -= d * 1.6
            Q[:, 1] += d * 0.25
            Q[:, 0] *= 1 + d * 0.15
        elif peinado == "corto":
            arriba = (m > 0.5) & (Q[:, 2] > 0.3)
            Q[:, 2] += np.where(arriba, 0.03 * Q[:, 2], 0)
        return Q

    nombre = {"mono": "Pelo con moño"}.get(peinado, "Pelo " + peinado)
    mat = _mat_pelo()
    obj = _cascara(nombre, cuanto, crecer, mat, alargar)
    if peinado == "mono":
        bm = bmesh.new()
        bmesh.ops.create_uvsphere(bm, u_segments=16, v_segments=10, radius=0.3)
        for v in bm.verts:
            v.co = _a_unity(np.array([[v.co.x, v.co.y + 0.75, v.co.z + 0.62]]), R_CABEZA)[0]
        _malla("Moño", bm, [mat])
        bm.free()
    return obj


def barba(tipo):
    P0 = forma_cabeza({})
    f0 = _frontal(P0)
    x, y, z = P0[:, 0], P0[:, 1], P0[:, 2]
    boca = 1 - _suave(((x / 0.2) ** 2 + ((z + 0.52) / 0.075) ** 2 - 1) / 0.6)
    bigote = _suave((f0 - 0.75) / 0.1) * _suave((0.24 - np.abs(x)) / 0.05) * _suave((-0.36 - z) / 0.03) * _suave((z + 0.48) / 0.03)
    if tipo == "bigote":
        mascara = bigote
    else:
        mandibula = _suave((-0.22 - z) / 0.1) * _suave((0.5 - y) / 0.15)
        patilla = _suave((np.abs(x) - 0.6) / 0.06) * _suave((0.12 - z) / 0.08) * _suave((0.15 - y) / 0.1)
        mascara = np.maximum(np.maximum(mandibula, patilla), bigote) * (1 - boca)
    crecer = {"bigote": 0.03, "corta": 0.02, "completa": 0.055}[tipo]
    return _cascara("Barba " + tipo, lambda P: mascara, crecer, _mat_pelo())


# ------------------------------------------------------------------ ojos, cejas y lentes

def _superficie(P, px, pz, rad=0.04):
    m = (np.abs(P[:, 0] - px) < rad) & (np.abs(P[:, 2] - pz) < rad) & (P[:, 1] < 0)
    return P[m, 1].min() if m.any() else -0.9


def _textura_iris():
    """Iris gris claro con fibras y pupila negra: Unity lo tiñe con el color de ojos."""
    n = 64
    yy, xx = np.indices((n, n))
    r = np.hypot(yy - n / 2, xx - n / 2) / (n / 2)
    ang = np.arctan2(yy - n / 2, xx - n / 2)
    pix = np.ones((n, n, 4))
    valor = (0.55 + 0.45 * r) * (0.85 + 0.15 * np.sin(ang * 40))
    pix[..., :3] = valor[..., None]
    pix[r > 0.9, :3] = 0.35
    pix[r < 0.38, :3] = 0.03
    pix[np.hypot(yy - n * 0.36, xx - n * 0.36) < 3, :3] = 1.0
    return c.imagen("iris", pix)


def ojos():
    """Ojos con párpados. Llaves: ojos_grandes y edad (el párpado de arriba baja con los años)."""
    P = forma_cabeza({})
    esclera = c.material("esclera", (0.95, 0.94, 0.91), aspereza=0.15)
    iris = c.material("iris", (1, 1, 1), aspereza=0.1, textura=_textura_iris())
    piel = c.material("piel", (0.86, 0.68, 0.56), aspereza=0.5)
    pestanas = c.material("pestanas", (0.06, 0.045, 0.04), aspereza=0.6)

    def construir(tam, edad):
        bm = bmesh.new()
        uv = bm.loops.layers.uv.new()
        ro = OJO_R * tam
        for lado in (-1, 1):
            yf = _superficie(P, lado * OJO_X, OJO_Z)
            centro = np.array([lado * OJO_X, yf + OJO_R * 0.62, OJO_Z])
            # Esclera, con el casquete de adelante como iris (misma esfera, otro material).
            geo = bmesh.ops.create_uvsphere(bm, u_segments=20, v_segments=14, radius=ro)
            for v in geo["verts"]:
                v.co = centro + np.array(v.co[:])
            for f in {f for v in geo["verts"] for f in v.link_faces}:
                d = (np.array(f.calc_center_median()[:]) - centro) / ro
                f.material_index = 1 if d[1] < -0.86 else 0
                for lazo in f.loops:
                    x, _, zz = (np.array(lazo.vert.co[:]) - centro) / ro
                    lazo[uv].uv = (0.5 + x / 0.52 * 0.5, 0.5 + zz / 0.52 * 0.5)
            # Párpado de arriba: casquete de piel; baja con la edad.
            corte = ro * (0.44 - 0.22 * edad)
            geo = bmesh.ops.create_uvsphere(bm, u_segments=16, v_segments=10, radius=ro * 1.08)
            for v in geo["verts"]:
                x, yv, zz = v.co
                zz = max(zz, corte)  # aplastado en la línea del párpado (sin borrar: misma malla siempre)
                v.co = centro + np.array([x, yv, zz])
            for f in {f for v in geo["verts"] for f in v.link_faces}:
                f.material_index = 2
            # Pestañas: un aro fino en el borde del párpado.
            radio = math.sqrt(max(1e-8, (ro * 1.06) ** 2 - corte ** 2))
            geo = bmesh.ops.create_cone(bm, cap_ends=False, segments=16, radius1=radio, radius2=radio * 1.02, depth=ro * 0.06)
            for v in geo["verts"]:
                x, yv, zz = v.co
                v.co = centro + np.array([x, yv, corte + zz])
            for f in {f for v in geo["verts"] for f in v.link_faces}:
                f.material_index = 3
            # Párpado de abajo.
            geo = bmesh.ops.create_uvsphere(bm, u_segments=16, v_segments=10, radius=ro * 1.04)
            for v in geo["verts"]:
                x, yv, zz = v.co
                v.co = centro + np.array([x, yv, min(zz, -ro * 0.70)])
            for f in {f for v in geo["verts"] for f in v.link_faces}:
                f.material_index = 2
        return bm

    bm = construir(1.0, 0.0)
    for v in bm.verts:
        v.co = _a_unity(np.array([v.co[:]]), R_CABEZA)[0]
    obj = _malla("Ojos", bm, [esclera, iris, piel, pestanas])
    bm.free()
    obj.shape_key_add(name="Basis")
    for nombre, (tam, edad) in (("ojos_grandes", (1.18, 0.0)), ("edad", (1.0, 0.8))):
        b = construir(tam, edad)
        llave = obj.shape_key_add(name=nombre)
        llave.data.foreach_set("co", _a_unity(np.array([v.co[:] for v in b.verts]), R_CABEZA).astype(np.float32).ravel())
        b.free()
    return obj


def cejas(gruesas):
    """Cejas: una tira que sigue la frente y se afina hacia afuera."""
    P = forma_cabeza({})
    bm = bmesh.new()
    grosor = 0.034 if gruesas else 0.022
    for lado in (-1, 1):
        filas = []
        for j in range(11):
            u = (j - 5) / 5
            px = lado * (OJO_X + 0.03 + u * 0.13)
            pz = 0.205 + 0.03 * (1 - u * u) - 0.012 * u
            yy = _superficie(P, px, pz, 0.03) - 0.006
            g = grosor * (1.1 - 0.5 * max(0.0, u))
            filas.append([bm.verts.new((px, yy - dy, pz + dz)) for dz, dy in ((-g, 0.0), (0.0, 0.012), (g, 0.0))])
        for a, b in zip(filas, filas[1:]):
            for k in range(2):
                bm.faces.new((a[k], b[k], b[k + 1], a[k + 1]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    for v in bm.verts:
        v.co = _a_unity(np.array([v.co[:]]), R_CABEZA)[0]
    obj = _malla("Cejas gruesas" if gruesas else "Cejas finas", bm, [_mat_pelo()])
    bm.free()
    return obj


def lentes():
    P = forma_cabeza({})
    marco = c.material("marco_lentes", (0.08, 0.08, 0.09), aspereza=0.3)
    vidrio = c.material("vidrio_lentes", (0.85, 0.88, 0.9), aspereza=0.05)
    bm = bmesh.new()
    for lado in (-1, 1):
        y = _superficie(P, lado * OJO_X, OJO_Z) - 0.07
        geo = bmesh.ops.create_circle(bm, cap_ends=False, segments=24, radius=0.135)
        anillo = geo["verts"]
        for v in anillo:
            x, z, _ = v.co
            v.co = (lado * OJO_X + x * 1.2, y, OJO_Z + z * 0.85)
        # Grosor del marco: el anillo se extruye hacia afuera.
        ext = bmesh.ops.extrude_edge_only(bm, edges=[e for e in bm.edges if all(v in anillo for v in e.verts)])
        for v in [e for e in ext["geom"] if isinstance(e, bmesh.types.BMVert)]:
            d = np.array([v.co.x - lado * OJO_X, 0, v.co.z - OJO_Z])
            n = d / (np.linalg.norm(d) or 1)
            v.co = (v.co.x + n[0] * 0.022, v.co.y, v.co.z + n[2] * 0.022)
        # Patilla hacia la oreja.
        a = bm.verts.new((lado * (OJO_X + 0.16), y, OJO_Z + 0.03))
        b = bm.verts.new((lado * 0.72, 0.05, OJO_Z + 0.0))
        a2 = bm.verts.new((lado * (OJO_X + 0.16), y, OJO_Z + 0.05))
        b2 = bm.verts.new((lado * 0.72, 0.05, OJO_Z + 0.02))
        bm.faces.new((a, b, b2, a2))
    yp = _superficie(P, 0, OJO_Z + 0.03) - 0.075
    v1, v2 = bm.verts.new((-0.12, yp, OJO_Z + 0.03)), bm.verts.new((0.12, yp, OJO_Z + 0.03))
    v3, v4 = bm.verts.new((0.12, yp, OJO_Z + 0.05)), bm.verts.new((-0.12, yp, OJO_Z + 0.05))
    bm.faces.new((v1, v2, v3, v4))
    for v in bm.verts:
        v.co = _a_unity(np.array([v.co[:]]), R_CABEZA)[0]
    obj = _malla("Lentes", bm, [marco, vidrio])
    bm.free()
    return obj


# ------------------------------------------------------------------ cuerpos

def cuerpo(tipo):
    """Cuerpo de 1,70 m (Unity lo escala a la estatura de cada persona) con ropa simple: polera,
    pantalón y zapatos. Los colores los pone Unity. Pies en el origen, mirando hacia -z de Unity."""
    mujer, nino = tipo == "mujer", tipo == "nino"
    P = {"pelvis": (0, 0, 0.95), "cintura": (0, 0, 1.08), "pecho": (0, 0, 1.30), "cuello": (0, 0, 1.52),
         "cadI": (0.10, 0, 0.92), "rodI": (0.105, -0.01, 0.50), "tobI": (0.105, 0.01, 0.09), "pieI": (0.105, -0.10, 0.04),
         "cadD": (-0.10, 0, 0.92), "rodD": (-0.105, -0.01, 0.50), "tobD": (-0.105, 0.01, 0.09), "pieD": (-0.105, -0.10, 0.04)}
    hombro = 0.185 if mujer else 0.205
    for lado, s in (("I", 1), ("D", -1)):
        P["hom" + lado] = (s * hombro, 0, 1.39)
        P["cod" + lado] = (s * (hombro + 0.045), 0.01, 1.14)
        P["mun" + lado] = (s * (hombro + 0.06), -0.01, 0.89)
        P["mano" + lado] = (s * (hombro + 0.06), -0.01, 0.84)
    R = {"pelvis": 0.17 if mujer else 0.154, "cintura": 0.132 if mujer else 0.148, "pecho": 0.17 if mujer else 0.187, "cuello": 0.062}
    for lado in "ID":
        R.update({"hom" + lado: 0.071, "cod" + lado: 0.052, "mun" + lado: 0.039, "mano" + lado: 0.044,
                  "cad" + lado: 0.12 if mujer else 0.116, "rod" + lado: 0.071, "tob" + lado: 0.049, "pie" + lado: 0.05})
    if nino:
        R.update({"pecho": 0.19, "cintura": 0.17, "pelvis": 0.17})
    aristas = [("pelvis", "cintura"), ("cintura", "pecho"), ("pecho", "cuello")]
    for lado in "ID":
        aristas += [("pecho", "hom" + lado), ("hom" + lado, "cod" + lado), ("cod" + lado, "mun" + lado), ("mun" + lado, "mano" + lado),
                    ("pelvis", "cad" + lado), ("cad" + lado, "rod" + lado), ("rod" + lado, "tob" + lado), ("tob" + lado, "pie" + lado)]
    claves = list(P)
    me = bpy.data.meshes.new("Cuerpo")
    me.from_pydata([P[k] for k in claves], [(claves.index(a), claves.index(b)) for a, b in aristas], [])
    obj = bpy.data.objects.new("Cuerpo", me)
    bpy.context.scene.collection.objects.link(obj)
    obj.modifiers.new("Piel", "SKIN")
    for i, k in enumerate(claves):
        me.skin_vertices[0].data[i].radius = (R[k], R[k] * 0.82)
    me.skin_vertices[0].data[0].use_root = True
    obj.modifiers.new("Suave", "SUBSURF").levels = 2
    bpy.context.view_layer.objects.active = obj
    for m in list(obj.modifiers):
        bpy.ops.object.modifier_apply(modifier=m.name)
    # Cortes rectos donde cambia la ropa (el borde de la polera, las mangas, el cuello, los zapatos).
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    manga = 0.88 if mujer else 1.25
    for z in (0.11, 0.93, manga, 1.46, 1.53):
        geo = bm.verts[:] + bm.edges[:] + bm.faces[:]
        bmesh.ops.bisect_plane(bm, geom=geo, plane_co=(0, 0, z), plane_no=(0, 0, 1))
    for x in (hombro + 0.02, -(hombro + 0.02), 0.075, -0.075):
        geo = bm.verts[:] + bm.edges[:] + bm.faces[:]
        bmesh.ops.bisect_plane(bm, geom=geo, plane_co=(x, 0, 0), plane_no=(1, 0, 0))
    bm.to_mesh(obj.data)
    bm.free()
    for nombre, color in (("polera", (0.25, 0.38, 0.32)), ("pantalon", (0.20, 0.24, 0.32)), ("zapatos", (0.16, 0.13, 0.11)), ("piel", (0.86, 0.68, 0.56))):
        obj.data.materials.append(c.material(nombre, color, aspereza=0.8 if nombre != "piel" else 0.5))
    for poly in obj.data.polygons:
        x, _, z = poly.center
        if z < 0.11:
            poly.material_index = 2
        elif abs(x) > hombro + 0.02 and z > 0.7:
            poly.material_index = 3 if z < manga else 0
        elif z > 1.53 or (z > 1.46 and abs(x) < 0.075):
            poly.material_index = 3
        elif z < 0.93:
            poly.material_index = 1
        else:
            poly.material_index = 0
    obj.data.shade_smooth()
    # Manos y zapatos.
    for lado in (-1, 1):
        h = c.esfera("Mano", (0, 0, 0), 0.05, obj.data.materials[3], escala=(0.55, 1.25, 0.85), lados=12)
        h.location = (lado * (hombro + 0.06), -0.01, 0.80)
        z = c.esfera("Zapato", (0, 0, 0), 0.06, obj.data.materials[2], escala=(0.8, 0.75, 1.9), lados=12)
        z.location = (lado * 0.105, -0.06, 0.045)
    # Todo mira hacia -z de Unity: se gira medio vuelta y se dejan las transformaciones aplicadas.
    for o in bpy.context.scene.objects:
        if o.type == "MESH":
            o.location.x, o.location.y = -o.location.x, -o.location.y
            o.rotation_euler.z += math.pi
            o.select_set(True)
            bpy.context.view_layer.objects.active = o
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    return obj
