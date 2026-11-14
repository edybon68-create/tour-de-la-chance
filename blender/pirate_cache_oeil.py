"""
pirate_cache_oeil.py — Cache-œil (boîte d'accessoires Pirates : `cache_oeil`, Super rare,
Visage). Fiche : docs/specs/v5-catalogue.md §2 n° 03 ; règles : art-bible §13.

Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/pirate_cache_oeil.py
Sortie : blender/exports/cache_oeil.fbx et docs/captures/v5_cheval_cache_oeil.png.

Où il se pose : sur l'œil GAUCHE du cheval, donc à DROITE de l'image (Unity x = +0,165).
L'autre œil reste toujours visible : le cordon passe AU-DESSUS de lui, en diagonale sur
le front (comme un vrai cache-œil), puis fait le tour de la tête à hauteur des yeux. Il
reste sous le bord d'un chapeau ou d'un bandana (y ≤ 1,81 devant).

Les trois niveaux de forme (bible §13.2) :
  - primaire   : la plaque de cuir bombée (28 × 31 cm : elle couvre largement l'œil) ;
  - secondaire : le cordon (Ø 4 cm) qui épouse la tête, mesuré par rayons ;
  - tertiaire  : l'étoile dorée cousue (Ø 11 cm, bombée), 12 points de couture en cuir
                 foncé sur le bord, deux rivets dorés aux attaches du cordon.
Matières (3) : cuir brun satiné (0,50), cuir foncé mat (0,25), doré déco (1 / 0,80).

Hiérarchie (le code Unity retrouve les pièces par leur nom) :
    cache_oeil          vide, origine = PointVisage (attachScale 1, attachOffset 0)
    └── CacheOeil       plaque + cordon + étoile ; origine = PointVisage
        └── Eclat_1..3  vides : sur les branches de l'étoile (1 éclat / 2,5 s, émission 0,5)
Pas de pièce animée (fiche : « — ») : l'objet est rigide, il suit la tête.
"""

import math
import os
import sys

import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402
import commun_pirates as P  # noqa: E402

NOM = "cache_oeil"
POINT = "PointVisage"
MATS = ["M5_PirCuir", "M5_PirCuirFonce", "M7_OrVif"]  # 0, 1, 2 (V7 : étoile en or clair)
CENTRE_TETE = (0.0, 1.68, 0.03)
OEIL = (0.165, 1.685, -0.268)          # devant de l'œil gauche du cheval (Unity)
RX, RZ = 0.16, 0.17                   # demi-tailles de la plaque (V7 : un peu plus grande, comme l'icône)
# Le cordon : (angle en degrés vu du dessus, 0 = devant, + = gauche de l'image ; hauteur Unity)
CORDON = [(-21, 1.775), (-8, 1.795), (8, 1.808), (24, 1.814), (40, 1.808), (58, 1.79),
          (80, 1.76), (105, 1.73), (135, 1.71), (165, 1.70), (195, 1.695), (225, 1.69),
          (255, 1.69), (283, 1.69), (299, 1.69)]
ARBRE = None


def sur_tete(a, y, ecart):
    return C.sur_cheval_lisse(ARBRE, (0.0, y, 0.03), a, 0.0, ecart, pente=0.7,
                              fenetre=(0.20, 0.0), pas=0.10)


def lisser_chemin(pts, n=3):
    """Courbe de Catmull-Rom : `n` points de plus entre deux points de passage."""
    res = []
    for i in range(len(pts) - 1):
        p0 = pts[max(i - 1, 0)]
        p1, p2 = pts[i], pts[i + 1]
        p3 = pts[min(i + 2, len(pts) - 1)]
        for k in range(n):
            t = k / n
            t2, t3 = t * t, t * t * t
            res.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2
                              + (-p0 + 3 * p1 - 3 * p2 + p3) * t3))
    res.append(pts[-1])
    return res


def construire():
    global ARBRE
    C.nouvelle_scene()
    ARBRE = P.arbre()
    bm = bmesh.new()
    # --- la plaque, posée sur l'œil, face tournée comme l'œil (vers l'avant et le côté)
    c_tete = C.U(*CENTRE_TETE)
    p_oeil = C.U(*OEIL)
    n = (p_oeil - c_tete)
    n.z = 0.0
    n = (n.normalized() + Vector((0, 0.9, 0.12))).normalized()   # plus tournée vers l'avant
    centre = p_oeil + n * 0.022
    m = C.repere(centre, n)
    contour = C.contour_ellipse(RX, RZ, n=28)
    C.bm_coussin(bm, contour, 0.03, 0.085, m, mat=0, mat_dos=1, anneaux=3)   # V7 : lisse et bombée
    # 12 points de couture sur le bord
    for k in range(0):   # V7 : points de couture retirés (petites taches sombres)
        t = 2 * math.pi * (k + 0.5) / 12
        q = m @ Vector((RX * 0.82 * math.cos(t), 0.024, RZ * 0.82 * math.sin(t)))
        C.bm_boite(bm, (0.022, 0.012, 0.012), q, mat=1)
    # étoile dorée cousue, un peu au-dessus du centre
    m_et = m @ Matrix.Translation((0.0, 0.07, 0.0))
    C.bm_etoile(bm, 0.075, 0.03, m_et, mat=2, bombe=0.018)   # V7 : grosse étoile bombée au centre
    eclats = [m_et @ Vector((0.055 * math.cos(math.pi / 2 + 2 * math.pi * k / 5), 0.02,
                             0.055 * math.sin(math.pi / 2 + 2 * math.pi * k / 5)))
              for k in (0, 1, 4)]
    # --- le cordon : du haut-intérieur de la plaque, en diagonale au-dessus de l'autre
    # œil, puis tout le tour de la tête jusqu'au bord extérieur de la plaque
    pts = [sur_tete(math.radians(a), y, 0.018)[0] for a, y in CORDON]
    debut = m @ Vector((RX * 0.55, 0.01, RZ * 0.80))        # haut, côté intérieur
    fin = m @ Vector((-RX * 0.98, 0.0, 0.0))                 # bord extérieur
    chemin = lisser_chemin([debut] + pts + [fin], n=2)
    C.bm_tube(bm, chemin, 0.018, cotes=8, mat=1)
    for q in (debut, fin):
        C.bm_disque(bm, 0.022, 0.014, C.repere(q + n * 0.012, n), cotes=6, mat=2, bombe=0.006)
    obj = C.piece("CacheOeil", bm, MATS, lissage="angle", angle=50, pivot=C.U(*C.CHEVAL_ACCROCHES[POINT]))
    for e in C.reperes_effets(eclats):
        C.parenter(e, obj)
    return C.racine_v5(NOM, POINT, [obj])


if __name__ == "__main__":
    P.finir_pirate(construire(), NOM, POINT)
