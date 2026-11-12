"""
chapeau_haut_de_forme.py — Haut-de-forme (boîte d'accessoires n°1 : `chapeau_haut_de_forme`,
Peu commun, Tête). VERSION v5 « plus jeu moderne » (docs/art-bible.md §13.8 fiche 6), fait
SUR le cheval.

Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/chapeau_haut_de_forme.py
Sortie : blender/exports/chapeau_haut_de_forme.fbx et docs/captures/v5_cheval_chapeau_haut_de_forme.png.

Les trois niveaux de forme (bible §13.2) :
  - primaire   : le tube un peu évasé vers le haut (silhouette « poire » à l'envers) et le
                 bord relevé sur les côtés (+10 cm) ;
  - secondaire : le ruban rouge de 14 cm, bordé en haut et en bas d'un liseré doré de 4 cm
                 (le contraste de valeur qui dessine le chapeau noir) ; un nœud plat à
                 3 lobes sur le côté gauche ;
  - tertiaire  : une carte à jouer glissée dans le ruban, avec un cœur rouge en relief (sans
                 lettre ni chiffre) : la touche « magicien ».
Matières : feutre de soie (smoothness 0,40 : le contre-jour dessine enfin le chapeau),
ruban satin 0,55, doré déco, carte satinée.

Hiérarchie (le code Unity retrouve les pièces par leur nom) :
    chapeau_haut_de_forme  vide, origine = PointChapeau (attachScale 1, attachOffset 0)
    └── Anim_HautDeForme   tout le chapeau ; origine = centre de la base (ressort, et léger
                           balancement ±3° en 3 s : bible §13.5 types A et B)
"""

import math
import os
import sys

import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402

NOM = "chapeau_haut_de_forme"
POINT = "PointChapeau"
MATS = ["M5_FeutreSoie", "M5_SatinRouge", "M5_DoreDeco", "M5_BlancSatin"]  # 0..3
BX, BY, BZ = C.CHAPEAU_BASE[0], C.CHAPEAU_BASE[1] + 0.06, C.CHAPEAU_BASE[2]
RT = 0.29          # rayon du tube en bas
SEG = 16


def tube(bm):
    p = [(RT - 0.03, -0.03), (RT, 0.0),
         # liserés dorés BOMBÉS (demi-jonc) : leur moitié haute regarde le ciel et brille,
         # alors qu'une bande droite ne refléterait que le sol sombre (vue d'en haut)
         (RT + 0.022, 0.012), (RT + 0.03, 0.028), (RT + 0.016, 0.045),   # liseré du bas
         (RT + 0.015, 0.145),                                            # ruban
         (RT + 0.022, 0.152), (RT + 0.03, 0.168), (RT + 0.016, 0.185),   # liseré du haut
         (RT - 0.002, 0.19), (RT + 0.005, 0.36), (RT + 0.035, 0.52),
         (RT + 0.03, 0.555), (RT + 0.005, 0.57), (0.0, 0.575)]
    mats = [0, 2, 2, 2, 1, 2, 2, 2, 2, 0, 0, 0, 0, 0]
    v0 = set(bm.verts)
    C.bm_revolution(bm, p, SEG, mats=mats)
    return [v for v in bm.verts if v not in v0]


def bord(bm):
    """Le bord : un anneau plat de 4 cm, relevé de 10 cm sur les côtés (gauche/droite)."""
    n = 24
    loops = []
    for r_, dz in ((RT - 0.01, 0.02), (RT + 0.17, 0.02), (RT + 0.17, -0.02), (RT - 0.01, -0.02)):
        loop = []
        for k in range(n):
            a = 2 * math.pi * k / n
            leve = 0.10 * math.cos(a) ** 2 * ((r_ - RT) / 0.17) ** 1.5 if r_ > RT else 0.0
            loop.append((r_ * math.cos(a), r_ * math.sin(a), dz + leve))
        loops.append(loop)
    loops.append(loops[0])
    rangs = C.bm_anneaux(bm, loops, mats=[0] * 4)
    v = [x for r in rangs for x in r]
    bmesh.ops.remove_doubles(bm, verts=v, dist=1e-5)
    return [x for x in v if x.is_valid]


def noeud_et_carte(bm):
    """Nœud à 3 lobes à gauche (+X Blender) et carte à jouer devant à droite."""
    v = []
    a = math.radians(-15)                 # presque à gauche de l'image (+X), un peu devant
    d = Vector((math.cos(a), math.sin(a) * -1, 0)).normalized()
    d = Vector((1.0, 0.25, 0.0)).normalized()
    centre = d * (RT + 0.04) + Vector((0, 0, 0.095))
    m = C.repere(centre, d)
    for ang, l in ((90, 0.065), (210, 0.065), (330, 0.065)):
        r = math.radians(ang)
        cont = C.contour_ellipse(0.05, 0.035, n=8)
        mm = m @ Matrix.Translation((l * math.cos(r), 0, l * math.sin(r))) \
            @ Matrix.Rotation(-r, 4, "Y")
        v += C.bm_coussin(bm, cont, 0.03, 0.05, mm, mat=1, anneaux=1)
    v += C.bm_disque(bm, 0.03, 0.04, m @ Matrix.Translation((0, 0.01, 0)), cotes=8, mat=1,
                     bombe=0.01)
    # carte : glissée dans le ruban, devant à droite de l'image, penchée de 15°
    dc = Vector((-0.55, 1.0, 0.0)).normalized()
    cc = dc * (RT + 0.03) + Vector((0, 0, 0.17))
    mc = C.repere(cc, dc) @ Matrix.Rotation(math.radians(15), 4, "Y")
    v += C.bm_plaque(bm, C.contour_rectangle_arrondi(0.13, 0.18, 0.02, par_coin=1), 0.02,
                     mc, mat=3)
    v += C.bm_coussin(bm, C.contour_coeur(0.08, n=12), 0.012, 0.03,
                      mc @ Matrix.Translation((0, 0.012, 0.01)), mat=1, anneaux=1,
                      dos_plat=True)
    return v


def construire():
    C.nouvelle_scene()
    base = C.U(BX, BY, BZ)
    bm = bmesh.new()
    v = tube(bm) + bord(bm) + noeud_et_carte(bm)
    bmesh.ops.translate(bm, vec=base, verts=list(set(v)))
    chapeau = C.piece("Anim_HautDeForme", bm, MATS, lissage="angle", pivot=base)
    return C.racine_v5(NOM, POINT, [chapeau])


if __name__ == "__main__":
    C.finir_v5(construire(), NOM, POINT)
