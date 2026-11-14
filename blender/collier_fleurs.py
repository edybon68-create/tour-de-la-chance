"""
collier_fleurs.py — Collier de fleurs (boîte d'accessoires n°1 : `collier_fleurs`, Peu commun, Cou).
VERSION v5 « plus jeu moderne » (docs/art-bible.md §13.8 fiche 9), fait SUR le cheval.

Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/collier_fleurs.py
Sortie : blender/exports/collier_fleurs.fbx et docs/captures/v5_cheval_collier_fleurs.png.

Défaut corrigé : l'ancien collier était un anneau ROND et fin (fait pour la capsule) qui
flottait devant le cou et rentrait dans la crinière derrière. Celui-ci est construit comme
l'écharpe : chaque point du tour est posé sur la VRAIE surface du cou par un rayon « lissé »
(commun.sur_cheval_lisse), qui passe PAR-DESSUS la crinière au lieu d'y entrer. Le collier
« pend » : plus bas devant (sur le poitrail, y ≈ 0,94) que derrière (sur la nuque, y ≈ 1,10).

Les trois niveaux de forme (bible §13.2) :
  - primaire   : 8 grosses fleurs Ø 0,22 sur le devant et les côtés, tournées vers
                 l'extérieur et le haut (elles regardent la caméra) ;
  - secondaire : pétales BOMBÉS (5 par fleur, 6 cm d'épaisseur), une perle blanc cassé entre
                 deux fleurs, un cordon de perles qui passe sur la nuque ;
  - tertiaire  : le cœur de chaque fleur en dôme jaune verni.
Matières : pétales mats (0,30), cœurs et perles vernis (0,85).

Hiérarchie (le code Unity retrouve les pièces par leur nom) :
    collier_fleurs       vide, origine = PointCou (attachScale 1, attachOffset 0)
    └── Anim_Collier     perles + cordon ; origine = PointCou (ressort sur l'ensemble)
        └── Anim_Fleur_1 … Anim_Fleur_8   une fleur chacune ; origine = son cœur. Le code
                         les fait pivoter de ±8° sur elles-mêmes (axe = la normale de la
                         fleur = leur axe Y local), période 3 s, décalées de 0,12 s d'une
                         fleur à la suivante (une vague qui fait le tour). Numérotées de la
                         droite de l'image vers la gauche.
"""

import math
import os
import sys

import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402

NOM = "collier_fleurs"
POINT = "PointCou"
MATS = ["M5_PetaleRose", "M5_PetaleBlanc", "M5_JauneVerni", "M5_BlancVerni"]  # 0..3
NB = 10
ETENDUE = 2.3          # les fleurs couvrent ±2,0 rad (≈ ±115°) autour du devant
R_FLEUR = 0.14          # V7 (lot 5) : fleurs plus grosses, comme l'icône
AXE_Z = 0.02
ARBRE = None


def hauteur(a):
    """Le collier pend : y = 0,94 devant (a = 0), 1,10 derrière (a = π)."""
    return 1.02 - 0.08 * math.cos(a)


def sur_cou(a, ecart):
    centre = (0.0, hauteur(a), AXE_Z)
    return C.sur_cheval_lisse(ARBRE, centre, a, 0.0, ecart, pente=0.6,
                              fenetre=(0.42, 0.14), pas=0.14)


def contour_fleur():
    pts = []
    for p in range(5):
        c = math.pi / 2 + 2 * math.pi * p / 5
        for f, r in ((-0.5, 0.42), (-0.2, 0.97), (0.2, 0.97)):
            a = c + f * (2 * math.pi / 5)
            pts.append((R_FLEUR * r * math.cos(a), R_FLEUR * r * math.sin(a)))
    return pts


def fleur(k, a):
    p, d = sur_cou(a, 0.045)
    n = (d + Vector((0, 0, 0.42))).normalized()     # ≈ 23° vers le haut
    m = C.repere(p, n)
    bm = bmesh.new()
    C.bm_coussin(bm, contour_fleur(), 0.06, 0.10, m, mat=k % 2, anneaux=1, dos_plat=True)
    C.bm_coussin(bm, C.contour_ellipse(0.05, 0.05, n=12), 0.05, 0.10,
                 m @ Matrix.Translation((0, 0.025, 0)), mat=2, anneaux=1, dos_plat=True)
    return C.piece(f"Anim_Fleur_{k + 1}", bm, MATS, lissage="tout", pivot=p)


def construire():
    global ARBRE
    C.nouvelle_scene()
    ARBRE = C.arbre_cheval()
    angles = [-ETENDUE + 2 * ETENDUE * k / (NB - 1) for k in range(NB)]
    # (angle négatif = droite de l'image : on numérote de droite à gauche)
    fleurs = [fleur(k, a) for k, a in enumerate(angles)]
    bm = bmesh.new()
    # perles entre deux fleurs (un peu en retrait)
    for k in range(NB - 1):
        a = (angles[k] + angles[k + 1]) / 2
        p, d = sur_cou(a, 0.04)
        C.bm_sphere(bm, 0.045, p, segments=6, anneaux=4, mat=3)
    # cordon de perles sur la nuque : de la dernière fleur à la première, par derrière
    chemin = []
    n_dos = 9
    for i in range(n_dos + 1):
        a = ETENDUE + (2 * math.pi - 2 * ETENDUE) * i / n_dos
        p, d = sur_cou(a, 0.025)
        chemin.append(p)
    C.bm_tube(bm, chemin, 0.018, cotes=5, mat=3)
    for i in range(1, n_dos, 2):
        C.bm_sphere(bm, 0.04, chemin[i], segments=6, anneaux=4, mat=3)
    collier = C.piece("Anim_Collier", bm, MATS, lissage="tout",
                      pivot=C.U(*C.CHEVAL_ACCROCHES[POINT]))
    for f in fleurs:
        C.parenter(f, collier)
    return C.racine_v5(NOM, POINT, [collier])


if __name__ == "__main__":
    C.finir_v5(construire(), NOM, POINT, y_min=0.55)
