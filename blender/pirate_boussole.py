"""
pirate_boussole.py — Boussole (boîte d'accessoires Pirates : `boussole`, Super rare, Cou).
Fiche : docs/specs/v5-catalogue.md §2 n° 05 ; règles : art-bible §13.

Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/pirate_boussole.py
Sortie : blender/exports/boussole.fbx et docs/captures/v5_cheval_boussole.png.

Les trois niveaux de forme (bible §13.2) :
  - primaire   : le gros médaillon doré (Ø 0,30 : le Ø 0,22 de la fiche exagéré ×1,4, bible §13.2),
                 posé à plat sur le poitrail (il suit la pente mesurée par un rayon) ;
  - secondaire : le cadran crème en creux, l'aiguille rouge en losange (le détail-signature),
                 la bélière (l'anneau du haut) et sa couronne ;
  - tertiaire  : la rose des vents dorée à 4 branches sous l'aiguille, 4 repères dorés
                 (N, E, S, O : de simples plots, aucune lettre), 2 maillons sous la chaîne.
La chaîne : un jonc doré de 3,6 cm qui épouse le cou (commun_pirates.tour_cou), en V
devant, jusqu'aux 2 maillons qui tiennent le médaillon.
Matières (3) : doré déco (metallic 1, 0,80), cadran crème satiné (0,50), aiguille rouge
vernie (0,85).

Hiérarchie (le code Unity retrouve les pièces par leur nom) :
    boussole            vide, origine = PointCou (attachScale 1, attachOffset 0)
    └── Chaine          le jonc autour du cou ; origine = PointCou
        └── Anim_Medaillon  2 maillons + médaillon ; origine = le haut du 1er maillon
            │               (ressort A, se balance au saut)
            ├── Anim_Aiguille  l'aiguille ; origine = son axe (au centre du cadran) :
            │                  tourne autour de la NORMALE DU CADRAN, qui vaut en Unity
            │                  (repère du parent) AXE_AIGUILLE ≈ (0 ; 0,33 ; −0,95) (le
            │                  script l'affiche), en « cherchant le nord » (oscille, repart)
            └── Eclat_1..3  vides : sur le bord doré (1 éclat / 2,5 s, émission 0,5)
"""

import math
import os
import sys

import bmesh
import bpy
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402
import commun_pirates as P  # noqa: E402

NOM = "boussole"
POINT = "PointCou"
MATS = ["M5_DoreDeco", "M5_PirCadran", "M5_PirAiguille"]   # 0, 1, 2
Y_MEDAILLON = 0.77
R_MED = 0.15


def repere_medaillon():
    """Repère du médaillon : Z local = la normale du poitrail (vers l'avant), Y local au
    plus près du haut. Renvoie (matrice, centre, normale)."""
    p, d = C.sur_cheval_lisse(P.arbre(), (0.0, Y_MEDAILLON, P.AXE_COU_Z), 0.0, 0.0, 0.0,
                              pente=0.6, fenetre=(0.3, 0.12), pas=0.1)
    # normale « moyenne » du poitrail : on vise deux points au-dessus et en dessous
    p_h, _ = C.sur_cheval_lisse(P.arbre(), (0.0, Y_MEDAILLON + 0.1, P.AXE_COU_Z), 0.0, 0.0, 0.0)
    p_b, _ = C.sur_cheval_lisse(P.arbre(), (0.0, Y_MEDAILLON - 0.1, P.AXE_COU_Z), 0.0, 0.0, 0.0)
    pente = (p_h - p_b).normalized()
    normale = Vector((1, 0, 0)).cross(pente).normalized()
    if normale.y < 0:
        normale = -normale
    centre = p + normale * 0.045
    return P.matrice_vers(centre, normale, haut=pente), centre, normale


def medaillon(bm, m):
    prof = [(0.0, -0.035), (0.11, -0.04), (R_MED - 0.01, -0.03), (R_MED, -0.008),
            (R_MED - 0.002, 0.014), (R_MED - 0.02, 0.026), (R_MED - 0.03, 0.012),
            (0.0, 0.012)]
    av = set(bm.verts)
    C.bm_revolution(bm, prof, 18, mats=[0, 0, 0, 0, 0, 0, 1])
    v = [x for x in bm.verts if x not in av]
    bmesh.ops.transform(bm, matrix=m, verts=v)
    # rose des vents dorée (4 branches) et 4 plots
    C.bm_etoile(bm, 0.088, 0.008, m @ Matrix.Translation((0, 0, 0.016))
                @ Matrix.Rotation(math.radians(90), 4, "X"), mat=0, branches=4, creux=0.3)
    for k in range(4):
        a = math.pi / 2 * k
        C.bm_sphere(bm, 0.012, m @ Vector((0.095 * math.cos(a), 0.095 * math.sin(a), 0.016)),
                    segments=6, anneaux=3, mat=0)
    # bélière (anneau) et couronne en haut
    haut = m @ Vector((0, R_MED + 0.012, 0))
    C.bm_sphere(bm, 0.022, haut, segments=8, anneaux=4, mat=0, echelle=(1.2, 1, 0.9))
    centre_anneau = m @ Vector((0, R_MED + 0.05, 0))
    C.bm_tore(bm, 0.03, 0.011, segments=10, segments_tube=4, mat=0,
              matrice=m @ Matrix.Translation((0, R_MED + 0.05, 0))
              @ Matrix.Rotation(math.radians(90), 4, "Y"))
    return centre_anneau


def aiguille(m):
    bm = bmesh.new()
    contour = [(0.0, 0.105), (0.027, 0.0), (0.0, -0.085), (-0.027, 0.0)]
    C.bm_plaque(bm, contour, 0.016, m @ Matrix.Translation((0, 0, 0.03))
                @ Matrix.Rotation(math.radians(90), 4, "X"), mat=2)
    C.bm_sphere(bm, 0.017, m @ Vector((0, 0, 0.04)), segments=6, anneaux=3, mat=0)
    o = C.piece("Anim_Aiguille", bm, MATS, lissage="angle", pivot=m @ Vector((0, 0, 0.03)))
    return o


def construire():
    C.nouvelle_scene()
    m, centre, normale = repere_medaillon()
    bm = bmesh.new()
    anneau = medaillon(bm, m)
    # 2 maillons au-dessus de la bélière
    up = (m.to_3x3() @ Vector((0, 1, 0))).normalized()
    lat = (m.to_3x3() @ Vector((1, 0, 0))).normalized()
    m1 = anneau + up * 0.07
    m2 = m1 + up * 0.075
    P.bm_maillon(bm, m1, up, normale, longueur=0.10, largeur=0.06, fil=0.012, mat=0)
    P.bm_maillon(bm, m2, up, lat, longueur=0.10, largeur=0.06, fil=0.012, mat=0)
    attache = m2 + up * 0.04
    med = C.piece("Anim_Medaillon", bm, MATS, lissage="angle", pivot=attache)
    aig = aiguille(m)
    C.parenter(aig, med)
    print("AXE_AIGUILLE Unity = (%.3f ; %.3f ; %.3f)" % (-normale.x, normale.z, -normale.y))

    # chaîne : un jonc qui suit le cou, en V jusqu'à l'attache
    y_att = attache.z

    def hauteur(a):
        c = max(0.0, math.cos(a))
        return 1.04 - (1.04 - y_att - 0.01) * c ** 4

    tour = P.tour_cou(hauteur, 0.02, n=26)
    chemin = [p for p, _ in tour]
    chemin[0] = attache.copy()
    chemin.append(chemin[0])
    bm = bmesh.new()
    C.bm_tube(bm, chemin, 0.018, cotes=5, mat=0, boucher=False)
    chaine = C.piece("Chaine", bm, MATS, lissage="tout", pivot=C.U(*C.CHEVAL_ACCROCHES[POINT]))
    C.parenter(med, chaine)
    eclats = [m @ Vector((R_MED * math.cos(a), R_MED * math.sin(a), 0.02))
              for a in (math.radians(135), math.radians(20), math.radians(250))]
    for e in C.reperes_effets(eclats):
        C.parenter(e, med)
    return C.racine_v5(NOM, POINT, [chaine])


if __name__ == "__main__":
    P.finir_pirate(construire(), NOM, POINT, y_min=0.55)
