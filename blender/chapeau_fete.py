"""
chapeau_fete.py — Chapeau pointu d'anniversaire (shop : `chapeau_fete`, 150 pièces).

Fiche suivie : docs/art-bible.md §6.3 et §6.5. Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/chapeau_fete.py

Un seul objet, nommé comme l'identifiant du shop. Origine = point d'accroche = sommet de la
tête (à poser sur `PointChapeau`, position et rotation locales à zéro).
L'inclinaison de 10° est DÉJÀ dans le modèle : ne pas la rajouter dans Unity.
Elle est faite autour du centre de la tête (0,5 sous l'origine) pour que le bord du chapeau
reste posé sur la tête au lieu de se soulever d'un côté.
"""

import math
import os
import sys

import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402

NOM = "chapeau_fete"
MATS = ["M_CartonRose", "M_CartonJaune", "M_Blanc"]  # indices 0, 1, 2
SEGMENTS = 16
BASE = -0.06            # le bord intérieur (rayon 0,27) touche la tête à cette hauteur
HAUTEUR = 0.55
RAYON = 0.30            # base Ø 0,60
EPAISSEUR = 0.03
INCLINAISON = 10.0      # degrés, vers la gauche de l'image (= -X dans Unity)


def rayon_a(z):
    return RAYON * (1 - (z - BASE) / HAUTEUR)


def construire():
    C.nouvelle_scene()
    C.graine(7)
    sommet = BASE + HAUTEUR
    # 4 rayures horizontales, de bas en haut : rose, jaune, rose, jaune.
    # Elles rétrécissent vers le haut pour paraître régulières vues de dessus.
    niveaux = [BASE, BASE + 0.16, BASE + 0.31, BASE + 0.44, sommet]
    profil = [(0.0, sommet)]                      # pointe extérieure
    for z in reversed(niveaux[1:-1]):
        profil.append((rayon_a(z), z))
    profil.append((RAYON, BASE))                  # bord extérieur
    profil.append((RAYON - EPAISSEUR, BASE))      # bord inférieur (3 cm visibles)
    profil.append((0.0, sommet - 0.08))           # pointe intérieure (creux)
    # un matériau par tronçon : jaune, rose, jaune, rose (du haut vers le bas), bord, dedans
    mats = [1, 0, 1, 0, 0, 0]
    bm = bmesh.new()
    C.bm_revolution(bm, profil, SEGMENTS, mats=mats, angle_depart=math.pi / 2)
    cone = C.objet_depuis_bmesh("Cone", bm, MATS)
    C.lisser_angle(cone, 30)

    # Pompon : icosphère bosselée (sommets décalés au hasard, graine fixe), Ø 0,20.
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=2, radius=0.10)
    for v in bm.verts:
        v.co *= 1.0 + random_bosse()
        v.co.z += sommet + 0.06
    for f in bm.faces:
        f.material_index = 2
    pompon = C.objet_depuis_bmesh("Pompon", bm, MATS)
    C.lisser_tout(pompon)

    chapeau = C.joindre([cone, pompon], NOM)
    # Inclinaison autour du centre de la tête (z = -0,5), vers +X Blender (= -X Unity,
    # comme la rotation (0 ; 0 ; 10°) de la spec du shop).
    pivot = Vector((0, 0, -0.5))
    m = Matrix.Translation(pivot) @ Matrix.Rotation(math.radians(INCLINAISON), 4, "Y") \
        @ Matrix.Translation(-pivot)
    chapeau.data.transform(m)
    return chapeau


def random_bosse():
    import random
    return random.uniform(-0.12, 0.12)


if __name__ == "__main__":
    racine = construire()
    C.rapport(racine)
    C.rendre_miniature(racine, f"blender_{NOM}.png")
    chemin = C.exporter_fbx(racine, f"{NOM}.fbx")
    C.verifier_fbx(chemin)
