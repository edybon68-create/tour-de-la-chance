"""
rendu_skins_textures.py — Rendu de contrôle du cheval habillé de chaque texture de skin (V7).

Lancer :
    TEX=<dossier des textures> SORTIE=<dossier> \\
        /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/rendu_skins_textures.py
Options : SKINS="animal_zebre ..." (sinon toutes les images du dossier).
Pour chaque skin : <SORTIE>/<id>.png, deux vues côte à côte (3/4 avant et profil), avec la
lumière du jeu. La texture est posée sur les UV cylindriques du corps (comme Unity) ;
l'image « _lumiere » éventuelle est branchée en émission.
"""
import math
import os
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402

TEX = os.environ["TEX"]
SORTIE = os.environ["SORTIE"]
CASE = 420


def materiau(nom):
    m = bpy.data.materials.new("Skin_" + nom)
    m.use_nodes = True
    nt = m.node_tree
    bsdf = nt.nodes["Principled BSDF"]
    bsdf.inputs["Roughness"].default_value = 0.45
    bsdf.inputs["Coat Weight"].default_value = 0.2
    img = nt.nodes.new("ShaderNodeTexImage")
    img.image = bpy.data.images.load(os.path.join(TEX, nom + ".png"))
    img.extension = "REPEAT"
    img.interpolation = "Linear"
    nt.links.new(img.outputs["Color"], bsdf.inputs["Base Color"])
    lum = os.path.join(TEX, nom + "_lumiere.png")
    if os.path.exists(lum):
        li = nt.nodes.new("ShaderNodeTexImage")
        li.image = bpy.data.images.load(lum)
        nt.links.new(li.outputs["Color"], bsdf.inputs["Emission Color"])
        bsdf.inputs["Emission Strength"].default_value = 1.0
    return m


def rendre(nom, corps, scene):
    corps.data.materials.clear()
    corps.data.materials.append(materiau(nom))
    chemins = []
    for k, d in enumerate(((0.75, 0.75, 0.35), (1.0, -0.05, 0.1))):
        d = Vector(d).normalized()
        cd = bpy.data.cameras.new("Cam")
        cd.type = "ORTHO"
        cd.ortho_scale = 2.35
        cam = bpy.data.objects.new("Cam", cd)
        scene.collection.objects.link(cam)
        centre = Vector((0, 0, 1.05))
        cam.location = centre + d * 8
        cam.rotation_euler = (-d).to_track_quat("-Z", "Y").to_euler()
        scene.camera = cam
        ch = os.path.join(SORTIE, f"_tmp_{nom}_{k}.png")
        scene.render.filepath = ch
        bpy.ops.render.render(write_still=True)
        chemins.append(ch)
        bpy.data.objects.remove(cam)
    import numpy as np
    imgs = []
    for ch in chemins:
        im = bpy.data.images.load(ch)
        p = np.empty(CASE * CASE * 4, np.float32)
        im.pixels.foreach_get(p)
        imgs.append(p.reshape(CASE, CASE, 4))
        bpy.data.images.remove(im)
        os.remove(ch)
    out = np.concatenate(imgs, axis=1)
    im = bpy.data.images.new("o", 2 * CASE, CASE)
    im.pixels.foreach_set(out.ravel())
    im.filepath_raw = os.path.join(SORTIE, nom + ".png")
    im.file_format = "PNG"
    im.save()
    print("RENDU", nom)


def principal():
    os.makedirs(SORTIE, exist_ok=True)
    C.MONDE_DEGRADE = True
    C.nouvelle_scene()
    objs = C.importer_fbx(os.path.join(C.DOSSIER_MODELES, "personnage_cheval.fbx"), (0, 0, 0))
    corps = next(o for o in objs if o.name.startswith("Corps"))
    scene = bpy.context.scene
    C.monde_nuit(scene, ambiance=1.0)
    C.regler_rendu(scene, CASE, CASE)
    C.lumieres_jeu(scene)
    noms = os.environ.get("SKINS", "").split() or sorted(
        f[:-4] for f in os.listdir(TEX) if f.endswith(".png") and not f.endswith("_lumiere.png"))
    for n in noms:
        rendre(n, corps, scene)


if __name__ == "__main__":
    principal()
