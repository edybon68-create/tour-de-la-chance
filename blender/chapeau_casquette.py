"""
chapeau_casquette.py — Casquette rouge (shop : `chapeau_casquette`, 100 pièces).

Fiche suivie : docs/art-bible.md §6.3 et §6.4. Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/chapeau_casquette.py

Un seul objet, nommé comme l'identifiant du shop. Origine = point d'accroche = sommet de la
tête (à poser sur `PointChapeau` du personnage, position et rotation locales à zéro).
Le bas de la calotte descend sous l'origine (jusqu'à -0,17) pour coiffer la tête.
Avant (visière) = +Y dans Blender = -Z dans Unity.
"""

import math
import os
import sys

import bmesh
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402

NOM = "chapeau_casquette"
MATS = ["M_TissuRouge", "M_TissuRougeFonce", "M_Blanc"]  # indices 0, 1, 2
SEGMENTS = 18          # 18 = 6 coutures x 3 (la fiche dit 16 : 18 permet 6 coutures régulières)
BAS_CALOTTE = -0.10    # la calotte est une demi-sphère aplatie posée sur y = -0,10 (Unity)
HAUT_CALOTTE = 0.25    # ≈ 0,35 au-dessus de -0,10


def calotte():
    """Demi-sphère aplatie (Ø 1,0) + petit rebord rentrant qui épouse la tête.
    Une méridienne sur trois est légèrement bombée : ce sont les 6 coutures."""
    bm = bmesh.new()
    profil = []
    n = 5
    for k in range(n + 1):  # du sommet à l'équateur
        a = (math.pi / 2) * k / n
        profil.append((0.5 * math.sin(a), BAS_CALOTTE + (HAUT_CALOTTE - BAS_CALOTTE) * math.cos(a)))
    profil += [(0.42, -0.17), (0.0, -0.17)]  # rebord rentrant puis fond (caché dans la tête)

    def coutures(angle, i, r, z):
        s = round((angle - math.pi / 2) / (2 * math.pi / SEGMENTS)) % SEGMENTS
        if s % 3 == 0 and 0 < i <= n:
            return r * 1.025, z  # la couture ressort de ~1 cm
        return r, z

    anneaux = C.bm_revolution(bm, profil, SEGMENTS, mats=[0] * (len(profil) - 1),
                              angle_depart=math.pi / 2, rayon_fn=coutures)
    # Arêtes des coutures marquées « nettes » pour qu'elles accrochent la lumière.
    for i in range(1, n):
        for s in range(0, SEGMENTS, 3):
            a, b = anneaux[i][s], anneaux[i + 1][s]
            for e in a.link_edges:
                if e.other_vert(a) is b:
                    e.smooth = False
    return C.objet_depuis_bmesh("Calotte", bm, MATS)


def visiere():
    """Visière large vers l'avant (0,60 x 0,45, 5 cm d'épaisseur), courbée : les bords
    sont plus bas que le centre (4 segments en largeur), légèrement inclinée vers l'avant."""
    colonnes, rangs = 4, 2
    largeur = 0.30
    bm = bmesh.new()
    grille = []
    for j in range(rangs + 1):
        t = j / rangs  # 0 = arrière (dans la calotte), 1 = bord avant
        ligne = []
        for i in range(colonnes + 1):
            x = -largeur + 2 * largeur * i / colonnes
            u = x / largeur
            y_arriere = math.sqrt(0.45 ** 2 - x ** 2)
            y_avant = 0.45 + 0.45 * math.sqrt(1 - 0.75 * u * u)
            y = y_arriere + (y_avant - y_arriere) * t
            z = -0.085 - 0.07 * u * u - 0.04 * t
            ligne.append(bm.verts.new((x, y, z)))
        grille.append(ligne)
    for j in range(rangs):
        for i in range(colonnes):
            f = bm.faces.new((grille[j][i], grille[j][i + 1], grille[j + 1][i + 1], grille[j + 1][i]))
            f.material_index = 1
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    for f in bm.faces:  # normales vers le haut
        if f.normal.z < 0:
            f.normal_flip()
    o = C.objet_depuis_bmesh("Visiere", bm, MATS)
    epais = o.modifiers.new("Epaisseur", "SOLIDIFY")
    epais.thickness = 0.05
    epais.offset = 0.0
    C.biseau(o, 0.015, 1, 30)
    C.appliquer(o)
    return o


def construire():
    C.nouvelle_scene()
    c = calotte()
    C.lisser_tout(c)
    v = visiere()
    C.lisser_angle(v, 30)
    bm = bmesh.new()
    C.bm_sphere(bm, 0.05, (0, 0, HAUT_CALOTTE + 0.005), segments=6, anneaux=4,
                echelle=(1, 1, 0.6), mat=2)
    bouton = C.objet_depuis_bmesh("Bouton", bm, MATS)
    C.lisser_tout(bouton)
    chapeau = C.joindre([c, v, bouton], NOM)
    return chapeau


if __name__ == "__main__":
    racine = construire()
    C.rapport(racine)
    C.rendre_miniature(racine, f"blender_{NOM}.png")
    chemin = C.exporter_fbx(racine, f"{NOM}.fbx")
    C.verifier_fbx(chemin)
