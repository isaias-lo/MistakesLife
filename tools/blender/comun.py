"""Herramientas comunes para armar modelos de MistakesLife en Blender, solo con código.

Cada modelo vive en tools/blender/modelos/<id>.py y tiene una función `armar()` que
construye el objeto en una escena vacía. `armar_modelos.py` corre todos y exporta cada uno a
unity/Assets/MistakesLife/Resources/Modelos/<id>.fbx: el juego usa ese archivo en vez de la
forma simple de reserva (ver unity/Assets/MistakesLife/Scripts/Models.cs).

Medidas: todo se escribe en coordenadas de Unity (x a la derecha, y hacia arriba, z hacia el
fondo), en metros de la maqueta y medido desde el ancla del objeto, igual que las formas simples
de SceneObjectBuilder.cs. Así una posición se copia tal cual desde el código de Unity.
"""

import math
import os

import bpy  # bpy primero: trae bmesh
import bmesh  # noqa: E402
import numpy as np


def unity(x, y, z):
    """Pasa una posición de Unity a Blender (Unity da vuelta el eje x al importar un .fbx)."""
    return (-x, -z, y)


def escena_vacia():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.scene.unit_settings.system = "METRIC"


# ---------------------------------------------------------------- materiales y texturas

def imagen(nombre, pixeles):
    """Crea una imagen de Blender desde un arreglo (alto, ancho, 4) con valores 0..1."""
    alto, ancho, _ = pixeles.shape
    img = bpy.data.images.new(nombre, width=ancho, height=alto, alpha=True)
    img.pixels.foreach_set(pixeles.astype(np.float32).ravel())
    return img


def material(nombre, color, aspereza=0.7, textura=None):
    """Material simple: un color, o una textura si se da. Unity lo lee como material Standard."""
    mat = bpy.data.materials.new(nombre)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = (*color, 1.0)
    bsdf.inputs["Roughness"].default_value = aspereza
    bsdf.inputs["Metallic"].default_value = 0.0
    if textura is not None:
        nodo = mat.node_tree.nodes.new("ShaderNodeTexImage")
        nodo.image = textura
        mat.node_tree.links.new(nodo.outputs["Color"], bsdf.inputs["Base Color"])
    return mat


def _ruido(alto, ancho, escala, semilla, escala_x=None):
    """Ruido suave y repetible (sin azar suelto: siempre la misma semilla, el mismo dibujo).

    `escala` es el tamaño de las manchas en pixeles; `escala_x` las estira a lo ancho (fibras).
    """
    rng = np.random.default_rng(semilla)
    escala_x = escala_x or escala
    chico = rng.random((max(2, alto // escala + 2), max(2, ancho // escala_x + 2)))
    ys = np.linspace(0, chico.shape[0] - 1.001, alto)
    xs = np.linspace(0, chico.shape[1] - 1.001, ancho)
    y0 = ys.astype(int)
    x0 = xs.astype(int)
    fy = (ys - y0)[:, None]
    fx = (xs - x0)[None, :]
    fy = fy * fy * (3 - 2 * fy)
    fx = fx * fx * (3 - 2 * fx)
    a = chico[y0][:, x0]
    b = chico[y0][:, x0 + 1]
    c = chico[y0 + 1][:, x0]
    d = chico[y0 + 1][:, x0 + 1]
    return (a * (1 - fx) + b * fx) * (1 - fy) + (c * (1 - fx) + d * fx) * fy


def textura_madera(nombre, claro, oscuro, semilla=7, tamano=512, vertical=False):
    """Veta de madera: anillos estirados a lo largo, torcidos por ruido. Sutil, no de caricatura.

    vertical=True gira la veta para piezas paradas (patas).
    """
    alto = ancho = tamano
    y = np.linspace(0, 1, alto)[:, None]
    # La veta corre a lo largo de la imagen (eje x): anillos finos apenas torcidos.
    torcido = _ruido(alto, ancho, 48, semilla, escala_x=256) * 3.0 + _ruido(alto, ancho, 8, semilla + 1, escala_x=96) * 0.4
    anillos = np.sin((y * 70.0 + torcido) * math.pi) * 0.5 + 0.5
    anillos = anillos ** 4
    fibras = _ruido(alto, ancho, 1, semilla + 2, escala_x=24) * 0.25
    manchas = _ruido(alto, ancho, 96, semilla + 3, escala_x=256) * 0.3
    mezcla = np.clip(anillos * 0.45 + fibras + manchas, 0, 1)
    claro = np.array(claro)
    oscuro = np.array(oscuro)
    rgb = claro[None, None, :] * (1 - mezcla[..., None]) + oscuro[None, None, :] * mezcla[..., None]
    pix = np.concatenate([rgb, np.ones((alto, ancho, 1))], axis=2)
    if vertical:
        pix = np.ascontiguousarray(pix.transpose(1, 0, 2))
    return imagen(nombre, pix[::-1])


# ---------------------------------------------------------------- formas

def _objeto(nombre, malla, mat):
    obj = bpy.data.objects.new(nombre, malla)
    bpy.context.scene.collection.objects.link(obj)
    if mat is not None:
        obj.data.materials.append(mat)
    return obj


def caja(nombre, centro, tamano, mat, bisel=0.0, segmentos=2, ahusado=1.0):
    """Caja medida en Unity (centro y tamaño como en ShapeKit.Box), con bordes redondeados.

    ahusado < 1 angosta la cara de abajo (patas que se afinan hacia el piso).
    """
    sx, sy, sz = tamano
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    for v in bm.verts:
        if v.co.z < 0:  # en Blender z es "arriba"
            v.co.x *= ahusado
            v.co.y *= ahusado
        v.co.x *= sx
        v.co.y *= sz
        v.co.z *= sy
    malla = bpy.data.meshes.new(nombre)
    bm.to_mesh(malla)
    bm.free()
    obj = _objeto(nombre, malla, mat)
    obj.location = unity(*centro)
    if bisel > 0:
        mod = obj.modifiers.new("Bisel", "BEVEL")
        mod.width = bisel
        mod.segments = segmentos
        mod.limit_method = "ANGLE"
        mod.harden_normals = True
        obj.data.shade_smooth()
    return obj


def cilindro(nombre, centro, radio, largo, mat, eje="y", lados=12):
    """Cilindro medido en Unity; `eje` es el eje de Unity a lo largo del cual se estira."""
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=lados, radius1=radio, radius2=radio, depth=largo)
    malla = bpy.data.meshes.new(nombre)
    bm.to_mesh(malla)
    bm.free()
    obj = _objeto(nombre, malla, mat)
    obj.location = unity(*centro)
    if eje == "x":
        obj.rotation_euler = (0, math.pi / 2, 0)
    elif eje == "z":
        obj.rotation_euler = (math.pi / 2, 0, 0)
    obj.data.shade_smooth()
    return obj


def girar_y(obj, grados):
    """Gira como Quaternion.Euler(0, grados, 0) en Unity (en Blender el giro es al revés en z)."""
    obj.rotation_euler.z -= math.radians(grados)


def uv_cubo(obj, escala=1.0):
    """Proyección de cubo para que la textura caiga pareja en todas las caras."""
    bpy.context.view_layer.objects.active = obj
    for o in bpy.context.selected_objects:
        o.select_set(False)
    obj.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.cube_project(cube_size=escala)
    bpy.ops.object.mode_set(mode="OBJECT")


def uv_plano_arriba(obj):
    """UV de 0 a 1 mirando desde arriba (para hojas con líneas y tapas)."""
    malla = obj.data
    if not malla.uv_layers:
        malla.uv_layers.new()
    capa = malla.uv_layers.active.data
    xs = [v.co.x for v in malla.vertices]
    ys = [v.co.y for v in malla.vertices]
    x0, x1, y0, y1 = min(xs), max(xs), min(ys), max(ys)
    for poligono in malla.polygons:
        for i in poligono.loop_indices:
            v = malla.vertices[malla.loops[i].vertex_index].co
            capa[i].uv = ((v.x - x0) / (x1 - x0 or 1), (v.y - y0) / (y1 - y0 or 1))


# ---------------------------------------------------------------- exportar

def exportar_fbx(ruta):
    """Aplica modificadores y exporta en metros, con los ejes y las texturas que Unity espera.

    Las texturas van como .png aparte, en Modelos/Texturas/<id>.<nombre>.png: Unity no leía
    las texturas metidas dentro del .fbx y la mesa salía blanca en las capturas.
    """
    for obj in bpy.context.scene.objects:
        obj.select_set(obj.type == "MESH")
    carpeta = os.path.dirname(ruta)
    id_modelo = os.path.splitext(os.path.basename(ruta))[0]
    texturas = os.path.join(carpeta, "Texturas")
    os.makedirs(texturas, exist_ok=True)
    for img in bpy.data.images:
        if img.source != "GENERATED" and not img.name.startswith(id_modelo):
            continue
        archivo = os.path.join(texturas, f"{id_modelo}.{img.name}.png")
        img.filepath_raw = archivo
        img.file_format = "PNG"
        img.save()
        img.source = "FILE"
    bpy.ops.export_scene.fbx(
        filepath=ruta,
        use_selection=True,
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        bake_space_transform=True,
        use_mesh_modifiers=True,
        mesh_smooth_type="FACE",
        add_leaf_bones=False,
        path_mode="RELATIVE",
        embed_textures=False,
    )
