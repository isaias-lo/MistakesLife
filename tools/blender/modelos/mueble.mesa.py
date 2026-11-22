"""La mesa del comedor (mueble.mesa): mesa de madera de cuatro patas, como las de un comedor
de departamento en Santiago. Mismo tamaño que la forma simple (1,40 x 0,90 m de cubierta, con
la cara de arriba a 0,44 del piso de la maqueta), para que todo lo que se pone encima calce igual.
"""

import comun as c

ALTO_CUBIERTA = 0.44  # cara de arriba, igual que la forma simple de RoomBuilder.cs
GROSOR = 0.035
LARGO, ANCHO = 1.4, 0.9


def armar():
    veta = c.textura_madera("veta_mesa", claro=(0.66, 0.49, 0.33), oscuro=(0.50, 0.35, 0.22))
    madera = c.material("madera_mesa", (1, 1, 1), aspereza=0.55, textura=veta)
    veta_parada = c.textura_madera("veta_patas", claro=(0.66, 0.49, 0.33), oscuro=(0.50, 0.35, 0.22), vertical=True)
    madera_patas = c.material("madera_mesa_patas", (1, 1, 1), aspereza=0.55, textura=veta_parada)

    cubierta = c.caja("Cubierta", (0, ALTO_CUBIERTA - GROSOR / 2, 0), (LARGO, GROSOR, ANCHO), madera, bisel=0.008, segmentos=3)
    c.uv_cubo(cubierta, escala=1.6)

    # Faldón: las tablas bajo la cubierta que amarran las patas.
    alto_falda = 0.06
    y_falda = ALTO_CUBIERTA - GROSOR - alto_falda / 2
    entrada = 0.07
    for z in (-(ANCHO / 2 - entrada), ANCHO / 2 - entrada):
        c.uv_cubo(c.caja("Faldon largo", (0, y_falda, z), (LARGO - 2 * entrada, alto_falda, 0.02), madera, bisel=0.003), escala=1.6)
    for x in (-(LARGO / 2 - entrada), LARGO / 2 - entrada):
        c.uv_cubo(c.caja("Faldon corto", (x, y_falda, 0), (0.02, alto_falda, ANCHO - 2 * entrada), madera, bisel=0.003), escala=1.6)

    # Cuatro patas que se afinan hacia el piso.
    alto_pata = ALTO_CUBIERTA - GROSOR
    for x in (-(LARGO / 2 - entrada), LARGO / 2 - entrada):
        for z in (-(ANCHO / 2 - entrada), ANCHO / 2 - entrada):
            c.uv_cubo(c.caja("Pata", (x, alto_pata / 2, z), (0.05, alto_pata, 0.05), madera_patas, bisel=0.006, ahusado=0.7), escala=1.6)
