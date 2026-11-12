"""
cheval_accessoires.py — Essayage des 16 accessoires sur le cheval (contrôle visuel).

Lancer (après personnage_cheval.py, qui exporte blender/exports/personnage_cheval.fbx) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/cheval_accessoires.py
Pour n'essayer que quelques accessoires :
    ACCESSOIRES="chapeau_fete echarpe" /Applications/Blender.app/.../Blender --background --python ...

Ce script ne crée AUCUN modèle : il importe le cheval et chaque accessoire (FBX), pose
l'accessoire sur son point d'accroche EXACTEMENT comme le fera Unity (enfant du point,
localPosition = décalage, localScale = échelle), puis rend l'image vue de la caméra du jeu
et assemble la planche des 16 rendus :
    docs/captures/v5_planche_accessoires.png
(v5 : l'ancienne planche docs/captures/cheval_planche.png est gardée comme référence de
« l'état d'avant » citée par la bible §13.)

Le tableau REGLAGES est la référence pour le programmeur : il dit, pour chaque accessoire,
quel fichier utiliser (l'ancien dans Assets/Models ou la version refaite pour le cheval dans
blender/exports) et quelle échelle / quel décalage local appliquer.
"""

import math
import os
import sys

import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402

# id : (point d'accroche, échelle uniforme, décalage local Unity (x ; y ; z), source)
# source "unity"   = FBX actuel de TourDeLaChance-Unity/Assets/Models (lu, jamais modifié)
#        "exports" = version refaite pour le cheval, dans blender/exports
# v5 (bible §13) : les 16 accessoires sont refaits SUR le cheval, déjà à la bonne taille et à
# la bonne place par rapport à leur point d'accroche. Plus aucun bricolage d'échelle ni de
# décalage : attachScale = 1 et attachOffset = (0 ; 0 ; 0) pour tous.
REGLAGES = {
    "chapeau_casquette":     ("PointChapeau", 1.0, (0.0, 0.0, 0.0), "exports"),
    "chapeau_fete":          ("PointChapeau", 1.0, (0.0, 0.0, 0.0), "exports"),
    "chapeau_haut_de_forme": ("PointChapeau", 1.0, (0.0, 0.0, 0.0), "exports"),
    "chapeau_couronne":      ("PointChapeau", 1.0, (0.0, 0.0, 0.0), "exports"),
    "chapeau_bonnet":        ("PointChapeau", 1.0, (0.0, 0.0, 0.0), "exports"),
    "chapeau_sorcier":       ("PointChapeau", 1.0, (0.0, 0.0, 0.0), "exports"),
    "aureole":               ("PointChapeau", 1.0, (0.0, 0.0, 0.0), "exports"),
    "lunettes_soleil":       ("PointVisage", 1.0, (0.0, 0.0, 0.0), "exports"),
    "lunettes_coeur":        ("PointVisage", 1.0, (0.0, 0.0, 0.0), "exports"),
    "masque_heros":          ("PointVisage", 1.0, (0.0, 0.0, 0.0), "exports"),
    "noeud_papillon":        ("PointCou", 1.0, (0.0, 0.0, 0.0), "exports"),
    "echarpe":               ("PointCou", 1.0, (0.0, 0.0, 0.0), "exports"),
    "collier_fleurs":        ("PointCou", 1.0, (0.0, 0.0, 0.0), "exports"),
    "ailes_ange":            ("PointDos", 1.0, (0.0, 0.0, 0.0), "exports"),
    "ailes_papillon":        ("PointDos", 1.0, (0.0, 0.0, 0.0), "exports"),
    "planete":               ("PointDos", 1.0, (0.0, 0.0, 0.0), "exports"),
}

# Hauteur (au-dessus de la base du cheval) sous laquelle on ne cadre plus : on zoome sur ce
# qui porte l'accessoire. Les accessoires du cou et du dos ont besoin de voir plus bas.
Y_MIN = {"PointChapeau": 1.0, "PointVisage": 1.0, "PointCou": 0.55, "PointDos": 0.45}


def fichier(id_, source):
    dossier = C.DOSSIER_EXPORTS if source == "exports" else C.DOSSIER_MODELES
    return os.path.join(dossier, f"{id_}.fbx")


def essayer(id_, cheval):
    point, echelle, decalage, source = REGLAGES[id_]
    objs = C.importer_fbx(fichier(id_, source))
    racines = [o for o in objs if o.parent is None]
    C.poser_sur_point(racines, cheval[point], echelle, decalage)
    chemin = C.rendre_camera_jeu(list(cheval.values()) + objs, f"_tmp_jeu_{id_}.png",
                                 y_min=C.CHEVAL_POSITION[1] + Y_MIN[point])
    for o in objs:
        bpy.data.objects.remove(o, do_unlink=True)
    return chemin


def planche(chemins, nom="cheval_planche.png", colonnes=4, case=320):
    """Assemble les rendus en une grille (avec numpy, fourni avec Blender)."""
    import numpy as np
    lignes = math.ceil(len(chemins) / colonnes)
    grille = np.zeros((lignes * case, colonnes * case, 4), dtype=np.float32)
    grille[..., 3] = 1.0
    for i, ch in enumerate(chemins):
        img = bpy.data.images.load(ch)
        img.scale(case, case)
        px = np.array(img.pixels[:], dtype=np.float32).reshape(case, case, 4)
        l, c = divmod(i, colonnes)
        y0 = (lignes - 1 - l) * case        # les pixels Blender partent du BAS de l'image
        grille[y0:y0 + case, c * case:(c + 1) * case] = px
        bpy.data.images.remove(img)
    out = bpy.data.images.new("Planche", colonnes * case, lignes * case, alpha=True)
    out.pixels = grille.ravel()
    chemin = os.path.join(C.DOSSIER_CAPTURES, nom)
    out.filepath_raw = chemin
    out.file_format = "PNG"
    out.save()
    print("Planche :", chemin)


def main():
    C.MONDE_DEGRADE = True          # bible §13.4 : on juge le métal avec un ciel en dégradé
    C.nouvelle_scene()
    objs, cheval = C.importer_cheval()
    C.estrade()
    choix = os.environ.get("ACCESSOIRES", "").split() or list(REGLAGES)
    chemins = [essayer(id_, cheval) for id_ in choix]
    if len(choix) == len(REGLAGES):
        planche(chemins, nom="v5_planche_accessoires.png")
    for ch in chemins:              # les rendus un par un ne servent qu'à la planche
        os.remove(ch)


if __name__ == "__main__":
    main()
