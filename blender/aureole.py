"""
aureole.py — Auréole lumineuse (boîte d'accessoires n°1 : `aureole`, Légendaire, Tête).

Fiche suivie : docs/specs/boites.md §5.6 et art-bible.md §4.1 (émission 1,5). Lancer :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/aureole.py

Hiérarchie produite (le code Unity anime la pièce mobile, pas d'animation Blender) :
    aureole        objet vide, origine = point d'accroche Tête = `PointChapeau` (sommet de
                   la tête) : position et rotation locales à zéro
    └── Anneau     le tore, pivot AU CENTRE de l'anneau, position locale (0 ; 0,30 ; 0)
                   → flotte 30 cm au-dessus de la tête sans la toucher.
Animation prévue (code) : Anneau.localPosition.y = 0,30 + 0,04 × sin(2π t / 2 s) et rotation
autour de son axe Y local à 30°/s. L'épaisseur du tube ondule 6 fois sur le tour (±7 mm) :
sans ça, un anneau parfait qui tourne sur lui-même aurait l'air immobile.
"""

import math
import os
import sys

import bmesh

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402

NOM = "aureole"
EMPLACEMENT = "tete"
MATS = ["M_Aureole"]
HAUTEUR_FLOTTE = 0.30
R_TUBE = 0.04            # section de 8 cm
R_ANNEAU = 0.35 - R_TUBE  # Ø extérieur 0,70


def construire():
    C.nouvelle_scene()
    bm = bmesh.new()
    C.bm_tore(bm, R_ANNEAU, R_TUBE, segments=24, segments_tube=8, mat=0,
              rayon_fn=lambda a: R_TUBE + 0.007 * math.cos(6 * a))
    anneau = C.objet_depuis_bmesh("Anneau", bm, MATS)
    C.lisser_tout(anneau)
    # Pivot au centre de l'anneau, placé 0,30 au-dessus du sommet de la tête.
    anneau.location = C.perso(0, 2.0 + HAUTEUR_FLOTTE, 0)
    return C.racine_accroche(NOM, EMPLACEMENT, [anneau])


if __name__ == "__main__":
    C.finir_accessoire(construire(), NOM, EMPLACEMENT)
