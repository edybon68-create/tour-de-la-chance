"""
ailes_ange.py — Petites ailes d'ange (boîte d'accessoires n°1 : `ailes_ange`, Rare, Dos).

Fiche suivie : docs/specs/boites.md §5.6. Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/ailes_ange.py

Hiérarchie produite (le code Unity anime les pièces mobiles, pas d'animation Blender) :
    ailes_ange       objet vide, origine = point d'accroche Dos = `PointDos`,
                     Unity (0 ; 1,3 ; 0,5) local à Joueur : position et rotation locales à zéro
    ├── AileGauche   aile à GAUCHE de l'image (Unity x négatif) ; pivot = charnière collée au
    │                dos, position locale Unity (−0,32 ; −0,15 ; −0,100), rotation locale nulle
    └── AileDroite   symétrique, pivot (+0,32 ; −0,15 ; −0,100)
L'orientation (35° vers l'extérieur ET l'arrière) est déjà dans le maillage : rotation des
objets = 0. Battement (code) : AileGauche.localRotation = Euler(0, +a, 0) et
AileDroite.localRotation = Euler(0, −a, 0) avec a = 8° × sin(2π t / 1,6 s).
Les charnières sont écartées à ±0,32 (et non au milieu du dos) pour que les ailes dépassent
nettement des côtés du corps vu de face : c'est la seule partie que la caméra voit.

Deux couches par aile : les grandes plumes (ombre `#D9D2E4`, bord festonné en 3 plumes) et,
1 cm devant, le « manteau » blanc cassé du haut de l'aile, festonné lui aussi.
"""

import math
import os
import sys

import bmesh
from mathutils import Matrix

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402

NOM = "ailes_ange"
EMPLACEMENT = "dos"
MATS = ["M_Plume", "M_PlumeOmbre"]  # indices 0, 1
ORIENTATION = 35.0       # degrés vers l'arrière
X_CHARNIERE = 0.32
Y_CHARNIERE = 1.15       # 15 cm sous PointDos : vues de la caméra (au-dessus), elles
                         # remontent à hauteur d'épaules au lieu de ressembler à des oreilles
Z_CHARNIERE = math.sqrt(C.rayon_corps(Y_CHARNIERE) ** 2 - X_CHARNIERE ** 2)  # contre le dos

# Contours dessinés pour l'aile qui part vers +u (u = vers l'extérieur, v = vers le haut).
# Grandes plumes : 0,56 de long × 0,38 de haut (fiche : 0,50 × 0,35, un peu grossies pour
# dépasser du corps), bord inférieur en 3 plumes arrondies, pointe vers le haut et l'extérieur.
PLUMES = [(0.00, 0.10), (0.10, 0.17), (0.22, 0.23), (0.34, 0.27), (0.46, 0.29), (0.53, 0.27),
          (0.56, 0.22), (0.54, 0.14), (0.51, 0.06), (0.47, 0.00), (0.42, 0.02), (0.40, 0.06),
          (0.37, -0.02), (0.32, -0.07), (0.27, -0.05), (0.25, 0.00), (0.22, -0.06), (0.16, -0.09),
          (0.10, -0.07), (0.05, -0.02), (0.01, 0.03)]
# Manteau (haut de l'aile, blanc cassé), un peu plus grand en haut, festonné en bas.
MANTEAU = [(0.00, 0.11), (0.10, 0.18), (0.22, 0.24), (0.34, 0.28), (0.46, 0.30), (0.535, 0.28),
           (0.57, 0.225), (0.545, 0.16), (0.49, 0.13), (0.44, 0.15), (0.39, 0.10), (0.33, 0.085),
           (0.28, 0.11), (0.24, 0.07), (0.18, 0.045), (0.12, 0.06), (0.06, 0.02), (0.00, 0.03)]


def aile(nom, cote):
    """cote = +1 : aile vers +X Blender (gauche de l'image) ; -1 : vers -X (droite)."""
    bm = bmesh.new()
    rot = Matrix.Rotation(math.radians(-cote * ORIENTATION), 4, "Z")
    for contour, epaisseur, avance, mat in ((PLUMES, 0.05, -0.008, 1), (MANTEAU, 0.05, 0.012, 0)):
        pts = [(cote * u, v) for u, v in contour]
        if cote < 0:
            pts.reverse()
        C.bm_plaque(bm, pts, epaisseur, rot @ Matrix.Translation((0, avance, 0)), mat=mat)
    o = C.objet_depuis_bmesh(nom, bm, MATS)
    C.biseau(o, largeur=0.012, segments=1, angle=40)
    C.appliquer(o)
    C.lisser_angle(o, 40)
    # Pivot = charnière : le maillage est construit autour de l'origine, on place l'objet.
    o.location = C.perso(-cote * X_CHARNIERE, Y_CHARNIERE, Z_CHARNIERE)
    return o


def construire():
    C.nouvelle_scene()
    gauche = aile("AileGauche", +1)   # Unity x négatif = gauche de l'image
    droite = aile("AileDroite", -1)
    return C.racine_accroche(NOM, EMPLACEMENT, [gauche, droite])


if __name__ == "__main__":
    C.finir_accessoire(construire(), NOM, EMPLACEMENT)
