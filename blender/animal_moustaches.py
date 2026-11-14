"""
animal_moustaches.py — Moustaches de chat (boîte « Animaux : accessoires » : `moustaches`,
Commun, Visage). docs/specs/v5-catalogue.md §4 n°05, bible §13.

Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/animal_moustaches.py
Sortie : blender/exports/moustaches.fbx et docs/captures/v5_cheval_moustaches.png.

Trois niveaux de forme :
  - primaire   : 6 moustaches (3 de chaque côté du museau), longues (0,36) et ÉPAISSES
                 (Ø 3 cm à la racine, pointe affinée), courbées en arc, en éventail
                 (−18°, 0°, +18°) : on les lit de loin ;
  - secondaire : deux « coussinets » ronds à la racine de chaque groupe (les babines de
                 chat), et un petit nez rose triangulaire verni sur le haut du museau ;
  - tertiaire  : 3 petits points roses (racines des moustaches) sur chaque coussinet, le reflet du nez.
Matières : moustaches satinées 0,55, coussinets blanc cassé mats, nez rose verni 0,85.

Hiérarchie :
    moustaches                vide, origine = PointVisage (attachScale 1, attachOffset 0)
    └── Nez                   nez rose + coussinets
        ├── Anim_MoustachesGauche   origine = la racine (« frémissent » toutes les 3 s)
        └── Anim_MoustachesDroite
"""

import math
import os
import sys

import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402
import commun_animaux as A  # noqa: E402

NOM = "moustaches"
POINT = "PointVisage"
MATS = ["M5_Moustache", "M5_LapinBlanc", "M5_NezRose", "M5_Reflet", "M5_AnimNoirVerni"]


def construire():
    C.nouvelle_scene()
    bm = bmesh.new()
    # nez rose : triangle arrondi, pointe en bas, en haut du devant du museau
    p, n = A.sur_museau(0.0, 1.42, 0.0)
    n = (n + Vector((0, 0.2, 0.4))).normalized()   # V7 : face vers la caméra
    m = C.repere(p + n * 0.03, n)
    # V7 (lot 10) : nez en CŒUR rose bombé, comme la nouvelle icône
    C.bm_coussin(bm, C.contour_coeur(0.17, n=24), 0.04, 0.085, m, mat=2, anneaux=3)
    C.bm_reflet(bm, m @ Matrix.Translation((0.02, 0.032, 0.012)), mat=3, echelle=0.6)
    racines = {}
    for s in (-1, 1):                    # s = +1 : gauche de l'image (+X Blender)
        pc, nc = A.sur_museau(-s * 0.115, 1.355, 0.0)   # V7 : plus haut, vus du jeu
        nc = (nc + Vector((0, 0.3, 0.5))).normalized()   # V7 : tournés vers la caméra (au-dessus)
        mc = C.repere(pc + nc * 0.02, nc)
        # V7 (lot 10) : deux GROS coussinets blancs bien ronds (les joues de l'icône), sans détail (épuré)
        C.bm_coussin(bm, C.contour_ellipse(0.10, 0.085, n=24), 0.04, 0.09, mc, mat=1, anneaux=3)

        racines[s] = (pc + nc * 0.04, nc)
    nez = C.piece("Nez", bm, MATS, lissage="tout", pivot=p)

    for s, nom in ((1, "Anim_MoustachesGauche"), (-1, "Anim_MoustachesDroite")):
        r, nc = racines[s]
        bm = bmesh.new()
        for ang in (-18, 0, 18):
            a = math.radians(ang)
            chemin = []
            for k in range(6):
                t = k / 5
                L = 0.36 * t
                x = s * (0.03 + L * math.cos(a))
                z = L * math.sin(a) - 0.035 * t * t + 0.02 * ang / 18
                y = 0.04 * math.sin(t * math.pi) - 0.08 * t
                chemin.append(r + Vector((x, y, z)))
            # V7 (lot 10) : moustaches ÉPAISSES et arrondies au bout (icône), 8 côtés
            C.bm_tube(bm, chemin, 0.032, cotes=6, mat=1,
                      rayons=[1.0, 1.0, 0.95, 0.9, 0.85, 0.75])
            C.bm_sphere(bm, 0.024, chemin[-1], segments=8, anneaux=5, mat=1)
        C.parenter(C.piece(nom, bm, MATS, lissage="tout", pivot=r), nez)
    return C.racine_v5(NOM, POINT, [nez])


if __name__ == "__main__":
    A.finir_animal(construire(), NOM, POINT)
