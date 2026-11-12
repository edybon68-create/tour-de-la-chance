"""
essayage_angles.py — Contrôle des accessoires portés par le cheval, vus de PRÈS et sous
PLUSIEURS angles (face, 3/4 avant, profil, dessus, 3/4 arrière).

Pourquoi ce script en plus de cheval_accessoires.py ?
    cheval_accessoires.py montre l'accessoire depuis la caméra du jeu : c'est la vue qui
    compte pour le joueur, mais elle cache beaucoup de défauts (un pan d'écharpe qui rentre
    dans le cou, un masque qui flotte à 5 cm du museau, un collier qui traverse la crinière).
    Ici on tourne autour de l'accessoire, en vue orthographique (sans perspective : on juge
    les distances honnêtement), pour voir ce qui traverse, ce qui flotte, ce qui ne suit pas
    la forme du cheval.

Lancer (Blender fermé, ou en tout cas pas besoin de l'ouvrir) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/essayage_angles.py
Pour quelques accessoires seulement :
    ACCESSOIRES="echarpe masque_heros" /Applications/Blender.app/.../Blender --background --python ...

Il ne crée et n'exporte AUCUN modèle : il importe le cheval et chaque accessoire, pose
l'accessoire EXACTEMENT comme Unity (réglages REGLAGES de cheval_accessoires.py : enfant du
point d'accroche, décalage local, échelle uniforme) et écrit :
    docs/captures/v5_essai_<id>.png    une bande de 5 vues (dans l'ordre ci-dessus)
"""

import math
import os
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402
import cheval_accessoires as A  # noqa: E402

# D'où l'on regarde (axes Blender : +Y = devant le cheval, +Z = haut, +X = gauche de l'image).
VUES = [
    ("face", (0.0, 1.0, 0.18)),
    ("trois_quarts", (0.75, 0.75, 0.40)),
    ("profil", (1.0, 0.0, 0.05)),
    ("dessus", (0.05, 0.30, 1.0)),
    ("arriere", (-0.70, -0.70, 0.40)),
]
CASE = 420          # taille d'une vue (pixels)
MARGE = 0.14        # mètres de cheval visibles autour de l'accessoire


def bornes_objets(objs):
    pts = [o.matrix_world @ Vector(c) for o in objs if o.type == "MESH" for c in o.bound_box]
    return C._bornes(pts)


def rendre_vues(objs_cadre, prefixe):
    """Rend une image par vue en cadrant sur la boîte englobante de `objs_cadre`
    (agrandie de MARGE). Renvoie la liste des chemins."""
    scene = bpy.context.scene
    C.monde_nuit(scene, ambiance=1.0)
    C.regler_rendu(scene, CASE, CASE)
    mn, mx = bornes_objets(objs_cadre)
    mn -= Vector((MARGE, MARGE, MARGE))
    mx += Vector((MARGE, MARGE, MARGE))
    centre = (mn + mx) / 2
    coins = [Vector((x, y, z)) for x in (mn.x, mx.x) for y in (mn.y, mx.y) for z in (mn.z, mx.z)]
    chemins = []
    for nom, d in VUES:
        d = Vector(d).normalized()
        cam_data = bpy.data.cameras.new("CamEssai")
        cam_data.type = "ORTHO"
        cam = bpy.data.objects.new("CamEssai", cam_data)
        scene.collection.objects.link(cam)
        cam.location = centre + d * 6.0
        cam.rotation_euler = (-d).to_track_quat("-Z", "Y").to_euler()
        bpy.context.view_layer.update()
        inv = cam.matrix_world.inverted()
        q = [inv @ c for c in coins]
        largeur = max(p.x for p in q) - min(p.x for p in q)
        hauteur = max(p.y for p in q) - min(p.y for p in q)
        cam_data.ortho_scale = max(largeur, hauteur) * 1.02
        cam_data.clip_end = 20.0
        scene.camera = cam
        # Lumière principale « attachée » à la vue : en haut à gauche de la caméra, pour que
        # même la vue arrière soit lisible (on juge des formes, pas l'éclairage du jeu).
        droite = cam.matrix_world.to_3x3() @ Vector((1, 0, 0))
        dir_lum = -(d + Vector((0, 0, 0.9)) - droite * 0.5).normalized()
        sd = bpy.data.lights.new("Cle", "SUN")
        sd.energy = 3.0
        sd.color = C.hex_vers_lineaire("#FFF1D6")[:3]
        sd.angle = math.radians(3)
        sun = bpy.data.objects.new("Cle", sd)
        sun.rotation_euler = dir_lum.to_track_quat("-Z", "Y").to_euler()
        scene.collection.objects.link(sun)
        chemin = os.path.join(C.DOSSIER_CAPTURES, f"_tmp_{prefixe}_{nom}.png")
        scene.render.filepath = chemin
        bpy.ops.render.render(write_still=True)
        chemins.append(chemin)
        for o in (cam, sun):
            bpy.data.objects.remove(o, do_unlink=True)
    return chemins


def bande(chemins, nom):
    """Colle les vues côte à côte en une seule image, puis efface les vues temporaires."""
    import numpy as np
    n = len(chemins)
    grille = np.zeros((CASE, n * CASE, 4), dtype=np.float32)
    for i, ch in enumerate(chemins):
        img = bpy.data.images.load(ch)
        px = np.array(img.pixels[:], dtype=np.float32).reshape(CASE, CASE, 4)
        grille[:, i * CASE:(i + 1) * CASE] = px
        grille[:, i * CASE:i * CASE + 2] = (0.5, 0.45, 0.6, 1.0)   # fin séparateur
        bpy.data.images.remove(img)
        os.remove(ch)
    out = bpy.data.images.new("Bande", n * CASE, CASE, alpha=True)
    out.pixels = grille.ravel()
    chemin = os.path.join(C.DOSSIER_CAPTURES, nom)
    out.filepath_raw = chemin
    out.file_format = "PNG"
    out.save()
    bpy.data.images.remove(out)
    print("Bande :", chemin)
    return chemin


def essayer(id_, cheval, reglages=None):
    point, echelle, decalage, source = (reglages or A.REGLAGES)[id_]
    objs = C.importer_fbx(A.fichier(id_, source))
    racines = [o for o in objs if o.parent is None]
    C.poser_sur_point(racines, cheval[point], echelle, decalage)
    chemin = bande(rendre_vues(objs, id_), f"v5_essai_{id_}.png")
    for o in objs:
        bpy.data.objects.remove(o, do_unlink=True)
    return chemin


def main():
    C.MONDE_DEGRADE = True          # bible §13.4 : on juge le métal avec un ciel en dégradé
    C.nouvelle_scene()
    _, cheval = C.importer_cheval(position_unity=(0, 0, 0))
    choix = os.environ.get("ACCESSOIRES", "").split() or list(A.REGLAGES)
    for id_ in choix:
        essayer(id_, cheval)


if __name__ == "__main__":
    main()
