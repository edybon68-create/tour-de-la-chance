"""
masque_heros.py — Masque de héros (boîte d'accessoires n°1 : `masque_heros`, Rare, Visage).

Fiche suivie : docs/specs/boites.md §5.6. Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/masque_heros.py

Un seul objet, nommé comme l'identifiant. Origine = point d'accroche Visage = `PointVisage`,
Unity (0 ; 1,6 ; −0,49) local à Joueur : position et rotation locales à zéro.

Comment il est construit (les trous ne se font pas avec des primitives) :
  1. on dessine le masque À PLAT : un bandeau de 0,95 × 0,22 percé de deux ovales
     (0,20 × 0,17), rempli de triangles par Blender (`triangle_fill`) ;
  2. on « enroule » ce dessin sur une sphère un peu plus grande que la tête (rayon 0,52),
     comme une décalcomanie : chaque point (s, h) du dessin devient un point de la sphère ;
  3. un modificateur Solidify lui donne 4 cm d'épaisseur ; sa tranche (le bord, y compris
     le tour des trous) reçoit le bleu foncé.
Les trous tombent exactement autour des yeux : on les voit à travers.
"""

import math
import os
import sys

import bmesh
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402

NOM = "masque_heros"
EMPLACEMENT = "visage"
MATS = ["M_MasqueBleu", "M_MasqueBord"]  # indices 0, 1
LARGEUR, HAUTEUR = 0.95, 0.22
R_ENROULE = 0.49         # rayon horizontal de la tête à hauteur des yeux (s -> angle)
R_INTERIEUR = 0.52       # le masque flotte 2 cm devant la peau (les yeux ressortent à 0,53)
EPAISSEUR = 0.04
TROU_L, TROU_H = 0.20, 0.17
LAT0 = math.asin((1.6 - C.TETE_CENTRE_Y) / C.TETE_RAYON)   # latitude des yeux


def sur_tete(s, h, rayon):
    """Enroulement : point (s, h) du dessin à plat -> point de la sphère (repère personnage).
    s = abscisse le long du bandeau (0 au milieu), h = hauteur (0 = hauteur des yeux)."""
    phi = s / R_ENROULE
    lat = LAT0 + h / C.TETE_RAYON
    d = Vector((math.sin(phi) * math.cos(lat), math.cos(phi) * math.cos(lat), math.sin(lat)))
    return C.perso(0, C.TETE_CENTRE_Y, 0) + d * rayon


def contour_bandeau(n=16):
    """Bord extérieur du masque à plat : haut presque droit (un peu relevé aux bouts),
    petite encoche en bas au milieu (le « nez »), bouts arrondis."""
    demi = LARGEUR / 2
    haut, bas = [], []
    for k in range(n + 1):
        s = -demi + LARGEUR * k / n
        u = s / demi
        haut.append((s, HAUTEUR / 2 + 0.015 * u ** 4))
        bas.append((s, -HAUTEUR / 2 + 0.035 * math.exp(-(s / 0.07) ** 2) + 0.03 * u ** 4))
    pts = bas + [(demi + 0.012, 0.0)] + list(reversed(haut)) + [(-demi - 0.012, 0.0)]
    return pts


def construire():
    C.nouvelle_scene()
    bm = bmesh.new()
    boucles = [contour_bandeau()]
    x_trou = R_ENROULE * math.atan2(0.18, math.sqrt(0.25 - 0.18 ** 2 - 0.1 ** 2))
    for cote in (-1, 1):
        boucles.append(C.contour_ellipse(TROU_L / 2, TROU_H / 2, n=14, cx=cote * x_trou))
    aretes = []
    for boucle in boucles:
        vs = [bm.verts.new((s, h, 0)) for s, h in boucle]
        for i in range(len(vs)):
            aretes.append(bm.edges.new((vs[i], vs[(i + 1) % len(vs)])))
    bmesh.ops.triangle_fill(bm, use_beauty=True, use_dissolve=False, edges=aretes,
                            normal=(0, 0, 1))
    # Sécurité : si le remplissage a bouché les trous, on retire les triangles dedans.
    for f in list(bm.faces):
        c = f.calc_center_median()
        for cote in (-1, 1):
            if ((c.x - cote * x_trou) / (TROU_L / 2)) ** 2 + (c.y / (TROU_H / 2)) ** 2 < 1:
                bm.faces.remove(f)
                break
    # Enroulement sur la sphère.
    for v in bm.verts:
        v.co = sur_tete(v.co.x, v.co.y, R_INTERIEUR)
    # Normales vers l'extérieur (loin du centre de la tête).
    centre = C.perso(0, C.TETE_CENTRE_Y, 0)
    for f in bm.faces:
        f.material_index = 0
        if f.normal.dot(f.calc_center_median() - centre) < 0:
            f.normal_flip()

    obj = C.objet_depuis_bmesh("Bandeau", bm, MATS)
    epais = obj.modifiers.new("Epaisseur", "SOLIDIFY")
    epais.thickness = EPAISSEUR
    epais.offset = 1.0                 # l'épaisseur pousse vers l'extérieur (sens des normales)
    epais.material_offset_rim = 1      # la tranche (bords et tour des trous) en bleu foncé
    epais.use_rim = True
    epais.use_even_offset = True
    C.appliquer(obj)

    # Pans noués aux deux bouts : un petit nœud et deux rubans de 0,15 qui filent vers
    # l'arrière en s'écartant (un vers le haut, un vers le bas). Déjà épais : pas de Solidify.
    bm = bmesh.new()
    section = [(-0.02, -0.03), (0.02, -0.03), (0.02, 0.03), (-0.02, 0.03)]  # 4 × 6 cm
    rayon = R_INTERIEUR + EPAISSEUR / 2
    for cote in (-1, 1):
        s_bout = cote * (LARGEUR / 2 + 0.01)
        C.bm_sphere(bm, 0.045, sur_tete(s_bout, 0.0, rayon), segments=8, anneaux=5, mat=0)
        for pente in (0.02, -0.03):
            chemin = [sur_tete(s_bout, 0.0, rayon)]
            for k in (1, 2, 3):
                chemin.append(sur_tete(s_bout + cote * 0.05 * k, pente * k, rayon + 0.012 * k))
            C.bm_balayage(bm, chemin, section, mat=0)
    pans = C.objet_depuis_bmesh("Pans", bm, MATS)
    obj = C.joindre([obj, pans], NOM)
    C.lisser_angle(obj, 45)
    return C.vers_accroche(obj, EMPLACEMENT)


if __name__ == "__main__":
    C.finir_accessoire(construire(), NOM, EMPLACEMENT)
