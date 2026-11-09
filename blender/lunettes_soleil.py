"""
lunettes_soleil.py — Lunettes de soleil (boîte d'accessoires n°1 : `lunettes_soleil`, Commun, Visage).

Fiche suivie : docs/specs/boites.md §5.6. Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/lunettes_soleil.py

Un seul objet, nommé comme l'identifiant. Origine = point d'accroche Visage = `PointVisage`,
Unity (0 ; 1,6 ; −0,49) local à Joueur (devant la tête, à hauteur des yeux) : position et
rotation locales à zéro.
Chaque verre est tourné pour épouser la courbe de la tête (environ 21° vers le côté et 11°
vers le haut) et posé juste devant l'œil, qu'il cache : c'est le côté « cool ».
"""

import math
import os
import sys

import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402

NOM = "lunettes_soleil"
EMPLACEMENT = "visage"
MATS = ["M_VerreSombre", "M_Reflet"]  # indices 0, 1
EPAISSEUR = 0.04
LARGEUR, HAUTEUR = 0.30, 0.20     # fiche : 0,26 × 0,18, grossis (« grosses lunettes »)
ECART = 0.565            # distance du centre du verre au centre de la tête (yeux à 0,53)


def verre(bm, x_unity):
    """Un verre « rectangle arrondi » 0,30 × 0,20 × 4 cm + son reflet en diagonale.
    Renvoie le repère du verre (utile pour accrocher le pont et la branche)."""
    m = C.repere_verre(x_unity, ECART)
    C.bm_plaque(bm, C.contour_rectangle_arrondi(LARGEUR, HAUTEUR, 0.075, par_coin=4),
                EPAISSEUR, m, mat=0)
    # Reflet blanc 5 × 2 cm, en haut à gauche de l'image (+X local), penché à 35°.
    rot = Matrix.Rotation(math.radians(35), 4, "Y")
    pos = Matrix.Translation((0.06, EPAISSEUR / 2 + 0.002, 0.04))
    v = C.bm_boite(bm, (0.05, 0.006, 0.02), (0, 0, 0), mat=1)
    bmesh.ops.transform(bm, matrix=m @ pos @ rot, verts=v)
    return m


def construire():
    C.nouvelle_scene()
    bm = bmesh.new()        # verres (biseautés)
    bm_fins = bmesh.new()   # branches et pont (fins : pas de biseau, ils sont déjà arrondis)
    section = C.contour_rectangle_arrondi(0.03, 0.04, 0.012, par_coin=1)  # 3 × 4 cm
    for x_unity in (-0.18, +0.18):
        m = verre(bm, x_unity)
        cote = 1 if x_unity < 0 else -1   # côté Blender (+X = gauche de l'image)
        # Branche : part du bord extérieur du verre et file vers l'arrière en longeant la
        # tête ; elle s'enfonce dans la tête après ~8 cm visibles.
        # (+X local du verre = +X Blender = gauche de l'image, pour les deux verres.)
        depart = m @ Vector((cote * (LARGEUR / 2 - 0.02), -0.005, 0.03))
        chemin = [depart]
        for angle, rayon in ((44, 0.52), (52, 0.495), (60, 0.455)):
            a = math.radians(angle)
            chemin.append(C.perso(0, 1.63, 0) + Vector((cote * rayon * math.sin(a),
                                                        rayon * math.cos(a), 0)))
        C.bm_balayage(bm_fins, chemin, section, mat=0)

    # Pont : barre courbe de 8 cm entre les deux verres, un peu au-dessus de leur centre.
    pont = []
    for k in range(5):
        a = math.radians(-7 + 14 * k / 4)
        pont.append(C.perso(0, 1.5, 0) + Vector((0.565 * math.sin(a), 0.565 * math.cos(a), 0.13)))
    C.bm_balayage(bm_fins, pont, section, mat=0)

    verres = C.objet_depuis_bmesh("Verres", bm, MATS)
    C.biseau(verres, largeur=0.008, segments=1, angle=60)
    C.appliquer(verres)
    fins = C.objet_depuis_bmesh("Monture", bm_fins, MATS)
    obj = C.joindre([verres, fins], NOM)
    C.lisser_angle(obj, 40)
    return C.vers_accroche(obj, EMPLACEMENT)


if __name__ == "__main__":
    C.finir_accessoire(construire(), NOM, EMPLACEMENT)
