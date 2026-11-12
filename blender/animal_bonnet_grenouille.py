"""
animal_bonnet_grenouille.py — Bonnet grenouille (boîte « Animaux : accessoires » :
`bonnet_grenouille`, Peu commun, Tête). docs/specs/v5-catalogue.md §4 n°06, bible §13.

Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/animal_bonnet_grenouille.py
Sortie : blender/exports/bonnet_grenouille.fbx et docs/captures/v5_cheval_bonnet_grenouille.png.

Trois niveaux de forme :
  - primaire   : un bonnet de tissu vert tout rond (silhouette « poire » : le dôme déborde
                 un peu au-dessus du revers) qui contient les oreilles du cheval ;
  - secondaire : DEUX GROS YEUX de grenouille en relief sur le dessus (Ø 0,24 : le
                 détail-signature à 180 %), le revers côtelé ;
  - tertiaire  : pupilles noires vernies avec reflet, sourire brodé sur le revers, deux
                 petites narines brodées (noir verni).
Matières : tissu vert mat 0,15, blanc verni 0,85 (yeux), noir verni (pupilles, broderies),
reflet.

Hiérarchie :
    bonnet_grenouille     vide, origine = PointChapeau (attachScale 1, attachOffset 0)
    └── Bonnet            dôme + revers + broderies ; origine = centre de la base
        ├── Anim_OeilGauche   œil + pupille ; origine = centre du globe (clignement : le code
        └── Anim_OeilDroit    écrase l'échelle verticale 1 → 0,1 → 1 en 0,15 s, toutes les 4 s)
"""

import math
import os
import sys

import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402
import commun_animaux as A  # noqa: E402

NOM = "bonnet_grenouille"
POINT = "PointChapeau"
MATS = ["M5_TissuGrenouille", "M5_BlancVerni", "M5_AnimNoirVerni", "M5_Reflet"]  # 0..3
BX, BY, BZ = C.CHAPEAU_BASE[0], C.CHAPEAU_BASE[1] + 0.04, C.CHAPEAU_BASE[2]
R0 = C.CHAPEAU_RAYON + 0.03
HAUT = 0.38
REVERS = 0.11


def dome(bm):
    profil = [(0.0, HAUT), (0.15, HAUT - 0.015), (0.26, HAUT - 0.06), (0.335, HAUT - 0.14),
              (0.365, 0.19), (0.36, 0.13), (R0 + 0.01, REVERS), (R0 - 0.01, 0.08)]
    C.bm_revolution(bm, profil, 16, mats=[0] * 7)


def revers(bm):
    profil = [(R0 - 0.02, REVERS + 0.005), (R0 + 0.03, REVERS + 0.01), (R0 + 0.055, REVERS - 0.02),
              (R0 + 0.055, 0.02), (R0 + 0.03, -0.012), (R0 - 0.03, -0.012)]

    def cotes(a, i, r, z):
        if 1 <= i <= 4:
            return r + 0.018 * (0.5 + 0.5 * math.cos(15 * a)), z
        return r, z
    C.bm_revolution(bm, profil, 30, mats=[0] * 5, rayon_fn=cotes)


def broderies(bm):
    """Sourire (arc), 2 narines et 2 joues, posés sur le devant du revers."""
    r = R0 + 0.075
    zc = REVERS / 2
    sourire = []
    for k in range(9):
        a = math.pi / 2 + math.radians(-34 + 68 * k / 8)
        sourire.append(Vector((r * math.cos(a), r * math.sin(a),
                               zc + 0.035 * (abs(k - 4) / 4) ** 1.6 - 0.005)))
    C.bm_tube(bm, sourire, 0.014, cotes=5, mat=2)
    # narines : sur le dôme, au-dessus du revers, devant
    for s in (-1, 1):
        a = math.pi / 2 + s * 0.12
        d = Vector((math.cos(a), math.sin(a), 0.25)).normalized()
        p = Vector((math.cos(a), math.sin(a), 0)) * 0.365 + Vector((0, 0, 0.20))
        C.bm_disque(bm, 0.016, 0.014, C.repere(p, d), cotes=6, mat=2)


def oeil(base, cote):
    """Un gros œil de grenouille, à moitié enfoncé dans le dôme, qui regarde devant."""
    a = math.pi / 2 + cote * 0.48
    d = Vector((math.cos(a), math.sin(a), 0))
    c = d * 0.19 + Vector((0, 0, HAUT + 0.01))
    bm = bmesh.new()
    C.bm_sphere(bm, 0.13, c, segments=10, anneaux=7, mat=1)
    # socle : un bourrelet vert sous le globe (l'œil « sort » du bonnet)
    C.bm_tore(bm, 0.105, 0.035, segments=10, segments_tube=5, mat=0,
              matrice=Matrix.Translation(c + Vector((0, 0, -0.06))))
    regard = Vector((cote * 0.12, 1.0, 0.18)).normalized()
    A.oeil_mignon(bm, c + regard * 0.115, regard, 0.065, 2, 3, hauteur=1.2)
    bmesh.ops.translate(bm, vec=base, verts=bm.verts)
    nom = "Anim_OeilGauche" if cote > 0 else "Anim_OeilDroit"
    return C.piece(nom, bm, MATS, lissage="tout", pivot=base + c)


def construire():
    C.nouvelle_scene()
    base = C.U(BX, BY, BZ)
    bm = bmesh.new()
    dome(bm)
    revers(bm)
    broderies(bm)
    bmesh.ops.translate(bm, vec=base, verts=bm.verts)
    bonnet = C.piece("Bonnet", bm, MATS, lissage="tout", pivot=base)
    for cote in (+1, -1):
        C.parenter(oeil(base, cote), bonnet)
    return C.racine_v5(NOM, POINT, [bonnet])


if __name__ == "__main__":
    A.finir_animal(construire(), NOM, POINT)
