"""
lunettes_soleil.py — Lunettes de soleil (boîte d'accessoires n°1 : `lunettes_soleil`, Commun, Visage).

Fiche suivie : docs/specs/boites.md §5.6. Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/lunettes_soleil.py

VERSION CHEVAL (personnage « cavalier d'échecs », blender/personnage_cheval.py).
Un seul objet, nommé comme l'identifiant. Origine = point d'accroche Visage = `PointVisage`
du cheval, Unity (0 ; 1,68 ; −0,253) local à Joueur (sur le front, entre les yeux) :
position et rotation locales à zéro, échelle 1.
Chaque verre est posé 4 cm devant un œil (yeux en x = ±0,165), tourné de 10° vers le côté
pour suivre les coins du front ; il cache l'œil : c'est le côté « cool ».
Les branches longent les côtés du crâne : leur chemin est MESURÉ sur le cheval par des
rayons (commun.sur_cheval), elles collent donc à sa vraie forme.
Sortie : blender/exports/lunettes_soleil.fbx (en attente d'intégration dans Unity).
"""

import math
import os
import sys

import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402

NOM = "lunettes_soleil"
POINT = "PointVisage"
MATS = ["M_VerreSombre", "M_Reflet"]  # indices 0, 1
EPAISSEUR = 0.04
LARGEUR, HAUTEUR = 0.25, 0.23     # un peu plus que l'œil du cheval (0,17 × 0,21)
MONTEE = 0.02            # verres 2 cm plus haut que les yeux : vue d'en haut, ils les cachent bien
AVANCE = 0.04            # le verre est 4 cm devant l'œil
OUVERTURE = 10.0         # degrés : chaque verre tourne vers son côté


def repere_verre_cheval(x_unity):
    """Repère du verre devant l'œil situé en x_unity (Y local = vers l'avant, un peu ouvert)."""
    cote = 1 if x_unity < 0 else -1          # côté Blender (+X = gauche de l'image)
    a = math.radians(OUVERTURE)
    centre = C.depuis_unity(x_unity, C.CHEVAL_YEUX_Y + MONTEE, C.CHEVAL_YEUX_AVANT - AVANCE)
    return C.repere(centre, Vector((cote * math.sin(a), math.cos(a), 0.0)))


def verre(bm, x_unity):
    """Un verre « rectangle arrondi » + son reflet en diagonale. Renvoie son repère."""
    m = repere_verre_cheval(x_unity)
    C.bm_plaque(bm, C.contour_rectangle_arrondi(LARGEUR, HAUTEUR, 0.07, par_coin=4),
                EPAISSEUR, m, mat=0)
    # Reflet blanc 5 × 2 cm, en haut à gauche de l'image (+X local), penché à 35°.
    rot = Matrix.Rotation(math.radians(35), 4, "Y")
    pos = Matrix.Translation((0.05, EPAISSEUR / 2 + 0.002, 0.045))
    v = C.bm_boite(bm, (0.05, 0.006, 0.02), (0, 0, 0), mat=1)
    bmesh.ops.transform(bm, matrix=m @ pos @ rot, verts=v)
    return m


def construire():
    C.nouvelle_scene()
    arbre = C.arbre_cheval()   # la forme de la tête, pour coller les branches
    bm = bmesh.new()        # verres (biseautés)
    bm_fins = bmesh.new()   # branches et pont (fins : pas de biseau, ils sont déjà arrondis)
    section = C.contour_rectangle_arrondi(0.03, 0.04, 0.012, par_coin=1)  # 3 × 4 cm
    for x_unity in (-C.CHEVAL_YEUX_X, +C.CHEVAL_YEUX_X):
        m = verre(bm, x_unity)
        cote = 1 if x_unity < 0 else -1
        # Branche : part du bord extérieur du verre, longe le côté du crâne (2 cm au-dessus
        # de la peau) et finit enfoncée dans la tête, derrière l'œil.
        chemin = [m @ Vector((cote * (LARGEUR / 2 - 0.02), -0.005, 0.03))]
        for angle, ecart in ((52, 0.03), (66, 0.022), (80, 0.018), (94, -0.03)):
            p, _ = C.sur_cheval(arbre, math.radians(cote * angle), C.CHEVAL_YEUX_Y + MONTEE + 0.03, ecart)
            chemin.append(p)
        C.bm_balayage(bm_fins, chemin, section, mat=0)

    # Pont : barre de 8 cm entre les deux verres, un peu au-dessus de leur centre.
    pont = []
    for k in range(5):
        a = math.radians(-9 + 18 * k / 4)
        p, _ = C.sur_cheval(arbre, a, C.CHEVAL_YEUX_Y + MONTEE + 0.04, AVANCE)
        pont.append(p)
    C.bm_balayage(bm_fins, pont, section, mat=0)

    verres = C.objet_depuis_bmesh("Verres", bm, MATS)
    C.biseau(verres, largeur=0.008, segments=1, angle=60)
    C.appliquer(verres)
    fins = C.objet_depuis_bmesh("Monture", bm_fins, MATS)
    obj = C.joindre([verres, fins], NOM)
    C.lisser_angle(obj, 40)
    return C.vers_accroche_cheval(obj, POINT)


if __name__ == "__main__":
    C.finir_accessoire_cheval(construire(), NOM, POINT)
