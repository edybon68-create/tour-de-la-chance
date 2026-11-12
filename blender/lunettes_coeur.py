"""
lunettes_coeur.py — Lunettes cœur (boîte d'accessoires n°1 : `lunettes_coeur`, Peu commun, Visage).
VERSION v5 « plus jeu moderne » (docs/art-bible.md §13.8 fiche 7), faites SUR le cheval.

Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/lunettes_coeur.py
Sortie : blender/exports/lunettes_coeur.fbx et docs/captures/v5_cheval_lunettes_coeur.png.

Défaut corrigé : les branches étaient droites et s'écartaient dans le vide. Comme pour les
lunettes de soleil, elles suivent le côté de la tête (rayons « lissés » sur le vrai cheval,
1,5 cm au-dessus de la peau) et rentrent sous la crinière.

Les trois niveaux de forme (bible §13.2) :
  - primaire   : deux grands cœurs BOMBÉS (10 cm au centre, 6 cm au bord), roses vernis ;
  - secondaire : la monture-cadre blanc cassé (6 cm) qui entoure chaque cœur ; pont et
                 branches ;
  - tertiaire  : un petit cœur jaune verni en relief à chaque coin extérieur (à la place des
                 charnières) ; reflets peints (barre + point) en haut à gauche de chaque cœur.
Matières : cœurs vernis 0,90, monture mate 0,30, petits cœurs vernis, reflet.

Hiérarchie (le code Unity retrouve les pièces par leur nom) :
    lunettes_coeur        vide, origine = PointVisage (attachScale 1, attachOffset 0)
    └── Anim_Lunettes     monture + branches + petits cœurs ; origine = le pont (ressort)
        ├── Anim_CoeurGauche  verre en cœur à gauche de l'image (+ son reflet) ; origine =
        └── Anim_CoeurDroit   centre du cœur. Battement « boum-boum » (bible §13.8 :
                              scale 1 → 1,08 → 1 → 1,05 → 1 en 0,5 s, période 1,6 s)
"""

import math
import os
import sys

import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402

NOM = "lunettes_coeur"
POINT = "PointVisage"
MATS = ["M5_MontureBlanche", "M5_CoeurVerni", "M5_JauneVerni", "M5_Reflet"]  # 0..3
TAILLE = 0.30          # largeur d'un cœur (couvre l'œil du cheval, 0,17 × 0,21)
MONTEE = 0.02
AVANCE = 0.055
OUVERTURE = 14.0
ARBRE = None


def repere_coeur(x_unity):
    cote = 1 if x_unity < 0 else -1
    a = math.radians(OUVERTURE)
    centre = C.U(x_unity * 1.05, C.CHEVAL_YEUX_Y + MONTEE, C.CHEVAL_YEUX_AVANT - AVANCE)
    return C.repere(centre, Vector((cote * math.sin(a), math.cos(a), 0.0))), cote


def cadre(bm, m):
    """Cadre blanc cassé : section arrondie de 6 cm balayée autour du cœur."""
    contour = C.contour_coeur(TAILLE * 1.04, n=18)
    chemin = [m @ Vector((x, 0.0, z)) for x, z in contour]
    avant = m.to_3x3() @ Vector((0, 1, 0))
    n = len(chemin)
    sec = [(-0.03, -0.03), (0.03, -0.03), (0.036, 0.0), (0.03, 0.03), (-0.03, 0.03), (-0.036, 0.0)]
    anneaux = []
    for i in range(n):
        t = (chemin[(i + 1) % n] - chemin[i - 1]).normalized()
        dehors = t.cross(avant).normalized()
        anneaux.append([bm.verts.new(chemin[i] + dehors * a + avant * b) for a, b in sec])
    faces = []
    for i in range(n):
        A, B = anneaux[i], anneaux[(i + 1) % n]
        for s in range(6):
            u = (s + 1) % 6
            faces.append(bm.faces.new((A[s], B[s], B[u], A[u])))
    bmesh.ops.recalc_face_normals(bm, faces=faces)


def branche(bm, m, cote):
    depart = m @ Vector((cote * TAILLE * 0.50, -0.01, 0.06))
    chemin = [depart]
    centre = (0.0, C.CHEVAL_YEUX_Y + MONTEE + 0.04, 0.03)
    for ang, ecart in ((60, 0.02), (74, 0.016), (88, 0.015), (102, 0.015), (116, -0.02)):
        p, _ = C.sur_cheval_lisse(ARBRE, centre, math.radians(cote * ang), 0.0, ecart,
                                  pente=0.5, fenetre=(0.14, 0.14), pas=0.07)
        chemin.append(p)
    C.bm_tube(bm, chemin, 0.02, cotes=6, mat=0)
    return depart


def construire():
    global ARBRE
    C.nouvelle_scene()
    ARBRE = C.arbre_cheval()
    bm = bmesh.new()
    coeurs = []
    reperes = []
    for x_unity, nom in ((-C.CHEVAL_YEUX_X, "Anim_CoeurGauche"), (C.CHEVAL_YEUX_X, "Anim_CoeurDroit")):
        m, cote = repere_coeur(x_unity)
        reperes.append(m)
        cadre(bm, m)
        depart = branche(bm, m, cote)
        # petit cœur jaune au coin extérieur
        C.bm_coussin(bm, C.contour_coeur(0.09, n=12), 0.03, 0.05,
                     C.repere(depart + m.to_3x3() @ Vector((0, 0.03, 0.0)),
                              m.to_3x3() @ Vector((0, 1, 0))), mat=2, anneaux=1)
        bmc = bmesh.new()
        C.bm_coussin(bmc, C.contour_coeur(TAILLE * 0.97, n=18), 0.06, 0.10, m, mat=1, anneaux=2)
        C.bm_reflet(bmc, m @ Matrix.Translation((0.06, 0.05, 0.05)), mat=3)
        coeurs.append(C.piece(nom, bmc, MATS, lissage="tout", pivot=m.translation))
    a, b = reperes
    pa = a @ Vector((-TAILLE * 0.40, 0, 0.07))
    pb = b @ Vector((TAILLE * 0.40, 0, 0.07))
    milieu = (pa + pb) / 2 + Vector((0, 0.02, 0.03))
    C.bm_tube(bm, [pa, pa.lerp(milieu, 0.6), milieu, pb.lerp(milieu, 0.6), pb], 0.022, cotes=6)
    monture = C.piece("Anim_Lunettes", bm, MATS, lissage="tout", pivot=milieu)
    for c in coeurs:
        C.parenter(c, monture)
    return C.racine_v5(NOM, POINT, [monture])


if __name__ == "__main__":
    C.finir_v5(construire(), NOM, POINT)
