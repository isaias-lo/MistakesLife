"""Los lugares de los oficios (OfficeBuilder.cs): local de atención, tienda, restorán y obra.

Medidas en coordenadas de Unity desde el ancla de cada mueble, igual que la forma simple. Sin
amarillo en los productos: ese color es solo de la marca del jugador.
"""

import math

import numpy as np

import comun as c
import oficinas as o
import papeles as p


def _mat(nombre, color, aspereza=0.5):
    return c.material(nombre, color, aspereza=aspereza)


def textura_ladrillo(nombre, color=(0.66, 0.33, 0.22), junta=(0.78, 0.75, 0.70), semilla=5):
    """Ladrillo princesa a la vista: hiladas trabadas con su junta de mortero."""
    alto, ancho = 256, 256
    rng = np.random.default_rng(semilla)
    pix = np.ones((alto, ancho, 4))
    pix[..., :3] = junta
    fila = 32
    largo = 64
    for f in range(alto // fila):
        corrido = (largo // 2) * (f % 2)
        for k in range(-1, ancho // largo + 1):
            x0 = k * largo + corrido + 3
            x1 = x0 + largo - 6
            tono = 0.88 + rng.random() * 0.2
            pix[f * fila + 3:(f + 1) * fila - 3, max(x0, 0):max(min(x1, ancho), 0), :3] = np.array(color) * tono
    pix[..., :3] *= 1 - 0.06 * rng.random((alto, ancho, 1))
    return c.imagen(nombre, pix)


# ------------------------------------------------------------------ local de atención

def mostrador_local():
    """Mesón de atención de 4,00 m: frente de melamina con franja roja y la caja con su pantalla."""
    blanco = _mat("melamina_meson", (0.90, 0.89, 0.86), 0.45)
    rojo = _mat("franja_local", (0.72, 0.24, 0.22), 0.45)
    cubierta = _mat("cubierta_meson", (0.30, 0.31, 0.33), 0.35)
    c.caja("Meson", (0, 0.47, 0), (4.0, 0.94, 0.7), blanco, bisel=0.006)
    c.caja("Franja", (0, 0.62, -0.352), (4.0, 0.16, 0.006), rojo)
    c.caja("Zocalo", (0, 0.05, -0.3), (3.96, 0.1, 0.12), _mat("zocalo", (0.25, 0.25, 0.27), 0.5))
    c.caja("Cubierta", (0, 0.96, 0), (4.06, 0.04, 0.8), cubierta, bisel=0.004)
    # Caja: pantalla táctil hacia quien atiende y el lector de tarjetas hacia el cliente.
    negro = _mat("caja_pos", (0.14, 0.14, 0.16), 0.35)
    c.caja("Base de la caja", (1.2, 1.0, 0.1), (0.36, 0.04, 0.3), negro, bisel=0.004)
    pan = c.caja("Pantalla de la caja", (1.2, 1.17, 0.12), (0.34, 0.24, 0.02), negro, bisel=0.004)
    c.girar_x(pan, -15)
    c.caja("Gaveta", (1.2, 0.9, 0.2), (0.42, 0.1, 0.4), negro, bisel=0.004)
    c.caja("Lector de tarjetas", (0.85, 1.02, -0.2), (0.08, 0.04, 0.15), negro, bisel=0.006)
    c.caja("Dispensador de numeros", (-1.6, 1.08, -0.3), (0.12, 0.2, 0.1), rojo, bisel=0.01)


def estantes_local():
    """Góndola de 5,00 m contra el muro del fondo, tres repisas con productos de almacén."""
    metal = _mat("gondola", (0.85, 0.85, 0.84), 0.4)
    c.caja("Fondo de la gondola", (0, 1.1, 0.18), (5.0, 2.2, 0.03), metal)
    for dx in (-2.48, 0, 2.48):
        c.caja("Paral", (dx, 1.1, 0.1), (0.04, 2.2, 0.18), metal)
    rng = np.random.default_rng(11)
    colores = [(0.85, 0.35, 0.45), (0.35, 0.60, 0.85), (0.55, 0.75, 0.40), (0.92, 0.92, 0.90), (0.75, 0.30, 0.20), (0.30, 0.35, 0.55)]
    for k in range(3):
        y = 0.5 + k * 0.7
        c.caja("Repisa", (0, y, 0), (5.0, 0.03, 0.4), metal, bisel=0.002)
        c.caja("Portaprecios", (0, y - 0.02, -0.2), (5.0, 0.04, 0.01), _mat("portaprecios", (0.95, 0.95, 0.93), 0.6))
        x = -2.4
        while x < 2.3:
            tipo = int(rng.integers(3))
            col = colores[int(rng.integers(len(colores)))]
            mat = _mat("producto_%d_%d" % (tipo, colores.index(col)), col, 0.45)
            if tipo == 0:  # cajas (cereal, té, galletas)
                h = 0.22 + rng.random() * 0.1
                for j in range(3):
                    c.caja("Caja", (x + 0.06, y + 0.015 + h / 2, -0.1 + j * 0.1), (0.12, h, 0.07), mat, bisel=0.003)
                x += 0.14
            elif tipo == 1:  # botellas
                for j in range(2):
                    c.cilindro("Botella", (x + 0.04, y + 0.13, -0.1 + j * 0.12), 0.035, 0.24, mat, lados=12)
                    c.cilindro("Tapa", (x + 0.04, y + 0.265, -0.1 + j * 0.12), 0.014, 0.03, _mat("tapa_botella", (0.9, 0.9, 0.9), 0.4), lados=8)
                x += 0.09
            else:  # tarros
                for j in range(3):
                    c.cilindro("Tarro", (x + 0.04, y + 0.07, -0.1 + j * 0.1), 0.04, 0.11, mat, lados=14)
                x += 0.1
            x += 0.02


# ------------------------------------------------------------------ tienda de ropa

def vitrina():
    """La vitrina a la calle: vidrio de 4,60 x 1,80, un maniquí de busto con ropa de la temporada
    y el cartel de la liquidación."""
    alu = _mat("marco_vitrina", (0.24, 0.25, 0.28), 0.35)
    c.caja("Vidrio de la vitrina", (0, 1.2, 0), (4.6, 1.8, 0.02), _mat("vidrio_vitrina", (0.66, 0.80, 0.88), 0.05))
    c.caja("Marco superior", (0, 2.15, 0), (4.7, 0.1, 0.12), alu, bisel=0.004)
    c.caja("Marco inferior", (0, 0.28, 0), (4.7, 0.1, 0.12), alu, bisel=0.004)
    for dx in (-2.33, 0, 2.33):
        c.caja("Montante", (dx, 1.2, 0), (0.06, 1.9, 0.1), alu)
    c.caja("Tarima", (0, 0.12, -0.35), (4.6, 0.24, 0.6), _mat("tarima_vitrina", (0.88, 0.86, 0.82), 0.5), bisel=0.004)
    # Maniquí de busto (sin cabeza), sobre su pie.
    ropa = c.material("ropa_maniqui", (1, 1, 1), aspereza=0.9, textura=c.textura_tela("ropa_maniqui", (0.32, 0.45, 0.62), semilla=12))
    pie = _mat("pie_maniqui", (0.20, 0.20, 0.22), 0.3)
    mx, mz = -1.2, -0.3
    c.cilindro("Base", (mx, 0.25, mz), 0.15, 0.02, pie, lados=20)
    c.cilindro("Pie", (mx, 0.6, mz), 0.012, 0.7, pie, lados=8)
    torso = c.esfera("Torso", (mx, 1.2, mz), 0.2, ropa, escala=(1.0, 1.6, 0.6), lados=24)
    c.uv_cubo(torso, escala=0.5)
    c.esfera("Hombros", (mx, 1.45, mz), 0.2, ropa, escala=(1.15, 0.45, 0.6), lados=20)
    c.cilindro("Cuello", (mx, 1.58, mz), 0.045, 0.1, _mat("cuello_maniqui", (0.88, 0.86, 0.82), 0.4), lados=12)
    # Cartel de liquidación colgado detrás del vidrio.
    pix = p.hoja(120, 210, (0.82, 0.20, 0.20), 3, grano=0.0)
    p.renglones(pix, 0.12, 0.3, 0.15, 0.85, (0.98, 0.96, 0.95), 3, paso=0.12, grueso=0.7)
    p.numero(pix, "30", 0.42, 0.28, 0.42, (0.98, 0.96, 0.95))
    cartel = c.caja("Cartel de oferta", (1.4, 1.5, -0.04), (0.8, 0.45, 0.01), p.mat_hoja("cartel_oferta", pix, aspereza=0.5))
    c.uv_plano_frente(cartel)
    for dx in (-0.3, 0.3):
        c.cilindro("Hilo", (1.4 + dx, 1.9, -0.04), 0.002, 0.36, _mat("hilo", (0.2, 0.2, 0.2), 0.5), lados=6)


def percheros():
    """Dos percheros cromados con ropa colgada en ganchos (poleras, camisas, chaquetas)."""
    cromo = _mat("cromo_perchero", (0.70, 0.71, 0.73), 0.2)
    colores = [(0.20, 0.30, 0.50), (0.80, 0.80, 0.78), (0.55, 0.25, 0.30), (0.30, 0.42, 0.34), (0.15, 0.15, 0.17)]
    for rack in range(2):
        z = rack * -1.1
        c.cilindro("Barra", (0, 1.25, z), 0.015, 1.6, cromo, eje="x", lados=12)
        for dx in (-0.78, 0.78):
            c.cilindro("Pata", (dx, 0.63, z), 0.015, 1.24, cromo, lados=10)
            c.caja("Base", (dx, 0.015, z), (0.06, 0.03, 0.5), cromo, bisel=0.008)
        for k in range(7):
            x = -0.6 + k * 0.2
            col = colores[(k + rack * 2) % len(colores)]
            tela = c.material("ropa_%d_%d" % (rack, k), (1, 1, 1), aspereza=0.9, textura=c.textura_tela("ropa_%d_%d" % (rack, k), col, semilla=20 + k + rack * 7))
            largo = 0.55 + (k % 3) * 0.08
            cuerpo = c.caja("Prenda", (x, 1.2 - largo / 2, z), (0.03, largo, 0.42), tela, bisel=0.008)
            c.uv_cubo(cuerpo, escala=0.5)
            for lado in (-1, 1):
                m = c.caja("Manga", (x, 1.12, z + lado * 0.24), (0.025, 0.3, 0.08), tela, bisel=0.006)
                c.girar_x(m, lado * 12)
            c.caja("Gancho", (x, 1.205, z), (0.008, 0.012, 0.4), _mat("gancho_ropa", (0.15, 0.15, 0.16), 0.4))
            c.cilindro("Gancho", (x, 1.24, z), 0.004, 0.05, cromo, lados=6)


def caja_tienda():
    """La caja de la tienda: mesón blanco con la caja registradora, el lector y las bolsas."""
    blanco = _mat("meson_tienda", (0.90, 0.89, 0.86), 0.4)
    c.caja("Meson", (0, 0.5, 0), (1.6, 1.0, 0.6), blanco, bisel=0.006)
    c.caja("Cubierta", (0, 1.015, 0), (1.66, 0.03, 0.66), o.madera("cubierta_tienda", ((0.62, 0.46, 0.32), (0.52, 0.38, 0.26)), 13), bisel=0.004)
    negro = _mat("caja_pos", (0.14, 0.14, 0.16), 0.35)
    c.caja("Base de la caja", (0.3, 1.05, 0.1), (0.36, 0.04, 0.3), negro, bisel=0.004)
    pan = c.caja("Pantalla de la caja", (0.3, 1.2, 0.12), (0.34, 0.24, 0.02), negro, bisel=0.004)
    c.girar_x(pan, -15)
    c.caja("Lector de tarjetas", (0.0, 1.05, -0.15), (0.08, 0.04, 0.15), negro, bisel=0.006)
    bolsa = _mat("bolsa_papel", (0.80, 0.68, 0.50), 0.8)
    for k in range(3):
        c.caja("Bolsa doblada", (-0.45, 1.035 + k * 0.006, 0), (0.32, 0.005, 0.24), bolsa)
    c.caja("Rollo de boletas", (-0.1, 1.06, 0.15), (0.1, 0.06, 0.08), _mat("boleta", (0.97, 0.97, 0.96), 0.6), bisel=0.01)


# ------------------------------------------------------------------ restorán

def barra():
    """La barra del restorán: frente de madera, cubierta oscura, botellas y la ventanilla de la cocina."""
    madera = o.madera("barra_madera", ((0.52, 0.36, 0.24), (0.42, 0.28, 0.18)), 14)
    oscura = _mat("cubierta_barra", (0.22, 0.20, 0.19), 0.3)
    c.uv_cubo(c.caja("Barra", (0, 0.55, 0), (2.8, 1.1, 0.6), madera, bisel=0.006), escala=1.6)
    c.caja("Cubierta de la barra", (0, 1.125, -0.05), (2.9, 0.05, 0.72), oscura, bisel=0.006)
    c.caja("Apoyapies", (0, 0.2, -0.36), (2.7, 0.03, 0.03), _mat("cromo", (0.65, 0.66, 0.68), 0.25))
    # Ventanilla de la cocina en el muro, con luz cálida y el pedido colgado.
    c.caja("Ventanilla", (0.4, 1.7, 0.6), (1.4, 0.6, 0.04), _mat("cocina_luz", (0.95, 0.80, 0.55), 0.6))
    c.caja("Marco ventanilla", (0.4, 2.02, 0.58), (1.5, 0.05, 0.06), _mat("acero", (0.75, 0.76, 0.78), 0.3))
    c.caja("Repisa ventanilla", (0.4, 1.4, 0.5), (1.5, 0.03, 0.25), _mat("acero", (0.75, 0.76, 0.78), 0.3))
    for k in range(3):
        c.caja("Comanda", (0.0 + k * 0.25, 1.93, 0.57), (0.08, 0.12, 0.002), _mat("comanda", (0.97, 0.97, 0.95), 0.8))
    vidrio_verde = _mat("botella_verde", (0.22, 0.40, 0.25), 0.1)
    vidrio_cafe = _mat("botella_cafe", (0.45, 0.22, 0.12), 0.1)
    for i in range(5):
        mat = vidrio_verde if i % 2 == 0 else vidrio_cafe
        x = -1.2 + i * 0.12
        c.cilindro("Botella", (x, 1.25, 0.15), 0.035, 0.2, mat, lados=12)
        c.cilindro("Cuello", (x, 1.39, 0.15), 0.013, 0.08, mat, lados=8)
    for i in range(4):
        c.cilindro("Vaso", (0.9 + i * 0.12, 1.2, -0.15), 0.035, 0.1, _mat("vaso", (0.82, 0.88, 0.92), 0.05), lados=12)


def mesa_restoran():
    """Mesa de restorán con mantel, dos sillas de madera, platos, copas y servilletas."""
    madera = o.madera("silla_restoran", ((0.52, 0.36, 0.24), (0.42, 0.28, 0.18)), 15)
    mantel = c.material("mantel", (1, 1, 1), aspereza=0.9, textura=c.textura_tela("mantel", (0.95, 0.94, 0.90), semilla=16))
    c.uv_cubo(c.caja("Mantel", (0, 0.735, 0), (1.1, 0.03, 1.1), mantel, bisel=0.008), escala=0.6)
    for dx in (-0.55, 0.55):
        c.uv_cubo(c.caja("Caida del mantel", (dx, 0.62, 0), (0.01, 0.22, 1.1), mantel), escala=0.6)
    for dz in (-0.55, 0.55):
        c.uv_cubo(c.caja("Caida del mantel", (0, 0.62, dz), (1.1, 0.22, 0.01), mantel), escala=0.6)
    c.cilindro("Pie de la mesa", (0, 0.36, 0), 0.04, 0.7, madera, lados=12)
    c.cilindro("Base de la mesa", (0, 0.02, 0), 0.25, 0.04, madera, lados=20)
    for lado in (-1, 1):
        sx = lado * 0.8
        c.uv_cubo(c.caja("Asiento", (sx, 0.45, 0), (0.44, 0.04, 0.44), madera, bisel=0.006), escala=1.0)
        c.uv_cubo(c.caja("Respaldo", (sx + lado * 0.2, 0.75, 0), (0.04, 0.5, 0.42), madera, bisel=0.006), escala=1.0)
        for dx in (-0.18, 0.18):
            for dz in (-0.18, 0.18):
                c.caja("Pata", (sx + dx, 0.22, dz), (0.035, 0.44, 0.035), madera)
        px = lado * 0.27
        c.cilindro("Plato", (px, 0.757, 0), 0.13, 0.015, _mat("loza", (0.97, 0.97, 0.96), 0.25), lados=24)
        c.cilindro("Copa", (px - lado * 0.05, 0.8, 0.2), 0.03, 0.09, _mat("vaso", (0.82, 0.88, 0.92), 0.05), lados=12)
        c.caja("Servilleta", (px - lado * 0.02, 0.755, -0.2), (0.12, 0.01, 0.12), _mat("servilleta", (0.72, 0.18, 0.18), 0.8))
        for dz in (-0.16, 0.16):
            c.caja("Cubierto", (px, 0.753, dz), (0.17, 0.004, 0.014), _mat("cromo", (0.65, 0.66, 0.68), 0.25))
    c.cilindro("Salero", (0, 0.78, 0.35), 0.015, 0.06, _mat("vaso", (0.82, 0.88, 0.92), 0.05), lados=10)


# ------------------------------------------------------------------ obra

def andamio():
    """El muro en obra con su andamio de tubos, tablones y la malla verde de seguridad."""
    ladrillo = c.material("ladrillo_obra", (1, 1, 1), aspereza=0.9, textura=textura_ladrillo("ladrillo_obra"))
    muro = c.caja("Muro en obra", (0, 1.2, 0.5), (3.2, 2.4, 0.15), ladrillo)
    c.uv_cubo(muro, escala=0.9)
    hormigon = _mat("hormigon", (0.68, 0.67, 0.64), 0.9)
    for dx in (-1.6, 1.6):
        c.caja("Pilar de hormigon", (dx, 1.25, 0.5), (0.22, 2.5, 0.2), hormigon)
    c.caja("Cadena", (0, 2.5, 0.5), (3.4, 0.2, 0.2), hormigon)
    tubo = _mat("tubo_andamio", (0.55, 0.57, 0.60), 0.35)
    for x in (-1.4, 0.0, 1.4):
        for z in (0.0, 0.3):
            c.cilindro("Pilar del andamio", (x, 1.1, z), 0.024, 2.2, tubo, lados=10)
        c.caja("Base", (x, 0.01, 0.15), (0.15, 0.02, 0.45), tubo)
    for y in (0.6, 1.2, 1.9):
        for z in (0.0, 0.3):
            c.cilindro("Larguero", (0, y, z), 0.02, 3.0, tubo, eje="x", lados=8)
    for i in range(2):
        d = c.cilindro("Diagonal", (-0.7 + i * 1.4, 0.9, 0.0), 0.018, 1.6, tubo, lados=8)
        c.girar_z(d, 50 if i == 0 else -50)
    tablon = o.madera("tablon", ((0.72, 0.58, 0.38), (0.62, 0.48, 0.30)), 17)
    for dz in (0.06, 0.24):
        c.uv_cubo(c.caja("Tablon", (0, 1.23, dz), (3.0, 0.04, 0.17), tablon, bisel=0.004), escala=1.6)
    malla = _mat("malla_seguridad", (0.28, 0.55, 0.32), 0.9)
    c.caja("Malla de seguridad", (0, 1.8, -0.05), (3.0, 0.9, 0.01), malla)
    c.caja("Balde", (0.6, 1.33, 0.15), (0.2, 0.16, 0.2), _mat("balde", (0.15, 0.15, 0.16), 0.6), bisel=0.02)


SENTIDO_Z = 1  # si la boca no sigue al tambor, se cambia a -1


def betonera():
    """La betonera naranja con su tambor inclinado, el motor y dos sacos de cemento."""
    naranjo = _mat("betonera", (0.88, 0.45, 0.18), 0.4)
    fierro = _mat("fierro_betonera", (0.35, 0.36, 0.38), 0.4)
    # El tambor va inclinado 30 grados: la boca sigue su eje.
    tambor = c.cilindro("Tambor", (0, 0.85, 0), 0.38, 0.5, naranjo, lados=24)
    c.girar_z(tambor, 30)
    eje = (-math.sin(math.radians(30)) * SENTIDO_Z, math.cos(math.radians(30)))
    boca = c.cilindro("Boca", (eje[0] * 0.36, 0.85 + eje[1] * 0.36, 0), 0.25, 0.22, naranjo, lados=20)
    c.girar_z(boca, 30)
    fondo = c.cilindro("Fondo", (-eje[0] * 0.27, 0.85 - eje[1] * 0.27, 0), 0.3, 0.06, fierro, lados=20)
    c.girar_z(fondo, 30)
    c.caja("Soporte", (0, 0.45, 0), (0.12, 0.5, 0.7), fierro)
    c.caja("Motor", (-0.35, 0.5, 0), (0.3, 0.25, 0.3), _mat("motor", (0.25, 0.42, 0.62), 0.4), bisel=0.02)
    c.caja("Chasis", (0, 0.2, 0), (0.9, 0.06, 0.6), fierro)
    for dz in (-0.33, 0.33):
        r = c.cilindro("Rueda", (0.3, 0.15, dz), 0.15, 0.08, _mat("rueda", (0.10, 0.10, 0.11), 0.8), eje="z", lados=20)
    c.caja("Pata", (-0.35, 0.1, 0), (0.05, 0.2, 0.05), fierro)
    saco = c.material("saco_cemento", (1, 1, 1), aspereza=0.95, textura=c.textura_tela("saco_cemento", (0.86, 0.83, 0.74), semilla=18))
    for k, (y, alto) in enumerate(((0.14, 0.28), (0.4, 0.24))):
        s = c.caja("Saco de cemento", (0.85, y, 0.2), (0.45, alto, 0.32), saco, bisel=0.05, segmentos=3)
        c.uv_cubo(s, escala=0.6)
        c.girar_y(s, 8 - k * 14)


def ladrillos():
    """Tres pallets de ladrillos y la carretilla azul."""
    ladrillo = c.material("ladrillo_pila", (1, 1, 1), aspereza=0.9, textura=textura_ladrillo("ladrillo_pila", semilla=7))
    pallet = o.madera("pallet", ((0.70, 0.58, 0.40), (0.60, 0.48, 0.32)), 19)
    for k in range(3):
        y = k * 0.22
        c.uv_cubo(c.caja("Pallet", (0, y + 0.03, 0), (0.95, 0.06, 0.65), pallet), escala=1.2)
        pila = c.caja("Ladrillos", (0, y + 0.14, 0), (0.9, 0.16, 0.6), ladrillo)
        c.uv_cubo(pila, escala=0.5)
    # Carretilla.
    azul = _mat("carretilla", (0.22, 0.38, 0.60), 0.4)
    fierro = _mat("fierro_carretilla", (0.30, 0.31, 0.33), 0.4)
    cx, cz = 0.9, 0.9
    c.caja("Tolva", (cx, 0.45, cz), (0.6, 0.24, 0.8), azul, bisel=0.04, segmentos=3, ahusado=0.7)
    c.cilindro("Rueda", (cx, 0.17, cz - 0.5), 0.17, 0.08, _mat("rueda", (0.10, 0.10, 0.11), 0.8), eje="x", lados=20)
    for dx in (-0.22, 0.22):
        c.cilindro("Mango", (cx + dx, 0.45, cz + 0.45), 0.017, 0.9, fierro, eje="z", lados=8)
        c.caja("Pata", (cx + dx, 0.18, cz + 0.25), (0.03, 0.36, 0.03), fierro)
