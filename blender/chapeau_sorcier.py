"""
chapeau_sorcier.py — Chapeau de sorcier (boîte d'accessoires n°1 : `chapeau_sorcier`, Rare, Tête).

Fiche suivie : docs/specs/boites.md §5.6. Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/chapeau_sorcier.py

Un seul objet, nommé comme l'identifiant. Origine = point d'accroche Tête = `PointChapeau`
(sommet de la tête) : position et rotation locales à zéro.
  - bord Ø 1,2, 5 cm d'épaisseur, centré sur y = −0,05, légèrement ondulé ;
  - cône de 0,75 (base Ø 0,7) construit anneau par anneau le long d'une « colonne » : droite
    sur les deux premiers tiers, puis qui se plie vers l'arrière (pointe tordue) ;
  - ruban jaune de 8 cm à la base du cône, petite étoile à 5 branches devant.
"""

import math
import os
import sys

import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402

NOM = "chapeau_sorcier"
EMPLACEMENT = "tete"
MATS = ["M_FeutreViolet", "M_RubanJaune"]  # indices 0, 1
BASE_CONE = -0.025       # dessus du bord
HAUT_CONE = 0.75
R_CONE = 0.35
SEG_CONE = 16
ANNEAUX = 9
PLI_DEBUT = 0.6          # le pli commence aux 3/5 de la hauteur…
PLI_FINAL = 50.0         # … et la pointe finit penchée de 50° (≈ 30° en moyenne sur le tiers)


def bord():
    bm = bmesh.new()
    profil = [(0.27, -0.025), (0.58, -0.03), (0.60, -0.05), (0.58, -0.072), (0.27, -0.075),
              (0.27, -0.025)]

    def ondule(angle, i, r, z):
        poids = max(0.0, (r - 0.30) / 0.30)          # le centre ne bouge pas, le bord ondule
        return r, z + 0.025 * poids * math.sin(4 * angle + 0.6)

    C.bm_revolution(bm, profil, 24, mats=[0] * (len(profil) - 1), rayon_fn=ondule)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
    o = C.objet_depuis_bmesh("Bord", bm, MATS)
    C.lisser_angle(o, 50)
    return o


def colonne():
    """Centres et directions des anneaux du cône (repère Blender, arrière = -Y)."""
    pas = HAUT_CONE / ANNEAUX
    p = Vector((0, 0, BASE_CONE))
    res = []
    for k in range(ANNEAUX + 1):
        t = k / ANNEAUX
        alpha = 0.0 if t <= PLI_DEBUT else math.radians(PLI_FINAL) * (t - PLI_DEBUT) / (1 - PLI_DEBUT)
        # Vers l'arrière (-Y), avec un peu de côté (vers la droite de l'image, -X Blender) :
        # un pli purement vers l'arrière serait invisible depuis la caméra, qui est devant.
        direction = Vector((-0.45 * math.sin(alpha), -0.9 * math.sin(alpha), math.cos(alpha)))
        direction.normalize()
        res.append((p.copy(), direction, t))
        p += direction * pas
    return res


def cone():
    bm = bmesh.new()
    anneaux = []
    for centre, direction, t in colonne():
        if t >= 1.0:
            anneaux.append([bm.verts.new(centre)])
            continue
        r = R_CONE * (1 - t) ** 0.95
        # Deux axes perpendiculaires à la colonne pour dessiner l'anneau.
        ax = (Vector((1, 0, 0)) - direction * direction.x).normalized()
        ay = direction.cross(ax)
        anneau = []
        for s in range(SEG_CONE):
            a = math.pi / 2 + 2 * math.pi * s / SEG_CONE
            anneau.append(bm.verts.new(centre + ax * (r * math.cos(a)) + ay * (r * math.sin(a))))
        anneaux.append(anneau)
    faces = [bm.faces.new(anneaux[0])]
    for i in range(len(anneaux) - 1):
        A, B = anneaux[i], anneaux[i + 1]
        for s in range(SEG_CONE):
            t = (s + 1) % SEG_CONE
            if len(B) == 1:
                faces.append(bm.faces.new((A[s], A[t], B[0])))
            else:
                faces.append(bm.faces.new((A[s], A[t], B[t], B[s])))
    bmesh.ops.recalc_face_normals(bm, faces=faces)
    o = C.objet_depuis_bmesh("Cone", bm, MATS)
    C.lisser_tout(o)
    return o


def ruban():
    """Bande jaune de 8 cm, 1,5 cm plus large que le cône, à sa base."""
    bm = bmesh.new()
    r_haut = R_CONE * (1 - 0.08 / HAUT_CONE)
    profil = [(R_CONE + 0.015, BASE_CONE), (r_haut + 0.015, BASE_CONE + 0.08),
              (r_haut - 0.03, BASE_CONE + 0.08), (R_CONE - 0.03, BASE_CONE),
              (R_CONE + 0.015, BASE_CONE)]
    C.bm_revolution(bm, profil, SEG_CONE, mats=[1] * 4, angle_depart=math.pi / 2)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
    o = C.objet_depuis_bmesh("Ruban", bm, MATS)
    C.lisser_angle(o, 40)
    return o


def etoile():
    """Étoile à 5 branches Ø 0,18 (fiche : 0,14, grossie pour se lire de loin), 4 cm d'épaisseur, posée devant sur le ruban et penchée
    comme la pente du cône."""
    bm = bmesh.new()
    contour = []
    for k in range(10):
        a = math.pi / 2 + 2 * math.pi * k / 10
        r = 0.09 if k % 2 == 0 else 0.04
        contour.append((r * math.cos(a), r * math.sin(a)))
    pente = math.atan2(R_CONE, HAUT_CONE)
    z = BASE_CONE + 0.045
    rayon = R_CONE * (1 - 0.04 / HAUT_CONE) + 0.015 + 0.02
    avant = Vector((0, math.cos(pente), math.sin(pente)))
    C.bm_plaque(bm, contour, 0.04, C.repere(Vector((0, rayon, z)), avant), mat=1)
    o = C.objet_depuis_bmesh("Etoile", bm, MATS)
    C.biseau(o, largeur=0.008, segments=1, angle=40)
    C.appliquer(o)
    C.lisser_angle(o, 40)
    return o


def construire():
    C.nouvelle_scene()
    return C.joindre([bord(), cone(), ruban(), etoile()], NOM)


if __name__ == "__main__":
    C.finir_accessoire(construire(), NOM, EMPLACEMENT)
