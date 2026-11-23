"""La frazada doblada sobre el sillón (escena.casa.frazada): lana roja con una franja crema."""

import comun as c


def armar():
    lana = c.material("frazada_roja", (1, 1, 1), aspereza=1.0, textura=c.textura_tela("tela_frazada_roja", (0.72, 0.33, 0.28), semilla=93, rayas=((0.92, 0.82, 0.58), 60)))
    f = c.caja("Frazada", (0.0, 0.62, 0.15), (0.6, 0.07, 0.52), lana, bisel=0.025, segmentos=3)
    c.uv_cubo(f, escala=0.4)
