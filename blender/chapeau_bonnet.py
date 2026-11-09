"""
chapeau_bonnet.py — Bonnet de laine (boîte d'accessoires n°1 : `chapeau_bonnet`, Commun, Tête).

Fiche suivie : docs/specs/boites.md §5.6 (et art-bible.md §2.8, §3). Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/chapeau_bonnet.py

Un seul objet, nommé comme l'identifiant. Origine = point d'accroche Tête = `PointChapeau`
(sommet de la tête, Unity (0 ; 2,0 ; 0) local à Joueur) : position et rotation locales à zéro.
Le revers descend jusqu'à -0,21 : c'est là que son bord intérieur (rayon 0,40) touche la tête.
"""

import math
import os
import random
import sys

import bmesh

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402

NOM = "chapeau_bonnet"
EMPLACEMENT = "tete"
MATS = ["M_LaineTurquoise", "M_LaineBlanche"]  # indices 0, 1
SEGMENTS = 20           # 20 côtés : assez rond une fois lissé, 10 côtes sur le revers
BAS_DOME = -0.10        # la calotte est posée sur y = -0,10 (fiche)
HAUT_DOME = 0.35        # 0,45 de haut au total
RAYON_DOME = 0.475      # Ø 0,95


def dome():
    """Calotte : demi-ellipsoïde douce, lissée (la laine n'a pas d'arêtes)."""
    bm = bmesh.new()
    n = 5
    profil = []
    for k in range(n + 1):  # du sommet (pôle) à l'équateur
        a = (math.pi / 2) * k / n
        profil.append((RAYON_DOME * math.sin(a), BAS_DOME + (HAUT_DOME - BAS_DOME) * math.cos(a)))
    # Fond plat caché dans le revers (on ne le voit jamais).
    C.bm_revolution(bm, profil, SEGMENTS, mats=[0] * n, fermer_bas=True)
    o = C.objet_depuis_bmesh("Dome", bm, MATS)
    C.lisser_tout(o)
    return o


def revers():
    """Revers : gros anneau de laine blanc cassé de 10 cm de haut (Ø 1,0), avec des côtes
    verticales (une colonne de sommets sur deux ressort de 1 cm)."""
    bm = bmesh.new()
    # Section de l'anneau (rayon, hauteur), fermée : bord intérieur caché contre la tête.
    profil = [(0.40, -0.21), (0.48, -0.215), (0.505, -0.18), (0.505, -0.12),
              (0.48, -0.085), (0.43, -0.085), (0.40, -0.21)]

    def cotes(angle, i, r, z):
        s = round(angle / (2 * math.pi / SEGMENTS)) % SEGMENTS
        if s % 2 == 0 and i in (2, 3):
            return r + 0.012, z
        return r, z

    C.bm_revolution(bm, profil, SEGMENTS, mats=[1] * (len(profil) - 1), rayon_fn=cotes)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
    o = C.objet_depuis_bmesh("Revers", bm, MATS)
    C.lisser_angle(o, 50)
    return o


def pompon():
    """Gros pompon bosselé Ø 0,25 au sommet (sommets décalés au hasard, graine fixe)."""
    bm = bmesh.new()
    C.bm_sphere(bm, 0.125, (0, 0, 0), segments=10, anneaux=6, mat=1)
    for v in bm.verts:
        v.co *= 1.0 + random.uniform(-0.12, 0.12)
        v.co.z += 0.42
    o = C.objet_depuis_bmesh("Pompon", bm, MATS)
    C.lisser_tout(o)
    return o


def construire():
    C.nouvelle_scene()
    C.graine(11)
    return C.joindre([dome(), revers(), pompon()], NOM)


if __name__ == "__main__":
    C.finir_accessoire(construire(), NOM, EMPLACEMENT)
