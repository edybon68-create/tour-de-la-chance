"""
chapeau_fete.py — Chapeau de fête (boîte d'accessoires n°1 : `chapeau_fete`, Commun, Tête).
VERSION v5 « plus jeu moderne » (docs/art-bible.md §13.8 fiche 2), faite SUR le cheval.

Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/chapeau_fete.py
Sortie : blender/exports/chapeau_fete.fbx et docs/captures/v5_cheval_chapeau_fete.png.

Les trois niveaux de forme (bible §13.2) :
  - primaire   : le cône rayé rose / jaune, penché de 10° vers la gauche de l'image ;
  - secondaire : les bandes jaunes en RELIEF de 1,5 cm (chaque rayure se lit par son ombre) et
                 la collerette de papier à 12 festons arrondis, blanc cassé ;
  - tertiaire  : le gros pompon « duveteux » Ø 0,26 (7 boules groupées) — le détail-signature.
Matières : carton brillant (0,65) contre pompon et collerette mats (0,15).

Hiérarchie (le code Unity retrouve les pièces par leur nom) :
    chapeau_fete        vide, origine = PointChapeau (attachScale 1, attachOffset 0)
    └── Chapeau         cône + collerette ; origine = centre de la base
        └── Anim_Pompon le pompon ; origine = sa base, sur la pointe du cône (ressort : il
                        rebondit après chaque saut, bible §13.5 type A)
"""

import math
import os
import sys

import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402

NOM = "chapeau_fete"
POINT = "PointChapeau"
MATS = ["M5_CartonRose", "M5_CartonJaune", "M5_BlancMat"]  # 0, 1, 2
BX, BY, BZ = C.CHAPEAU_BASE[0], C.CHAPEAU_BASE[1] + 0.06, C.CHAPEAU_BASE[2]
R0 = C.CHAPEAU_RAYON + 0.02
HAUTEUR = 0.66
SEG = 28                       # V7 : 16 -> 28, cône bien rond
RELIEF = 0.015
PENCHE = math.radians(10)


def inclinaison():
    """Le chapeau penche de 10° vers la gauche de l'image, autour du centre du crâne (le
    bord reste posé sur la tête au lieu de se soulever d'un côté)."""
    pivot = C.U(*C.CHEVAL_CRANE_CENTRE)
    return (Matrix.Translation(pivot) @ Matrix.Rotation(PENCHE, 4, "Y")
            @ Matrix.Translation(-pivot))


def cone(bm):
    """Cône en 6 bandes (rose, jaune, rose…) ; les jaunes ressortent de 1,5 cm."""
    profil, mats = [(R0 - 0.03, -0.02)], []
    bandes = 6
    for k in range(bandes):
        t0, t1 = k / bandes * 0.88, (k + 1) / bandes * 0.88
        r0, r1 = R0 * (1 - t0), R0 * (1 - t1)
        z0, z1 = HAUTEUR * t0, HAUTEUR * t1
        jaune = k % 2 == 1
        d = RELIEF if jaune else 0.0
        if jaune:
            profil += [(r0, z0), (r0 + d, z0 + 0.01), (r1 + d, z1 - 0.01), (r1, z1)]
            mats += [0, 1, 1, 1]
        else:
            profil += [(r0, z0), (r1, z1)]
            mats += [0, 0]
    # le profil commence en dessous du bord : un petit rebord rentrant (invisible)
    profil.append((0.0, HAUTEUR))
    mats.append(0)
    # nettoyer les points en double (même position)
    p2, m2 = [profil[0]], []
    for p, m in zip(profil[1:], mats):
        if abs(p[0] - p2[-1][0]) + abs(p[1] - p2[-1][1]) > 1e-4:
            p2.append(p)
            m2.append(m)
    v_avant = set(bm.verts)
    C.bm_revolution(bm, p2, SEG, mats=m2)
    return [v for v in bm.verts if v not in v_avant]


def collerette(bm):
    """Collerette de papier : un anneau épais de 6 cm, bord extérieur en 12 festons ronds,
    penché vers le bas comme une fraise."""
    n_fest, par = 12, 3
    r_in, r_out = R0 - 0.02, R0 + 0.10
    tour_in_h, tour_in_b, tour_out_h, tour_out_b = [], [], [], []
    for k in range(n_fest * par):
        a = 2 * math.pi * k / (n_fest * par)
        f = (k % par) / par                       # position dans le feston (0..1)
        r = r_out - 0.06 * (1 - math.sin(math.pi * f))     # f = 0 : creux entre 2 festons
        c, s = math.cos(a), math.sin(a)
        chute = -0.08 * (r - r_in) / (r_out - r_in)
        tour_in_h.append((r_in * c, r_in * s, 0.03))
        tour_in_b.append((r_in * c, r_in * s, -0.03))
        tour_out_h.append((r * c, r * s, chute + 0.03))
        tour_out_b.append((r * c, r * s, chute - 0.03))
    rangs = C.bm_anneaux(bm, [tour_in_b, tour_in_h, tour_out_h, tour_out_b, tour_in_b],
                         mats=[2] * 4)
    # le dernier anneau est un double du premier : on soude
    bmesh.ops.remove_doubles(bm, verts=[v for r in rangs for v in r], dist=1e-5)
    return list({v for r in rangs for v in r if v.is_valid})


def construire():
    C.nouvelle_scene()
    base = C.U(BX, BY, BZ)
    m = inclinaison() @ Matrix.Translation(base)
    bm = bmesh.new()
    # V7 (point 17) : plus de collerette de papier blanche (absente de l'icône, elle faisait
    # « fraise »). À la place, un simple bourrelet rose roulé au bord du cône.
    v = cone(bm)
    v += C.bm_tore(bm, R0 - 0.005, 0.03, segments=SEG, segments_tube=8, mat=0)
    bmesh.ops.transform(bm, matrix=m, verts=list(set(v)))
    chapeau = C.piece("Chapeau", bm, MATS, lissage="angle")
    C.deplacer_origine(chapeau, m @ Vector((0, 0, 0)))

    bm = bmesh.new()
    rp = 0.13
    v = C.bm_pompon(bm, (0, 0, HAUTEUR - 0.02 + rp * 0.75), rp, mat=2, boules=7)
    bmesh.ops.transform(bm, matrix=m, verts=v)
    pompon = C.piece("Anim_Pompon", bm, MATS, lissage="tout",
                     pivot=m @ Vector((0, 0, HAUTEUR - 0.04)))
    C.parenter(pompon, chapeau)
    return C.racine_v5(NOM, POINT, [chapeau])


if __name__ == "__main__":
    C.finir_v5(construire(), NOM, POINT)
