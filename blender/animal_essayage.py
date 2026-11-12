"""
animal_essayage.py — Contrôle des objets Animaux portés par le cheval.

Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/animal_essayage.py
Quelques objets seulement :
    ACCESSOIRES="truffe poussin" /Applications/Blender.app/.../Blender --background --python ...
Sans la planche :  PLANCHE=0  ·  sans les 5 vues :  VUES=0

Ce script n'exporte rien. Pour chaque objet de commun_animaux.REGLAGES il écrit :
    docs/captures/v5_essai_<id>.png    5 vues orthographiques (essayage_angles.py)
puis, si tous les objets sont demandés, la planche docs/captures/v5_planche_animaux.png :
chaque objet vu depuis la caméra du jeu (même cadrage que v5_planche_accessoires.png), les
deux skins à extras sur un cheval repeint dans la couleur du skin (roue du paon repliée PUIS
déployée), deux essais « 4 emplacements portés ensemble » (pour voir que rien ne se
chevauche) et les deux boîtes 3D (vue fermée de v5_boite_*.png).
"""

import math
import os
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402
import commun_animaux as AN  # noqa: E402
import essayage_angles as E  # noqa: E402

Y_MIN = {"PointChapeau": 1.0, "PointVisage": 1.0, "PointCou": 0.55, "PointDos": 0.45,
         "Joueur": 0.3}
# Couleur du cheval pour les skins à extras (couleur de base du skin, catalogue §3).
SKIN = {
    "animal_paon_extras": ("#1E6EB0", 0.35, 0.2),
    "animal_lion_soleil_extras": ("#FFB830", 0.5, 0.3),
}
# Essais « tout porté » : un objet par emplacement (tête, visage, cou, dos).
COMBOS = [
    ["bonnet_grenouille", "lunettes_hibou", "echarpe_renard", "carapace"],
    ["bois_cerf", "moustaches", "collier_medaille", "chaton_sac"],
]


def peindre_cheval(cheval, spec):
    corps = next(o for n, o in cheval.items() if n.startswith("Corps"))
    mat = corps.data.materials[0]
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if spec is None:
        spec = ("#4A90D9", 0.0, 0.7)
    bsdf.inputs["Base Color"].default_value = C.hex_vers_lineaire(spec[0])
    bsdf.inputs["Metallic"].default_value = spec[1]
    bsdf.inputs["Roughness"].default_value = spec[2]


def importer(id_, cheval):
    point, echelle, decalage, source = AN.REGLAGES[id_]
    objs = C.importer_fbx(os.path.join(C.DOSSIER_EXPORTS, f"{id_}.fbx"))
    racines = [o for o in objs if o.parent is None]
    C.poser_sur_point(racines, cheval[point], echelle, decalage)
    return objs, point


def deployer_roue(objs):
    """Pose « saut de joie » de la roue du paon : chaque plume debout (rotation locale
    nulle) puis tournée en éventail autour de l'axe avant-arrière (voir animal_paon.py)."""
    plumes = sorted([o for o in objs if o.name.startswith("Anim_Plume_")], key=lambda o: o.name)
    n = len(plumes)
    for i, o in enumerate(plumes):
        ang = math.radians(-84 + 168 * i / (n - 1))
        o.rotation_mode = "XYZ"
        # Les objets importés d'un FBX sont en Y-haut dans leur parent : l'axe avant-arrière
        # du cheval y est l'axe Z local. Rotation nulle = plume debout.
        o.rotation_euler = (0.0, 0.0, -ang)
    bpy.context.view_layer.update()


def vue_jeu(objs, cheval, point, nom):
    return C.rendre_camera_jeu(list(cheval.values()) + objs, nom,
                               y_min=C.CHEVAL_POSITION[1] + Y_MIN[point])


def planche(chemins, nom, colonnes=5, case=320):
    import numpy as np
    lignes = math.ceil(len(chemins) / colonnes)
    grille = np.zeros((lignes * case, colonnes * case, 4), dtype=np.float32)
    grille[..., :3] = (0.12, 0.09, 0.19)
    grille[..., 3] = 1.0
    for i, ch in enumerate(chemins):
        img = bpy.data.images.load(ch)
        w, h = img.size
        px = np.array(img.pixels[:], dtype=np.float32).reshape(h, w, 4)
        if w > h:                       # boîte : on garde la moitié gauche (fermée)
            px = px[:, : w // 2]
        tmp = bpy.data.images.new("tmp", px.shape[1], px.shape[0], alpha=True)
        tmp.pixels = px.ravel()
        tmp.scale(case, case)
        px = np.array(tmp.pixels[:], dtype=np.float32).reshape(case, case, 4)
        bpy.data.images.remove(tmp)
        bpy.data.images.remove(img)
        l, c = divmod(i, colonnes)
        y0 = (lignes - 1 - l) * case
        grille[y0:y0 + case, c * case:(c + 1) * case] = px
    out = bpy.data.images.new("Planche", colonnes * case, lignes * case, alpha=True)
    out.pixels = grille.ravel()
    chemin = os.path.join(C.DOSSIER_CAPTURES, nom)
    out.filepath_raw = chemin
    out.file_format = "PNG"
    out.save()
    print("Planche :", chemin)


def main():
    C.MONDE_DEGRADE = True
    choix = os.environ.get("ACCESSOIRES", "").split() or list(AN.REGLAGES)
    faire_vues = os.environ.get("VUES", "1") != "0"
    faire_planche = os.environ.get("PLANCHE", "1") != "0" and len(choix) == len(AN.REGLAGES)

    if faire_vues:
        C.nouvelle_scene()
        _, cheval = C.importer_cheval(position_unity=(0, 0, 0))
        for id_ in choix:
            peindre_cheval(cheval, SKIN.get(id_))
            objs, _ = importer(id_, cheval)
            E.bande(E.rendre_vues(objs, id_), f"v5_essai_{id_}.png")
            for o in objs:
                bpy.data.objects.remove(o, do_unlink=True)

    if not faire_planche:
        return
    C.nouvelle_scene()
    _, cheval = C.importer_cheval()
    C.estrade()
    chemins, temporaires = [], []
    for id_ in choix:
        peindre_cheval(cheval, SKIN.get(id_))
        objs, point = importer(id_, cheval)
        ch = vue_jeu(objs, cheval, point, f"_tmp_jeu_{id_}.png")
        chemins.append(ch)
        temporaires.append(ch)
        if id_ == "animal_paon_extras":
            deployer_roue(objs)
            ch = vue_jeu(objs, cheval, point, "_tmp_jeu_paon_roue.png")
            chemins.append(ch)
            temporaires.append(ch)
        for o in objs:
            bpy.data.objects.remove(o, do_unlink=True)
    peindre_cheval(cheval, None)
    for k, combo in enumerate(COMBOS):
        tous = []
        for id_ in combo:
            objs, _ = importer(id_, cheval)
            tous += objs
        ch = vue_jeu(tous, cheval, "PointDos", f"_tmp_jeu_combo{k}.png")
        chemins.append(ch)
        temporaires.append(ch)
        for o in tous:
            bpy.data.objects.remove(o, do_unlink=True)
    for b in ("boite_skins_animaux", "boite_accessoires_animaux"):
        ch = os.path.join(C.DOSSIER_CAPTURES, f"v5_{b}.png")
        if os.path.exists(ch):
            chemins.append(ch)
    planche(chemins, "v5_planche_animaux.png")
    for ch in temporaires:
        os.remove(ch)


if __name__ == "__main__":
    main()
