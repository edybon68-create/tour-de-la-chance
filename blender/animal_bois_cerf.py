"""
animal_bois_cerf.py — Bois de cerf fleuris (boîte « Animaux : accessoires » : `bois_cerf`,
Rare, Tête). docs/specs/v5-catalogue.md §4 n°11, bible §13.

Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/animal_bois_cerf.py
Sortie : blender/exports/bois_cerf.fbx et docs/captures/v5_cheval_bois_cerf.png.

Trois niveaux de forme :
  - primaire   : deux bois en arc qui s'écartent en « V » (0,85 m d'envergure), sortis du
                 haut du crâne juste devant les oreilles du cheval ;
  - secondaire : 4 pointes par bois (le merrain + 3 andouillers), une bague « meule » à la
                 base, 3 petites fleurs roses et une feuille ;
  - tertiaire  : cœurs dorés des fleurs, pétales bombés (5 par fleur), nervure de la feuille,
                 bouts arrondis des pointes.
Matières : bois satiné 0,50, pétales mats 0,30, feuille vernie 0,60, doré déco (cœurs).

Hiérarchie :
    bois_cerf            vide, origine = PointChapeau (attachScale 1, attachOffset 0)
    └── Bois             les deux bois + la feuille
        ├── Anim_Fleur_1..3  une fleur ; origine = son cœur, axe de rotation = sa normale
        │                    (« les fleurs tournent doucement » : 20°/s, sens alternés)
        └── Eclat_1..3       vides au bout de 3 pointes (Rare : 1 éclat toutes les 4 s)
"""

import math
import os
import sys

import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402
import commun_animaux as A  # noqa: E402

NOM = "bois_cerf"
POINT = "PointChapeau"
# V7 (lot 10) : bois CARAMEL dodus à bouts crème, sur un serre-tête brun (nouvelle icône)
MATS = ["M7_BoisCaramel", "M5_PetaleCerf", "M5_FeuilleVerte", "M5_JauneVerni", "M7_BoutCreme",
        "M5_SangleBrune"]  # 0..5
K = 0.88                      # échelle des bois (le haut reste sous 2,5 m)


def bois(bm, s, base):
    """Un bois (s = +1 : gauche de l'image). Renvoie les bouts des pointes et des points
    où poser les fleurs."""
    def P(x, y, z):
        return base + Vector((s * x, y, z)) * K
    merrain = [P(0, 0, -0.02), P(0.06, 0.0, 0.12), P(0.17, -0.02, 0.25), P(0.29, -0.04, 0.35),
               P(0.38, -0.03, 0.47), P(0.43, 0.0, 0.60)]
    C.bm_tube(bm, merrain, 0.075, cotes=7, mat=0, rayons=[1.0, 0.95, 0.9, 0.82, 0.75, 0.62])
    C.bm_sphere(bm, 0.047, merrain[-1], segments=8, anneaux=5, mat=4)   # bout crème arrondi
    pointes = [merrain[-1]]
    for depuis, vers, r0 in ((1, (0.10, 0.17, 0.24), 0.034), (2, (0.18, 0.04, 0.46), 0.034),
                             (3, (0.25, 0.03, 0.55), 0.03)):
        a = merrain[depuis]
        b = P(*vers)
        mid = a.lerp(b, 0.5) + Vector((0, 0, 0.03))
        C.bm_tube(bm, [a, mid, b], r0 * 1.5, cotes=7, mat=0, rayons=[1.0, 0.9, 0.75])
        C.bm_sphere(bm, r0 * 1.5 * 0.75, b, segments=8, anneaux=5, mat=4)   # bout crème
        pointes.append(b)
    # meule : bague bosselée à la base
    meule = C.bm_tore(bm, 0.07, 0.025, segments=12, segments_tube=5, mat=0)
    axe = (merrain[1] - merrain[0]).normalized()
    bmesh.ops.transform(bm, matrix=A.matrice_vers(merrain[0] + axe * 0.03, axe), verts=meule)
    return pointes, merrain


def fleur(nom, centre, normale, rayon=0.075):
    bm = bmesh.new()
    m = C.repere(centre, normale)
    for k in range(5):
        a = 2 * math.pi * k / 5 + math.pi / 2
        p = Vector((math.cos(a), 0, math.sin(a))) * rayon * 0.62
        rot = Matrix.Rotation(-(a - math.pi / 2), 4, "Y")
        C.bm_coussin(bm, C.contour_ellipse(rayon * 0.42, rayon * 0.55, n=8), 0.02, 0.04,
                     m @ Matrix.Translation(p) @ rot, mat=1, anneaux=1)
    C.bm_disque(bm, rayon * 0.35, 0.03, m @ Matrix.Translation((0, 0.012, 0)), cotes=8, mat=3,
                bombe=0.012)
    return C.piece(nom, bm, MATS, lissage="tout", pivot=centre)


def construire():
    C.nouvelle_scene()
    bm = bmesh.new()
    toutes = []
    tiges = {}
    for s in (+1, -1):
        base, _ = A.surface((0.0, 1.62, -0.01), math.radians(90 * s), math.radians(58), -0.02,
                            fenetre=(0.0, 0.14))
        pointes, merrain = bois(bm, s, base)
        toutes += pointes
        tiges[s] = merrain
    # V7 (lot 10) : serre-tête brun qui passe sur le crâne d'un bois à l'autre (icône)
    arc = [A.surface((0.0, 1.62, -0.01), math.radians(a), math.radians(58 + 22 * math.cos(math.radians(a))),
                     0.012, fenetre=(0.0, 0.14))[0] for a in range(-90, 91, 15)]
    C.bm_tube(bm, arc[1:-1], 0.022, cotes=6, mat=5)
    # la feuille : accrochée près de la fleur du bois gauche
    a = tiges[1][2]
    m = C.repere(a + Vector((0.06, 0.07, -0.02)), Vector((0.3, 1, 0.4)).normalized())
    cont = A.contour_goutte(0.09, 0.16, n=12)
    C.bm_coussin(bm, cont, 0.018, 0.04, m @ Matrix.Rotation(math.radians(-50), 4, "Y"),
                 mat=2, anneaux=1)
    centre = C.U(*C.CHEVAL_ACCROCHES[POINT])
    racine = C.piece("Bois", bm, MATS, lissage="angle", pivot=centre)
    # 3 fleurs : sur le bois gauche (2) et le droit (1), tournées vers la caméra
    poses = [(tiges[1][2], Vector((0.25, 1, 0.3))), (tiges[1][4], Vector((0.2, 1, 0.5))),
             (tiges[-1][3], Vector((-0.25, 1, 0.4)))]
    for i, (p, n) in enumerate(poses):
        n = n.normalized()
        C.parenter(fleur(f"Anim_Fleur_{i + 1}", p + n * 0.045, n), racine)
    for e in C.reperes_effets([toutes[0], toutes[4], toutes[2]]):
        C.parenter(e, racine)
    return C.racine_v5(NOM, POINT, [racine])


if __name__ == "__main__":
    A.finir_animal(construire(), NOM, POINT)
