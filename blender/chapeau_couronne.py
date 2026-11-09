"""
chapeau_couronne.py — Couronne royale en or (shop : `chapeau_couronne`, 500 pièces).

Fiche suivie : docs/art-bible.md §6.3 et §6.7. Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/chapeau_couronne.py

Un seul objet, nommé comme l'identifiant du shop. Origine = point d'accroche = sommet de la
tête (à poser sur `PointChapeau`, position et rotation locales à zéro). L'anneau descend à
-0,15 : c'est la hauteur où son bord intérieur (rayon 0,36) touche la tête.
Avant (rubis, première pointe) = +Y Blender = -Z Unity.
"""

import math
import os
import sys

import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402

NOM = "chapeau_couronne"
MATS = ["M_Or", "M_Rubis"]  # indices 0, 1
POINTES = 5
PAR_POINTE = 4                         # segments par pointe : pointe, flanc, creux, flanc
SEGMENTS = POINTES * PAR_POINTE        # 20 segments autour
R_EXT, R_INT = 0.40, 0.36              # Ø 0,80, paroi de 4 cm
BOURRELET = 0.025                      # relief des bourrelets haut et bas
BAS, HAUT_ANNEAU = -0.15, 0.10         # anneau de 0,25 de haut
H_POINTE = 0.13                        # pointes de 0,13 au-dessus d'un liseré de 3 cm : sommet à 0,26
# hauteur du bord supérieur selon la position dans la pointe : pointe, flanc, creux, flanc
PROFIL_POINTE = [1.0, 0.5, 0.0, 0.5]


def anneau(r, z, avec_pointes=False):
    """Un anneau de SEGMENTS sommets. Le premier sommet est pile devant (+Y Blender)."""
    pts = []
    for s in range(SEGMENTS):
        a = math.pi / 2 + 2 * math.pi * s / SEGMENTS
        zz = z + (H_POINTE * PROFIL_POINTE[s % PAR_POINTE] if avec_pointes else 0.0)
        pts.append((r * math.cos(a), r * math.sin(a), zz))
    return pts


def construire():
    C.nouvelle_scene()

    # --- Anneau + bourrelets + pointes, en une seule surface fermée.
    # On décrit la « coupe » de la paroi par une suite d'anneaux, en faisant le tour :
    # bas intérieur -> bas extérieur (bourrelet) -> paroi -> bourrelet haut -> pointes ->
    # haut intérieur -> retour en bas par l'intérieur.
    re = R_EXT + BOURRELET
    coupe = [
        anneau(R_INT, BAS),
        anneau(re, BAS),
        anneau(re, BAS + 0.045),
        anneau(R_EXT, BAS + 0.05),
        anneau(R_EXT, HAUT_ANNEAU - 0.05),
        anneau(re, HAUT_ANNEAU - 0.045),
        anneau(re, HAUT_ANNEAU),
        anneau(R_EXT, HAUT_ANNEAU),
        anneau(R_EXT, HAUT_ANNEAU + 0.03, avec_pointes=True),
        anneau(R_INT, HAUT_ANNEAU + 0.03, avec_pointes=True),
        anneau(R_INT, BAS),
    ]
    bm = bmesh.new()
    rangs = C.bm_anneaux(bm, coupe, mats=[0] * len(coupe))
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    # Biseau à 2 segments seulement sur le bord extérieur des pointes (là où l'or
    # scintille, vu de la caméra) ; le bord intérieur reste net pour tenir le budget.
    # On marque ces arêtes avec un « poids de biseau ».
    poids = bm.edges.layers.float.get("bevel_weight_edge") or \
        bm.edges.layers.float.new("bevel_weight_edge")
    haut_ext = set(rangs[8])
    for e in bm.edges:
        a, b = e.verts
        if a in haut_ext and b in haut_ext:
            e[poids] = 1.0
    bande = C.objet_depuis_bmesh("Bande", bm, MATS)
    C.biseau(bande, 0.02, 2, poids=True)
    C.appliquer(bande)
    C.lisser_angle(bande, 30)

    # --- Perles au bout des pointes (Ø 0,13), la première pile devant.
    bm = bmesh.new()
    r_milieu = (R_EXT + R_INT) / 2
    for k in range(POINTES):
        a = math.pi / 2 + 2 * math.pi * k / POINTES
        C.bm_sphere(bm, 0.065, (r_milieu * math.cos(a), r_milieu * math.sin(a),
                                HAUT_ANNEAU + 0.03 + H_POINTE + 0.03), segments=8, anneaux=4, mat=0)
    perles = C.objet_depuis_bmesh("Perles", bm, MATS)
    C.lisser_tout(perles)

    # --- Rubis taillé : double pyramide à 8 côtés (16 facettes), Ø 0,12, serti devant.
    bm = bmesh.new()
    zc = (BAS + HAUT_ANNEAU) / 2
    yc = R_EXT + 0.01
    ceinture = [bm.verts.new((0.06 * math.cos(2 * math.pi * k / 8), yc,
                              zc + 0.06 * math.sin(2 * math.pi * k / 8))) for k in range(8)]
    avant = bm.verts.new((0, yc + 0.045, zc))
    arriere = bm.verts.new((0, yc - 0.03, zc))
    for k in range(8):
        j = (k + 1) % 8
        bm.faces.new((ceinture[k], ceinture[j], avant)).material_index = 1
        bm.faces.new((ceinture[j], ceinture[k], arriere)).material_index = 1
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    rubis = C.objet_depuis_bmesh("Rubis", bm, MATS)  # facettes nettes (pas de lissage)

    return C.joindre([bande, perles, rubis], NOM)


if __name__ == "__main__":
    racine = construire()
    C.rapport(racine)
    C.rendre_miniature(racine, f"blender_{NOM}.png")
    chemin = C.exporter_fbx(racine, f"{NOM}.fbx")
    C.verifier_fbx(chemin)
