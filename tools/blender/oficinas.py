"""Los lugares de trabajo de oficina (OfficeBuilder.cs): oficina abierta, cubículos, oficina común
y consulta, más lo que comparten (ventana, letrero, planta, archivadores, segunda pantalla).

Medidas en coordenadas de Unity desde el ancla de cada mueble, igual que la forma simple. En la
oficina la gente se sienta mirando hacia el fondo (+z): las pantallas miran hacia -z.
"""

import math

import numpy as np

import comun as c
import papeles as p

ESCRITORIO = ((0.86, 0.82, 0.74), (0.78, 0.73, 0.64))


def _mat(nombre, color, aspereza=0.5):
    return c.material(nombre, color, aspereza=aspereza)


def madera(nombre, colores=ESCRITORIO, semilla=41):
    claro, oscuro = colores
    return c.material(nombre, (1, 1, 1), aspereza=0.5, textura=c.textura_madera(nombre, claro=claro, oscuro=oscuro, semilla=semilla))


def metal_blanco():
    return _mat("metal_blanco", (0.90, 0.90, 0.89), 0.35)


def pantalla(x, y, z, ancho=0.6, alto=0.36, apoyo=None):
    """Monitor mirando hacia -z, con el centro de la pantalla en (x, y, z). `apoyo` = altura de la mesa."""
    negro = _mat("carcasa_monitor", (0.10, 0.10, 0.12), 0.35)
    vidrio = c.material("pantalla_monitor", (1, 1, 1), aspereza=0.15, textura=p.imagen("pantalla_monitor", _escritorio_pc()))
    c.caja("Monitor", (x, y, z), (ancho, alto, 0.03), negro, bisel=0.006)
    cara = c.caja("Pantalla", (x, y + 0.01, z - 0.016), (ancho - 0.03, alto - 0.04, 0.002), vidrio)
    c.uv_plano_frente(cara)
    if apoyo is not None:
        c.caja("Brazo del monitor", (x, (apoyo + y) / 2, z + 0.05), (0.05, y - apoyo, 0.025), negro, bisel=0.004)
        c.caja("Base del monitor", (x, apoyo + 0.006, z + 0.03), (0.22, 0.012, 0.16), negro, bisel=0.004)


def _escritorio_pc():
    pix = p.hoja(90, 150, (0.22, 0.30, 0.42), 3, grano=0.0)
    p.rect(pix, 0.0, 0.08, 0.0, 1.0, (0.12, 0.14, 0.18))
    p.rect(pix, 0.14, 0.9, 0.05, 0.6, (0.92, 0.93, 0.95))
    p.renglones(pix, 0.2, 0.8, 0.08, 0.56, (0.5, 0.52, 0.58), 3, paso=0.08, grueso=0.4)
    p.rect(pix, 0.14, 0.5, 0.64, 0.95, (0.85, 0.88, 0.92))
    p.rect(pix, 0.55, 0.9, 0.64, 0.95, (0.40, 0.62, 0.85))
    return pix


def teclado(x, y, z):
    c.caja("Teclado", (x, y + 0.008, z), (0.42, 0.016, 0.14), _mat("teclado", (0.18, 0.18, 0.2), 0.5), bisel=0.004)
    c.caja("Mouse", (x + 0.3, y + 0.012, z), (0.06, 0.024, 0.1), _mat("teclado", (0.18, 0.18, 0.2), 0.5), bisel=0.01)


def silla(x, z, mirando=1):
    """Silla de oficina con ruedas. `mirando` = 1 si quien se sienta mira hacia +z."""
    negro = _mat("silla_oficina", (0.16, 0.17, 0.19), 0.6)
    tela = c.material("tela_silla", (1, 1, 1), aspereza=0.9, textura=c.textura_tela("tela_silla", (0.22, 0.24, 0.28), semilla=52))
    cromo = _mat("cromo", (0.65, 0.66, 0.68), 0.25)
    for k in range(5):
        a = math.radians(k * 72 + 18)
        brazo = c.caja("Pata de la silla", (x + 0.14 * math.cos(a), 0.06, z + 0.14 * math.sin(a)), (0.28, 0.03, 0.04), negro, bisel=0.008)
        c.girar_y(brazo, -k * 72 - 18)
        c.esfera("Rueda", (x + 0.27 * math.cos(a), 0.03, z + 0.27 * math.sin(a)), 0.028, negro, lados=10)
    c.cilindro("Piston", (x, 0.25, z), 0.02, 0.34, cromo, lados=12)
    c.uv_cubo(c.caja("Asiento", (x, 0.46, z), (0.48, 0.07, 0.46), tela, bisel=0.025, segmentos=3), escala=0.5)
    respaldo = c.caja("Respaldo", (x, 0.78, z - mirando * 0.24), (0.44, 0.5, 0.05), tela, bisel=0.02, segmentos=3)
    c.uv_cubo(respaldo, escala=0.5)
    c.girar_x(respaldo, mirando * -6)
    for dx in (-0.25, 0.25):
        c.caja("Apoyabrazos", (x + dx, 0.64, z), (0.04, 0.03, 0.26), negro, bisel=0.01)
        c.caja("Soporte del apoyabrazos", (x + dx, 0.55, z - mirando * 0.05), (0.03, 0.16, 0.04), negro)


def mesa(x, z, largo, fondo, alto=0.75, mat=None, patas_blancas=True):
    """Mesa de oficina: cubierta de melamina con patas en marco de fierro."""
    mat = mat or madera("cubierta_oficina")
    c.uv_cubo(c.caja("Cubierta", (x, alto - 0.015, z), (largo, 0.03, fondo), mat, bisel=0.004), escala=1.6)
    fierro = metal_blanco() if patas_blancas else _mat("fierro_negro", (0.15, 0.15, 0.17), 0.4)
    for dx in (-largo / 2 + 0.05, largo / 2 - 0.05):
        c.caja("Pata", (x + dx, (alto - 0.03) / 2, z), (0.05, alto - 0.03, fondo - 0.1), fierro, bisel=0.005)
    c.caja("Faldon", (x, alto - 0.12, z + fondo / 2 - 0.05), (largo - 0.1, 0.16, 0.015), fierro)


def tazon(x, y, z, color=(0.90, 0.88, 0.84)):
    c.cilindro("Tazon", (x, y + 0.05, z), 0.04, 0.1, _mat("tazon_%d" % int(sum(color) * 100), color, 0.3), lados=16)


def vidrio(centro, tam):
    """Panel de vidrio esmerilado con perfil de aluminio arriba (separa un rincón)."""
    glass = _mat("vidrio_panel", (0.66, 0.80, 0.88), 0.08)
    alu = _mat("perfil_aluminio", (0.30, 0.32, 0.36), 0.35)
    x, y, z = centro
    sx, sy, sz = tam
    c.caja("Vidrio", centro, (min(sx, 0.02) if sx < sz else sx, sy, min(sz, 0.02) if sz < sx else sz), glass)
    c.caja("Perfil", (x, y + sy / 2, z), (max(sx, 0.05), 0.05, max(sz, 0.05)), alu)
    c.caja("Perfil", (x, y - sy / 2, z), (max(sx, 0.05), 0.04, max(sz, 0.05)), alu)


def planta(x=0.0, z=0.0, semilla=31):
    """Ficus en macetero, como el de la casa: macetero de 0,40 m y follaje hasta 1,15 m."""
    maceta = _mat("macetero_oficina", (0.30, 0.31, 0.33), 0.6)
    tierra = _mat("tierra", (0.24, 0.17, 0.12), 1.0)
    tallo = _mat("tallo", (0.36, 0.27, 0.18), 0.8)
    hojas = [_mat("hoja", (0.20, 0.42, 0.22), 0.45), _mat("hoja_clara", (0.28, 0.50, 0.26), 0.45)]
    c.caja("Macetero", (x, 0.22, z), (0.38, 0.44, 0.38), maceta, bisel=0.01)
    c.cilindro("Tierra", (x, 0.44, z), 0.17, 0.01, tierra, lados=20)
    c.cilindro("Tronco", (x, 0.72, z), 0.018, 0.6, tallo, lados=8)
    rng = np.random.default_rng(semilla)
    for i in range(46):
        alto = 0.6 + rng.random() * 0.55
        ang = rng.random() * 360
        radio = 0.06 + (1.15 - alto) * 0.35 + rng.random() * 0.08
        h = c.caja("Hoja", (x + radio * math.cos(math.radians(ang)), alto, z + radio * math.sin(math.radians(ang))), (0.2, 0.012, 0.09), hojas[i % 2], bisel=0.03, segmentos=3)
        h.rotation_euler = (math.radians(rng.random() * 50 - 25), math.radians(rng.random() * 40 - 20), math.radians(-ang))


def archivador(x=0.0, z=0.0):
    """Archivador metálico de cuatro cajones (0,50 x 1,20 x 0,60), con los cajones hacia -z."""
    gris = _mat("archivador", (0.55, 0.57, 0.60), 0.4)
    manilla = _mat("manilla", (0.75, 0.76, 0.78), 0.25)
    c.caja("Archivador", (x, 0.6, z), (0.5, 1.2, 0.6), gris, bisel=0.006)
    for i in range(4):
        y = 0.16 + i * 0.29
        c.caja("Cajon", (x, y, z - 0.302), (0.46, 0.26, 0.006), gris, bisel=0.003)
        c.caja("Manilla", (x, y + 0.06, z - 0.31), (0.14, 0.018, 0.015), manilla, bisel=0.004)
        c.caja("Etiqueta", (x, y + 0.1, z - 0.306), (0.07, 0.03, 0.002), _mat("etiqueta_cajon", (0.95, 0.95, 0.93), 0.8))


def ventanal(ancho, hojas):
    """Ventanal de aluminio en el muro del fondo (se ve desde -z), centro a 1,70 y 1,00 de alto."""
    alu = _mat("aluminio_oficina", (0.32, 0.34, 0.37), 0.35)
    vid = _mat("vidrio_oficina", (0.60, 0.77, 0.88), 0.05)
    y, alto = 1.7, 1.0
    c.caja("Vidrio", (0, y, 0.0), (ancho, alto, 0.01), vid)
    for i in range(hojas + 1):
        c.caja("Montante", (-ancho / 2 + i * ancho / hojas, y, -0.015), (0.04, alto + 0.04, 0.05), alu, bisel=0.002)
    for dy in (-alto / 2, alto / 2):
        c.caja("Marco", (0, y + dy, -0.015), (ancho + 0.04, 0.04, 0.05), alu, bisel=0.002)
    c.caja("Alfeizar", (0, y - alto / 2 - 0.04, -0.06), (ancho + 0.08, 0.025, 0.12), _mat("alfeizar_oficina", (0.88, 0.87, 0.84), 0.5), bisel=0.003)
    # Persianas enrolladas arriba.
    c.cilindro("Persiana", (0, y + alto / 2 + 0.06, -0.06), 0.04, ancho, _mat("persiana", (0.86, 0.85, 0.82), 0.7), eje="x", lados=14)


def letrero(grande):
    """Letrero con el nombre de la empresa en el muro del fondo (se ve desde -z)."""
    ancho = 1.8 if grande else 1.2
    pix = p.hoja(140, int(140 * ancho / 0.45), (0.20, 0.22, 0.26), 9, grano=0.01)
    izq = 0.3 if grande else 0.08
    p.rect(pix, 0.3, 0.52, izq, 0.92, (0.93, 0.93, 0.90))
    p.rect(pix, 0.62, 0.74, izq, izq + (0.92 - izq) * 0.7, (0.72, 0.74, 0.78))
    if grande:
        p.rect(pix, 0.15, 0.85, 0.04, 0.22, (0.90, 0.42, 0.25))
        p.rect(pix, 0.35, 0.65, 0.09, 0.17, (0.20, 0.22, 0.26))
    o = c.caja("Letrero", (0, 0, 0), (ancho, 0.45, 0.04), p.mat_hoja("letrero_%s" % ("grande" if grande else "mediano"), pix, aspereza=0.4), bisel=0.004)
    c.uv_plano_frente(o)
    for dx in (-ancho / 2 + 0.08, ancho / 2 - 0.08):
        c.cilindro("Separador", (dx, 0.15, 0.02), 0.012, 0.03, _mat("cromo", (0.65, 0.66, 0.68), 0.25), eje="z", lados=10)


def pizarra_proyecto():
    """Pizarra blanca de la oficina abierta en el muro lateral (mira hacia +x), con el tablero del proyecto."""
    pix = p.hoja(220, 400, (0.97, 0.97, 0.96), 4, grano=0.005)
    azul = (0.20, 0.32, 0.65)
    for k in range(3):
        x0 = 0.04 + k * 0.32
        p.rect(pix, 0.06, 0.1, x0, x0 + 0.25, azul)
        p.rect(pix, 0.12, 0.95, x0 + 0.3, x0 + 0.302, (0.6, 0.62, 0.7))
        for j in range(2 + k % 2):
            color = [(0.98, 0.88, 0.35), (0.55, 0.82, 0.95), (0.95, 0.60, 0.70)][(k + j) % 3]
            y0 = 0.16 + j * 0.24
            p.rect(pix, y0, y0 + 0.19, x0 + 0.02, x0 + 0.24, color)
            p.renglones(pix, y0 + 0.04, y0 + 0.16, x0 + 0.04, x0 + 0.22, (0.2, 0.2, 0.25), k * 10 + j, paso=0.05, grueso=0.3, mano=True)
    o = c.caja("Pizarra", (0.02, 1.5, 0), (0.015, 1.1, 2.0), p.mat_hoja("pizarra_proyecto", pix, aspereza=0.2))
    p.uv_plano_lado(o)
    alu = _mat("marco_pizarra_oficina", (0.75, 0.76, 0.78), 0.3)
    for dy in (-0.56, 0.56):
        c.caja("Marco", (0.025, 1.5 + dy, 0), (0.03, 0.025, 2.05), alu)
    for dz in (-1.01, 1.01):
        c.caja("Marco", (0.025, 1.5, dz), (0.03, 1.14, 0.025), alu)
    c.caja("Bandeja", (0.06, 0.93, 0), (0.07, 0.015, 1.6), alu)


def puf():
    """Puf de tela (bean bag) naranjo, hundido por el uso."""
    tela = c.material("tela_puf", (1, 1, 1), aspereza=0.9, textura=c.textura_tela("tela_puf", (0.86, 0.50, 0.28), semilla=61))
    o = c.esfera("Puf", (0, 0.24, 0), 0.38, tela, escala=(1.0, 0.65, 1.0), lados=28)
    c.uv_cubo(o, escala=0.6)
    o2 = c.esfera("Respaldo del puf", (0, 0.42, 0.2), 0.26, tela, escala=(1.1, 0.8, 0.6), lados=24)
    c.uv_cubo(o2, escala=0.6)


def tela_panel():
    return c.material("tela_panel", (1, 1, 1), aspereza=0.95, textura=c.textura_tela("tela_panel", (0.52, 0.59, 0.67), semilla=71))


def sillon(mirando, color, semilla):
    """Sillón de consulta de tela (0,90 x 0,70 x 0,90). `mirando` = +1 si quien se sienta mira hacia +x."""
    tela = c.material("tela_sillon_%d" % semilla, (1, 1, 1), aspereza=0.9, textura=c.textura_tela("tela_sillon_%d" % semilla, color, semilla=semilla))
    pata = _mat("pata_sillon", (0.30, 0.20, 0.13), 0.5)
    c.uv_cubo(c.caja("Base", (0, 0.24, 0), (0.86, 0.22, 0.86), tela, bisel=0.03, segmentos=3), escala=0.6)
    c.uv_cubo(c.caja("Cojin", (mirando * 0.05, 0.4, 0), (0.7, 0.12, 0.66), tela, bisel=0.04, segmentos=3), escala=0.6)
    c.uv_cubo(c.caja("Respaldo", (-mirando * 0.36, 0.55, 0), (0.16, 0.6, 0.86), tela, bisel=0.04, segmentos=3), escala=0.6)
    for dz in (-0.38, 0.38):
        c.uv_cubo(c.caja("Brazo", (0, 0.45, dz), (0.86, 0.22, 0.12), tela, bisel=0.04, segmentos=3), escala=0.6)
    for dx in (-0.36, 0.36):
        for dz in (-0.36, 0.36):
            c.cilindro("Pata", (dx, 0.06, dz), 0.022, 0.12, pata, lados=10)
