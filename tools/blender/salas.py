"""La sala del colegio (sala.*) y el aula de la u, el CFT o el instituto (aula.*).

Cada mueble va en su ancla de PlaceBuilder.cs, con las medidas de la forma simple. La sala es
cálida (madera, pizarrón verde de tiza, pupitre de fierro pintado); el aula, más sobria (melamina
gris, pizarra blanca, sillas negras). La clase mira hacia el muro de la pizarra (-x).
"""

import comun as c
import papeles as p

ESTILOS = {
    "sala": {
        "cubierta": ((0.80, 0.68, 0.50), (0.68, 0.55, 0.38)),
        "fierro": (0.24, 0.36, 0.55),
        "asiento": ((0.74, 0.60, 0.43), (0.62, 0.48, 0.33)),
        "marco_ventana": (0.90, 0.90, 0.88),
        "pizarra": (0.18, 0.32, 0.25),
        "marco_pizarra": (0.40, 0.27, 0.16),
        "escritorio": ((0.62, 0.46, 0.30), (0.50, 0.36, 0.23)),
    },
    "aula": {
        "cubierta": ((0.86, 0.85, 0.82), (0.80, 0.79, 0.76)),
        "fierro": (0.55, 0.56, 0.58),
        "asiento": ((0.16, 0.17, 0.19), (0.12, 0.13, 0.15)),
        "marco_ventana": (0.70, 0.71, 0.72),
        "pizarra": (0.96, 0.96, 0.95),
        "marco_pizarra": (0.75, 0.76, 0.78),
        "escritorio": ((0.84, 0.83, 0.80), (0.78, 0.77, 0.74)),
    },
}


def _madera(nombre, colores, semilla, aspereza=0.5):
    claro, oscuro = colores
    return c.material(nombre, (1, 1, 1), aspereza=aspereza, textura=c.textura_madera(nombre, claro=claro, oscuro=oscuro, semilla=semilla))


def _tubo(nombre, a, b, radio, mat):
    """Un tubo recto entre dos puntos alineados con un eje de Unity."""
    (x0, y0, z0), (x1, y1, z1) = a, b
    centro = ((x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2)
    if abs(y1 - y0) > 1e-6:
        return c.cilindro(nombre, centro, radio, abs(y1 - y0), mat, eje="y", lados=10)
    if abs(x1 - x0) > 1e-6:
        return c.cilindro(nombre, centro, radio, abs(x1 - x0), mat, eje="x", lados=10)
    return c.cilindro(nombre, centro, radio, abs(z1 - z0), mat, eje="z", lados=10)


def pupitre(estilo):
    """Mesa y silla de un puesto: la mesa en el ancla, la silla detrás (+x), mirando a la pizarra."""
    e = ESTILOS[estilo]
    cubierta = _madera("cubierta_" + estilo, e["cubierta"], 23, aspereza=0.45)
    fierro = c.material("fierro_" + estilo, e["fierro"], aspereza=0.4)
    goma = c.material("regaton", (0.1, 0.1, 0.1), aspereza=0.8)

    # Mesa: 0,70 de ancho (z) por 0,50 de fondo (x), cubierta a 0,72.
    alto, fondo, ancho = 0.72, 0.5, 0.7
    c.uv_cubo(c.caja("Cubierta del pupitre", (0, alto - 0.012, 0), (fondo, 0.024, ancho), cubierta, bisel=0.004), escala=1.2)
    for dx in (-fondo / 2 + 0.04, fondo / 2 - 0.04):
        for dz in (-ancho / 2 + 0.04, ancho / 2 - 0.04):
            _tubo("Pata", (dx, 0.01, dz), (dx, alto - 0.024, dz), 0.013, fierro)
            c.cilindro("Regaton", (dx, 0.006, dz), 0.015, 0.012, goma, lados=10)
    for dx in (-fondo / 2 + 0.04, fondo / 2 - 0.04):
        _tubo("Travesano", (dx, alto - 0.05, -ancho / 2 + 0.04), (dx, alto - 0.05, ancho / 2 - 0.04), 0.01, fierro)
    if estilo == "sala":
        # Parrilla para la mochila y los cuadernos bajo la cubierta.
        c.caja("Parrilla", (0, alto - 0.13, 0), (fondo - 0.1, 0.01, ancho - 0.1), fierro)
    else:
        c.caja("Faldon", (-fondo / 2 + 0.05, alto - 0.12, 0), (0.012, 0.18, ancho - 0.1), fierro)

    # Silla: asiento a 0,44, respaldo atrás.
    asiento = _madera("asiento_" + estilo, e["asiento"], 29, aspereza=0.4 if estilo == "sala" else 0.55)
    sx, sy, lado = 0.6, 0.44, 0.4
    c.uv_cubo(c.caja("Asiento", (sx, sy, 0), (lado, 0.02, lado), asiento, bisel=0.008), escala=1.0)
    respaldo = c.caja("Respaldo", (sx + lado / 2 - 0.02, sy + 0.3, 0), (0.016, 0.2, lado - 0.02), asiento, bisel=0.006)
    c.uv_cubo(respaldo, escala=1.0)
    c.girar_z(respaldo, -8)
    for dx in (-lado / 2 + 0.03, lado / 2 - 0.03):
        for dz in (-lado / 2 + 0.03, lado / 2 - 0.03):
            _tubo("Pata de la silla", (sx + dx, 0.01, dz), (sx + dx, sy - 0.01, dz), 0.011, fierro)
            c.cilindro("Regaton", (sx + dx, 0.006, dz), 0.013, 0.012, goma, lados=10)
    for dz in (-lado / 2 + 0.03, lado / 2 - 0.03):
        _tubo("Pata del respaldo", (sx + lado / 2 - 0.03, sy, dz), (sx + lado / 2 - 0.03, sy + 0.4, dz), 0.01, fierro)


def ventana(estilo):
    """Ventanal del muro del fondo (se ve desde -z): 3,60 x 1,00 con su centro a 1,70."""
    e = ESTILOS[estilo]
    marco = c.material("marco_ventanal_" + estilo, e["marco_ventana"], aspereza=0.4)
    vidrio = c.material("vidrio_ventanal", (0.62, 0.78, 0.90), aspereza=0.05)
    alfeizar = c.material("alfeizar_ventanal", (0.85, 0.83, 0.79), aspereza=0.6)
    y, ancho, alto = 1.7, 3.6, 1.0
    c.caja("Vidrio", (0, y, 0.0), (ancho, alto, 0.01), vidrio)
    hojas = 6 if estilo == "sala" else 4
    for i in range(hojas + 1):
        x = -ancho / 2 + i * ancho / hojas
        c.caja("Montante", (x, y, -0.015), (0.05 if estilo == "sala" else 0.04, alto + 0.04, 0.05), marco, bisel=0.003)
    for dy in (-alto / 2, alto / 2):
        c.caja("Marco horizontal", (0, y + dy, -0.015), (ancho + 0.06, 0.05, 0.05), marco, bisel=0.003)
    if estilo == "sala":
        # Las ventanas de colegio tienen un travesaño con la ventilación arriba.
        c.caja("Travesano", (0, y + alto / 2 - 0.25, -0.015), (ancho, 0.035, 0.045), marco, bisel=0.002)
    c.caja("Alfeizar", (0, y - alto / 2 - 0.045, -0.07), (ancho + 0.1, 0.03, 0.14), alfeizar, bisel=0.004)


def _pizarra_textura(estilo):
    if estilo == "sala":
        pix = p.hoja(220, 580, ESTILOS["sala"]["pizarra"], 5, grano=0.08)
        tiza = (0.80, 0.83, 0.80)
        # Restos borrados de tiza y la fecha arriba, como queda un pizarrón entre clases.
        p.renglones(pix, 0.08, 0.14, 0.04, 0.3, tiza, 5, paso=0.06, grueso=0.25, mano=True)
        borrado = pix[..., :3] * 0.88 + 0.12 * 0.75
        pix[90:180, 120:420, :3] = borrado[90:180, 120:420]
    else:
        pix = p.hoja(220, 580, ESTILOS["aula"]["pizarra"], 5, grano=0.01)
        plumon = (0.75, 0.78, 0.85)
        pix[60:160, 100:450, :3] = pix[60:160, 100:450, :3] * 0.97 + 0.03 * 0.8
        p.renglones(pix, 0.08, 0.14, 0.04, 0.25, plumon, 5, paso=0.06, grueso=0.2, mano=True)
    return pix


def pizarra(estilo):
    """Pizarrón verde (sala) o pizarra blanca (aula) en el muro de la izquierda, que mira hacia +x."""
    e = ESTILOS[estilo]
    marco = c.material("marco_pizarra_" + estilo, e["marco_pizarra"], aspereza=0.45)
    y, largo, alto = 1.55, 2.9, 1.1
    superficie = c.caja("Pizarra", (0.035, y, 0), (0.02, alto, largo), p.mat_hoja("pizarra_" + estilo, _pizarra_textura(estilo), aspereza=0.85 if estilo == "sala" else 0.2))
    p.uv_plano_lado(superficie)
    for dy in (-alto / 2 - 0.03, alto / 2 + 0.03):
        c.caja("Marco", (0.04, y + dy, 0), (0.05, 0.06, largo + 0.12), marco, bisel=0.004)
    for dz in (-largo / 2 - 0.03, largo / 2 + 0.03):
        c.caja("Marco", (0.04, y, dz), (0.05, alto + 0.12, 0.06), marco, bisel=0.004)
    c.caja("Bandeja", (0.09, y - alto / 2 - 0.06, 0), (0.1, 0.02, largo), marco, bisel=0.003)
    if estilo == "sala":
        tiza = c.material("tiza", (0.95, 0.95, 0.92), aspereza=0.9)
        for k, z in enumerate((-0.6, -0.52, 0.9)):
            c.cilindro("Tiza", (0.1, y - alto / 2 - 0.044, z), 0.005, 0.07, tiza, eje="z", lados=8)
        c.caja("Borrador", (0.1, y - alto / 2 - 0.035, 0.3), (0.05, 0.03, 0.13), c.material("borrador", (0.40, 0.27, 0.16), aspereza=0.6), bisel=0.004)
    else:
        for z, color in ((-0.7, (0.15, 0.25, 0.6)), (-0.6, (0.7, 0.15, 0.15)), (-0.5, (0.1, 0.1, 0.12))):
            c.cilindro("Plumon", (0.1, y - alto / 2 - 0.038, z), 0.009, 0.13, c.material("plumon_%d" % int(z * 10), color, aspereza=0.4), eje="x", lados=10)
        c.caja("Borrador", (0.1, y - alto / 2 - 0.035, 0.3), (0.06, 0.03, 0.14), c.material("borrador_aula", (0.2, 0.2, 0.22), aspereza=0.6), bisel=0.004)


def escritorio_profe(estilo):
    """El escritorio de quien hace clases, de frente al curso (+x), con su silla y sus cosas."""
    e = ESTILOS[estilo]
    madera = _madera("escritorio_profe_" + estilo, e["escritorio"], 31)
    fierro = c.material("fierro_profe_" + estilo, ESTILOS[estilo]["fierro"], aspereza=0.4)
    alto, fondo, ancho = 0.76, 0.7, 1.4
    c.uv_cubo(c.caja("Cubierta", (0, alto - 0.015, 0), (fondo, 0.03, ancho), madera, bisel=0.005), escala=1.4)
    c.uv_cubo(c.caja("Frente", (fondo / 2 - 0.02, alto / 2, 0), (0.02, alto - 0.03, ancho - 0.04), madera, bisel=0.003), escala=1.4)
    for dz in (-ancho / 2 + 0.02, ancho / 2 - 0.02):
        c.uv_cubo(c.caja("Lateral", (0, alto / 2, dz), (fondo - 0.04, alto - 0.03, 0.025), madera, bisel=0.003), escala=1.4)
    c.caja("Cajonera", (-0.05, alto - 0.12, ancho / 2 - 0.25), (fondo - 0.16, 0.16, 0.4), madera, bisel=0.003)

    # Silla de la profe detrás (-x).
    negro = c.material("silla_profe", (0.15, 0.15, 0.17), aspereza=0.6)
    c.caja("Asiento", (-0.55, 0.46, 0), (0.44, 0.05, 0.44), negro, bisel=0.01)
    c.caja("Respaldo", (-0.76, 0.75, 0), (0.04, 0.45, 0.42), negro, bisel=0.01)
    _tubo("Pata", (-0.55, 0.03, 0), (-0.55, 0.44, 0), 0.02, fierro)
    for dz in (-0.2, 0.2):
        c.caja("Base", (-0.55, 0.02, dz / 2), (0.5, 0.025, 0.04), fierro)

    # Encima: el libro de clases, un par de cuadernos y un tazón.
    if estilo == "sala":
        c.caja("Libro de clases", (0.05, alto + 0.02, -0.2), (0.32, 0.04, 0.24), c.material("libro_clases", (0.55, 0.15, 0.14), aspereza=0.5), bisel=0.003)
    else:
        c.caja("Notebook", (0.0, alto + 0.01, -0.2), (0.24, 0.02, 0.34), c.material("notebook_profe", (0.25, 0.26, 0.30), aspereza=0.35), bisel=0.004)
    c.caja("Cuadernos", (0.05, alto + 0.012, 0.25), (0.22, 0.024, 0.3), c.material("cuadernos_profe", (0.85, 0.82, 0.74), aspereza=0.7), bisel=0.002)
    c.cilindro("Tazon", (-0.15, alto + 0.05, 0.45), 0.04, 0.1, c.material("tazon_profe", (0.9, 0.88, 0.84), aspereza=0.3), lados=18)
