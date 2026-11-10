"""
masque_heros.py — Masque de héros (boîte d'accessoires n°1 : `masque_heros`, Rare, Visage).

Fiche suivie : docs/specs/boites.md §5.6. Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/masque_heros.py

VERSION CHEVAL (personnage « cavalier d'échecs », blender/personnage_cheval.py).
Un seul objet, nommé comme l'identifiant. Origine = point d'accroche Visage = `PointVisage`
du cheval, Unity (0 ; 1,68 ; −0,253) local à Joueur : position et rotation locales à zéro,
échelle 1. Sortie : blender/exports/masque_heros.fbx (en attente d'intégration).

Comment il est construit (les trous ne se font pas avec des primitives) :
  1. on dessine le masque À PLAT : un bandeau de 0,80 × 0,30 percé de deux ovales
     (0,21 × 0,25, un peu plus grands que les yeux du cheval), rempli de triangles par
     Blender (`triangle_fill`) ;
  2. on « enroule » ce dessin sur la tête du cheval, comme une décalcomanie : chaque point
     (s, h) du dessin est projeté sur la VRAIE surface de la tête par un rayon
     (commun.sur_cheval), puis décollé de 1,5 cm. Le bas du masque se pose donc sur le
     chanfrein (le haut du museau) au lieu de s'y enfoncer ;
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
POINT = "PointVisage"
MATS = ["M_MasqueBleu", "M_MasqueBord"]  # indices 0, 1
LARGEUR, HAUTEUR = 0.80, 0.30
R_ENROULE = 0.30         # « rayon » de la tête du cheval : abscisse s -> angle s / R
ECART = 0.015            # le masque flotte 1,5 cm devant la peau
EPAISSEUR = 0.035
TROU_L, TROU_H = 0.21, 0.25
Y_MASQUE = C.CHEVAL_YEUX_Y + 0.01   # hauteur du milieu du bandeau (Unity)
ARBRE = None             # forme de la tête du cheval (chargée dans construire())


def sur_tete(s, h, ecart):
    """Enroulement : point (s, h) du dessin à plat -> point sur la tête du cheval.
    s = abscisse le long du bandeau (0 au milieu), h = hauteur (0 = milieu du bandeau)."""
    p, _ = C.sur_cheval(ARBRE, s / R_ENROULE, Y_MASQUE + h, ecart)
    return p


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
    global ARBRE
    ARBRE = C.arbre_cheval()
    # Abscisse des trous : l'angle sous lequel on voit l'œil depuis l'axe de la tête.
    recul = C.CHEVAL_CRANE_CENTRE[2] - C.CHEVAL_YEUX_AVANT
    x_trou = R_ENROULE * math.atan2(C.CHEVAL_YEUX_X, recul)
    h_trou = C.CHEVAL_YEUX_Y - Y_MASQUE
    for cote in (-1, 1):
        boucles.append(C.contour_ellipse(TROU_L / 2, TROU_H / 2, n=14, cx=cote * x_trou,
                                         cz=h_trou))
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
            if ((c.x - cote * x_trou) / (TROU_L / 2)) ** 2 + ((c.y - h_trou) / (TROU_H / 2)) ** 2 < 1:
                bm.faces.remove(f)
                break
    # On recoupe chaque triangle en 4 : avec des triangles plus petits, le masque suit mieux
    # le creux entre le front et le museau (sinon de grands triangles plats y traversent).
    bmesh.ops.subdivide_edges(bm, edges=bm.edges[:], cuts=1, use_grid_fill=True)
    bmesh.ops.triangulate(bm, faces=bm.faces[:])
    # Enroulement sur la tête du cheval.
    for v in bm.verts:
        v.co = sur_tete(v.co.x, v.co.y, ECART)
    # Normales vers l'extérieur (loin de l'axe de la tête).
    centre = C.depuis_unity(*C.CHEVAL_CRANE_CENTRE)
    for f in bm.faces:
        f.material_index = 0
        dehors = f.calc_center_median() - centre
        dehors.z = 0
        if f.normal.dot(dehors) < 0:
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
    ecart = ECART + EPAISSEUR / 2
    for cote in (-1, 1):
        s_bout = cote * (LARGEUR / 2 + 0.01)
        C.bm_sphere(bm, 0.045, sur_tete(s_bout, 0.0, ecart), segments=8, anneaux=5, mat=0)
        for pente in (0.02, -0.03):
            chemin = [sur_tete(s_bout, 0.0, ecart)]
            for k in (1, 2, 3):
                chemin.append(sur_tete(s_bout + cote * 0.05 * k, pente * k, ecart + 0.012 * k))
            C.bm_balayage(bm, chemin, section, mat=0)
    pans = C.objet_depuis_bmesh("Pans", bm, MATS)
    obj = C.joindre([obj, pans], NOM)
    C.lisser_angle(obj, 45)
    return C.vers_accroche_cheval(obj, POINT)


if __name__ == "__main__":
    C.finir_accessoire_cheval(construire(), NOM, POINT)
