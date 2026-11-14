"""
pirate_longue_vue.py — Longue-vue (boîte d'accessoires Pirates : `longue_vue`, Super rare,
Dos). Fiche : docs/specs/v5-catalogue.md §2 n° 06 ; règles : art-bible §13.

Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/pirate_longue_vue.py
Sortie : blender/exports/longue_vue.fbx et docs/captures/v5_cheval_longue_vue.png.

Comment elle est portée : EN BANDOULIÈRE. Une sangle de cuir fait le tour du corps en
diagonale (basse devant, sur le poitrail à y = 0,53 ; haute derrière à y = 1,05 ; plus
haute à gauche de l'image qu'à droite), posée sur la surface mesurée par rayons. Elle
passe SOUS le foulard et sous le médaillon de la boussole (emplacement Cou), qui peuvent
donc être portés en même temps sans se toucher. La longue-vue est sanglée dans le dos, en
diagonale (25°), et dépasse de 0,33 m de chaque côté du cou vu de face.

Les trois niveaux de forme (bible §13.2) :
  - primaire   : la longue-vue (1,25 m, Ø 0,15 : le tube du gros bout en cuir) ;
  - secondaire : les 3 tubes emboîtés en laiton qui s'affinent, la sangle de cuir ;
  - tertiaire  : 5 bagues de laiton, la lentille bleutée vernie au gros bout, la boucle de
                 la sangle sur le poitrail, deux passants de cuir autour de la longue-vue.
Matières (3) : laiton (metallic 1, 0,75), cuir brun satiné (0,50), verre bleuté (0,95).

Hiérarchie (le code Unity retrouve les pièces par leur nom) :
    longue_vue          vide, origine = PointDos (attachScale 1, attachOffset 0)
    └── Bandouliere     la sangle et sa boucle ; origine = PointDos
        └── Anim_LongueVue  la longue-vue et ses passants ; origine = son milieu, sur la
                        sangle (ressort A : se balance un peu au saut, ±6°)
            └── Eclat_1..3  vides : sur les bagues de laiton (1 éclat / 2,5 s, émission 0,5)
"""

import math
import os
import sys

import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402
import commun_pirates as P  # noqa: E402

NOM = "longue_vue"
POINT = "PointDos"
MATS = ["M5_PirLaiton", "M5_PirCuir", "M5_PirVerre"]   # 0, 1, 2
N = 28
ANGLE = math.radians(40)        # la longue-vue penche : gauche de l'image plus haute
# V7 : 25° la faisait passer à plat derrière la tête, à hauteur des yeux : vue du jeu, elle
# semblait traverser le crâne « comme une broche ». Plus penchée et plus basse, elle se lit
# comme une longue-vue sanglée dans le dos (le haut dépasse à gauche du cou, le bas à droite).


def hauteur(a):
    """La sangle : basse devant (0,52), haute derrière (0,90), gauche (+) plus haute.
    Devant, elle passe SOUS le médaillon de la boussole (bas à 0,61) et la pointe du
    foulard (0,74) : on peut porter les deux ensemble."""
    return 0.71 - 0.19 * math.cos(a) + 0.05 * math.sin(a)


# Profil de la longue-vue le long de son axe : (rayon, position), du gros bout (+0,62,
# à droite de l'image, en bas) à l'oculaire (−0,62, à gauche, en haut) ; matériau par tronçon.
PROFIL = [(0.0, 0.615), (0.068, 0.62), (0.088, 0.615), (0.09, 0.56), (0.078, 0.55),
          (0.075, 0.33), (0.084, 0.32), (0.084, 0.28), (0.075, 0.27),
          (0.075, 0.06), (0.085, 0.05), (0.085, 0.01), (0.06, 0.0),
          (0.058, -0.28), (0.066, -0.29), (0.066, -0.32), (0.047, -0.33),
          (0.045, -0.54), (0.058, -0.57), (0.058, -0.62), (0.0, -0.625)]
GROS = 1.45      # V7 : facteur d'épaisseur du tube
MAT_PROFIL = [2, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]


def construire():
    C.nouvelle_scene()
    # --- la sangle
    tour = P.tour_cou(hauteur, -0.008, n=N)
    bm = bmesh.new()
    chemin = [p for p, _ in tour] + [tour[0][0]]
    hauts = [d for _, d in tour] + [tour[0][1]]
    C.bm_balayage_surface(bm, chemin[:-1], hauts[:-1],
                          [(-0.045, 0.0), (0.045, 0.0), (0.04, 0.03), (-0.04, 0.03)],
                          mat=1, ferme=True)
    # boucle de laiton sur le poitrail (un cadre rectangulaire bombé)
    p0, d0 = tour[0]
    m_b = C.repere(p0 + d0 * 0.03, d0)
    C.bm_plaque(bm, C.contour_rectangle_arrondi(0.13, 0.11, 0.025, par_coin=1), 0.025, m_b,
                mat=0)
    C.bm_plaque(bm, C.contour_rectangle_arrondi(0.07, 0.05, 0.01, par_coin=1), 0.03,
                m_b @ Matrix.Translation((0, 0.004, 0)), mat=1)
    sangle = C.piece("Bandouliere", bm, MATS, lissage="angle",
                     pivot=C.U(*C.CHEVAL_ACCROCHES[POINT]))

    # --- la longue-vue, dans le dos
    i_dos = N // 2
    p_dos, d_dos = tour[i_dos]
    centre = p_dos + d_dos * 0.13      # V7 : écartée du dos (tube plus gros)
    # axe : le long du dos (gauche-droite), penché de 25° (gauche de l'image = +X Blender)
    axe = Vector((-math.cos(ANGLE), 0.0, -math.sin(ANGLE)))   # vers le gros bout (droite, bas)
    bm = bmesh.new()
    # V7 (point 17) : tube 1,45 fois plus gros et 24 côtés. Vue du jeu, l'ancienne n'était
    # qu'une baguette sombre derrière la tête ; l'icône montre une longue-vue trapue.
    C.bm_revolution(bm, [(r * GROS, z) for r, z in PROFIL], 24, mats=MAT_PROFIL)
    m = P.matrice_vers(centre, axe, haut=d_dos)
    bmesh.ops.transform(bm, matrix=m, verts=bm.verts)
    # deux passants de cuir autour du gros tube (ils la tiennent contre la sangle)
    for u in (0.17, -0.15):
        C.bm_tore(bm, (0.083 if u > 0 else 0.066) * GROS, 0.018, segments=20, segments_tube=6, mat=1,
                  matrice=m @ Matrix.Translation((0, 0, u)))
    lv = C.piece("Anim_LongueVue", bm, MATS, lissage="angle", pivot=centre)
    C.parenter(lv, sangle)
    eclats = [m @ Vector((0, 0.086 * GROS, z)) for z in (0.30, 0.03, -0.305)]
    for e in C.reperes_effets(eclats):
        C.parenter(e, lv)
    return C.racine_v5(NOM, POINT, [sangle])


if __name__ == "__main__":
    P.finir_pirate(construire(), NOM, POINT, y_min=0.45)
