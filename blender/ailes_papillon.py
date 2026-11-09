"""
ailes_papillon.py — Ailes de papillon (boîte d'accessoires n°1 : `ailes_papillon`, Super rare, Dos).

Fiche suivie : docs/specs/boites.md §5.6. Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/ailes_papillon.py

Hiérarchie produite (le code Unity anime les pièces mobiles, pas d'animation Blender) :
    ailes_papillon   objet vide, origine = point d'accroche Dos = `PointDos`,
                     Unity (0 ; 1,3 ; 0,5) local à Joueur : position et rotation locales à zéro
    ├── AileGauche   gauche de l'image (Unity x négatif) : grand lobe en haut + petit lobe en
    │                bas ; pivot = charnière contre le dos, position locale Unity
    │                (−0,30 ; −0,20 ; −0,083), rotation locale nulle
    └── AileDroite   symétrique, pivot (+0,30 ; −0,20 ; −0,083)
L'ouverture (30° vers l'arrière) est déjà dans le maillage : rotation des objets = 0.
Battement (code), en miroir : AileGauche.localRotation = Euler(0, +a, 0),
AileDroite.localRotation = Euler(0, −a, 0), a = 20° × sin(2π t / 0,9 s).

Chaque lobe = 3 plaques empilées : le bord sombre (forme entière, 4 cm), le turquoise (3 cm
plus petit tout autour, un peu plus épais : il dépasse devant et derrière) et, sur les
grands lobes, la tache orange (encore un peu plus épaisse). Pas de couleurs de sommets.
"""

import math
import os
import sys

import bmesh
from mathutils import Matrix

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402

NOM = "ailes_papillon"
EMPLACEMENT = "dos"
MATS = ["M_AilePapillon", "M_TachePapillon", "M_BordPapillon"]  # indices 0, 1, 2
OUVERTURE = 30.0         # degrés vers l'arrière
X_CHARNIERE = 0.30
Y_CHARNIERE = 1.1        # sous PointDos : vue d'en haut, l'aile remonte ; ainsi les petits
                         # lobes du bas restent visibles à côté du corps
Z_CHARNIERE = math.sqrt(C.rayon_corps(Y_CHARNIERE) ** 2 - X_CHARNIERE ** 2)  # contre le dos
BORD = 0.03
EPAISSEUR = 0.04

# Lobes dessinés pour l'aile qui part vers +u (u = vers l'extérieur, v = vers le haut) :
# (centre u, centre v, demi-largeur, demi-hauteur, inclinaison en degrés, tache ?)
# Fiche : grands 0,45 × 0,40, petits 0,30 × 0,25 ; agrandis (0,60 × 0,44 et 0,42 × 0,28)
# pour « dépasser largement des côtés » malgré l'ouverture vers l'arrière.
LOBES = [(0.33, 0.17, 0.30, 0.22, 20.0, True),
         (0.26, -0.14, 0.21, 0.14, -25.0, False)]


def ellipse_tournee(cu, cv, rx, rz, angle, n=16):
    a = math.radians(angle)
    pts = []
    for x, z in C.contour_ellipse(rx, rz, n=n):
        pts.append((cu + x * math.cos(a) - z * math.sin(a), cv + x * math.sin(a) + z * math.cos(a)))
    return pts


def aile(nom, cote):
    bm = bmesh.new()
    rot = Matrix.Rotation(math.radians(-cote * OUVERTURE), 4, "Z")
    for cu, cv, rx, rz, angle, tache in LOBES:
        couches = [(ellipse_tournee(cu, cv, rx, rz, angle), EPAISSEUR, 2),
                   (ellipse_tournee(cu, cv, rx - BORD, rz - BORD, angle), EPAISSEUR + 0.006, 0)]
        if tache:
            # Tache ronde orange Ø 0,15 vers le bout du grand lobe.
            a = math.radians(angle)
            tu, tv = cu + 0.09 * math.cos(a), cv + 0.09 * math.sin(a)
            couches.append((C.contour_ellipse(0.075, 0.075, n=12, cx=tu, cz=tv), EPAISSEUR + 0.012, 1))
        for contour, epaisseur, mat in couches:
            pts = [(cote * u, v) for u, v in contour]
            if cote < 0:
                pts.reverse()
            C.bm_plaque(bm, pts, epaisseur, rot, mat=mat)
    o = C.objet_depuis_bmesh(nom, bm, MATS)
    C.lisser_angle(o, 40)
    o.location = C.perso(-cote * X_CHARNIERE, Y_CHARNIERE, Z_CHARNIERE)
    return o


def construire():
    C.nouvelle_scene()
    gauche = aile("AileGauche", +1)   # Unity x négatif = gauche de l'image
    droite = aile("AileDroite", -1)
    return C.racine_accroche(NOM, EMPLACEMENT, [gauche, droite])


if __name__ == "__main__":
    C.finir_accessoire(construire(), NOM, EMPLACEMENT)
