"""
chapeau_haut_de_forme.py — Haut-de-forme de magicien (shop : `chapeau_haut_de_forme`, 250 pièces).

Fiche suivie : docs/art-bible.md §6.3 et §6.6. Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/chapeau_haut_de_forme.py

Un seul objet, nommé comme l'identifiant du shop. Origine = point d'accroche = sommet de la
tête (à poser sur `PointChapeau`, position et rotation locales à zéro). Le tube descend
jusqu'à -0,14 : c'est là que son bord (rayon 0,35) touche la tête.
Avant = +Y Blender = -Z Unity ; le petit nœud est sur le côté gauche de l'image (-X Unity).
"""

import math
import os
import sys

import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402

NOM = "chapeau_haut_de_forme"
MATS = ["M_Feutre", "M_Satin"]  # indices 0, 1
SEGMENTS = 24
BAS_TUBE, HAUT_TUBE = -0.14, 0.56      # hauteur 0,70
R_BAS, R_HAUT = 0.35, 0.38             # Ø 0,70 en bas, Ø 0,76 en haut (s'évase un peu)
BORD_R_INT, BORD_R_EXT = 0.33, 0.55    # bord Ø 1,10
BORD_HAUT, BORD_EP = -0.08, 0.05       # 5 cm d'épaisseur
RELEVE = 0.06                          # les côtés gauche/droit du bord remontent de 6 cm


def rayon_tube(z):
    return R_BAS + (R_HAUT - R_BAS) * (z - BAS_TUBE) / (HAUT_TUBE - BAS_TUBE)


def construire():
    C.nouvelle_scene()

    # --- Tube : dessus plat biseauté (2 cm) pour accrocher la lumière.
    bm = bmesh.new()
    C.bm_revolution(bm, [(R_HAUT, HAUT_TUBE), (R_BAS, BAS_TUBE)], SEGMENTS,
                    mats=[0], fermer_haut=True)
    tube = C.objet_depuis_bmesh("Tube", bm, MATS)
    C.biseau(tube, 0.02, 1, 30)
    C.appliquer(tube)

    # --- Bord : anneau épais dont les côtés (axe X) remontent en courbe douce.
    def anneau(r, z, releve):
        pts = []
        for s in range(SEGMENTS):
            a = math.pi / 2 + 2 * math.pi * s / SEGMENTS
            dz = RELEVE * math.cos(a) ** 2 if releve else 0.0
            pts.append((r * math.cos(a), r * math.sin(a), z + dz))
        return pts
    bm = bmesh.new()
    # dessus (de l'intérieur vers l'extérieur), tranche extérieure, dessous.
    # Le côté intérieur n'est pas fermé : il est caché dans le tube.
    rangs = C.bm_anneaux(bm, [anneau(BORD_R_INT, BORD_HAUT, False),
                      anneau(BORD_R_EXT, BORD_HAUT, True),
                      anneau(BORD_R_EXT, BORD_HAUT - BORD_EP, True),
                      anneau(BORD_R_INT, BORD_HAUT - BORD_EP, False)])
    # Les normales sont cohérentes entre elles ; on vérifie juste que le dessus regarde
    # vers le haut, sinon on retourne tout.
    # (face du dessus = celle qui relie l'anneau intérieur haut à l'anneau extérieur haut)
    dessus = next(f for f in rangs[0][0].link_faces if rangs[1][0] in f.verts)
    if dessus.normal.z < 0:
        bmesh.ops.reverse_faces(bm, faces=bm.faces)
    bord = C.objet_depuis_bmesh("Bord", bm, MATS)

    # --- Ruban rouge satiné : 10 cm de haut, 1,5 cm de relief, à la base du tube.
    z0, z1 = BORD_HAUT, BORD_HAUT + 0.10
    bm = bmesh.new()
    C.bm_revolution(bm, [(rayon_tube(z1) - 0.003, z1), (rayon_tube(z1) + 0.015, z1),
                         (rayon_tube(z0) + 0.015, z0)], SEGMENTS, mats=[1, 1])
    ruban = C.objet_depuis_bmesh("Ruban", bm, MATS)

    # --- Petit nœud plat sur le côté gauche de l'image (+X Blender = -X Unity).
    bm = bmesh.new()
    xc = rayon_tube((z0 + z1) / 2) + 0.025
    zc = (z0 + z1) / 2
    C.bm_boite(bm, (0.03, 0.05, 0.07), (xc + 0.005, 0, zc), mat=1)  # le centre du nœud
    for sy in (-1, 1):  # les deux boucles, légèrement en éventail
        verts = C.bm_boite(bm, (0.025, 0.10, 0.075), (0, 0, 0), mat=1)
        m = Matrix.Translation((xc, sy * 0.065, zc)) @ Matrix.Rotation(math.radians(sy * 15), 4, "X")
        bmesh.ops.transform(bm, matrix=m, verts=verts)
    noeud = C.objet_depuis_bmesh("Noeud", bm, MATS)

    chapeau = C.joindre([tube, bord, ruban, noeud], NOM)
    C.lisser_angle(chapeau, 30)
    return chapeau


if __name__ == "__main__":
    racine = construire()
    C.rapport(racine)
    C.rendre_miniature(racine, f"blender_{NOM}.png")
    chemin = C.exporter_fbx(racine, f"{NOM}.fbx")
    C.verifier_fbx(chemin)
