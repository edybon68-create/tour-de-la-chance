"""
pirate_bandana_pirate.py — Bandana (boîte d'accessoires Pirates : `bandana_pirate`,
Super rare, Tête). Fiche : docs/specs/v5-catalogue.md §2 n° 02 ; règles : art-bible §13.

Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/pirate_bandana_pirate.py
Sortie : blender/exports/bandana_pirate.fbx et docs/captures/v5_cheval_bandana_pirate.png.

Comment il colle au crâne :
    le tissu est une calotte « enroulée » sur la vraie tête du cheval : pour chaque point
    (longitude, latitude vues depuis le centre de la tête), un rayon se pose sur la peau
    (commun.sur_cheval_lisse, lissé pour passer par-dessus le toupet). On mesure la tête
    SANS SES OREILLES (commun_pirates.arbre_sans_oreilles) : le tissu épouse le crâne et
    les oreilles passent au travers, par-dessus le bandana, comme demandé par la fiche.
    Le bord avant s'arrête au-dessus des yeux (y ≥ 1,83 : il ne les cache jamais).

Les trois niveaux de forme (bible §13.2) :
  - primaire   : la calotte de tissu rouge (une seule épaisseur, bord caché par l'ourlet) ;
  - secondaire : l'ourlet roulé (un boudin de 6 cm qui fait le tour), le gros nœud sur la
                 nuque et ses deux pans (le gauche plus long : asymétrie de vie) ;
  - tertiaire  : 11 gros pois blancs vernis (Ø 9 à 11 cm), bombés.
Matières (2) : satin rouge (smoothness 0,50) et pois blanc verni (0,85).

Hiérarchie (le code Unity retrouve les pièces par leur nom) :
    bandana_pirate      vide, origine = PointChapeau (attachScale 1, attachOffset 0)
    └── Bandana         calotte + ourlet + pois + nœud ; origine = PointChapeau
        ├── Anim_PanGauche  pan à gauche de l'image ; origine = le nœud (ressort, et
        └── Anim_PanDroit   « flottent » ±10° en 1,4 s, le droit décalé de 0,08 s)
        └── Eclat_1..3  vides : sur des pois (1 éclat / 2,5 s, émission 0,5)
"""

import math
import os
import sys

import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402
import commun_pirates as P  # noqa: E402

NOM = "bandana_pirate"
POINT = "PointChapeau"
MATS = ["M5_PirBandanaRouge", "M5_BlancVerni"]   # 0, 1
CENTRE = (0.0, 1.62, 0.05)          # centre de la tête (Unity) d'où l'on enroule le tissu
NA = 22                             # tranches autour de la tête
ECART = 0.022
ARBRE = None


def lat_bord(a):
    """Latitude (radians) du bord du tissu : haut devant (au-dessus des yeux), bas sur la
    nuque. a = 0 devant, ±π derrière."""
    return math.radians(28 + 11 * math.cos(a))


def sur_tete(a, e, ecart=ECART):
    return C.sur_cheval_lisse(ARBRE, CENTRE, a, e, ecart, pente=1.2, fenetre=(0.14, 0.10),
                              pas=0.07)


def latitudes(a):
    e0 = lat_bord(a)
    return [e0 + (math.radians(84) - e0) * (k / 5) ** 0.9 for k in range(6)]


def calotte():
    bm = bmesh.new()
    rangs = []
    for i in range(NA):
        a = 2 * math.pi * i / NA
        rangs.append([bm.verts.new(sur_tete(a, e)[0]) for e in latitudes(a)])
    pole = bm.verts.new(sur_tete(0.0, math.radians(90))[0])
    m = len(rangs[0])
    for i in range(NA):
        A, B = rangs[i], rangs[(i + 1) % NA]
        for k in range(m - 1):
            bm.faces.new((A[k], B[k], B[k + 1], A[k + 1]))
        bm.faces.new((A[-1], B[-1], pole))
    centre = C.U(*CENTRE)
    bm.normal_update()
    for f in bm.faces:
        if f.normal.dot(f.calc_center_median() - centre) < 0:
            f.normal_flip()
    o = C.objet_depuis_bmesh("Calotte", bm, MATS)
    # Une seule épaisseur de tissu : l'intérieur, collé au crâne, ne se voit jamais
    # (l'ourlet roulé cache la tranche) ; on économise ainsi la moitié des triangles.
    return o


def ourlet(bm):
    chemin = []
    for i in range(NA):
        a = 2 * math.pi * i / NA
        chemin.append(sur_tete(a, lat_bord(a), ECART + 0.02)[0])
    chemin.append(chemin[0])
    C.bm_tube(bm, chemin, 0.032, cotes=5, mat=0, boucher=False)


def pois(bm):
    """11 pois blancs bombés, posés sur la calotte (jamais sur l'ourlet)."""
    eclats = []
    for k, (a_deg, e_deg, r) in enumerate((
            (0, 52, 0.055), (40, 44, 0.05), (-42, 46, 0.05), (85, 40, 0.05), (-88, 38, 0.05),
            (20, 70, 0.05), (-25, 68, 0.05), (130, 50, 0.05), (-130, 52, 0.05),
            (65, 62, 0.045), (-65, 60, 0.045))):
        a, e = math.radians(a_deg), math.radians(e_deg)
        p, d = sur_tete(a, e, ECART + 0.03)
        C.bm_coussin(bm, C.contour_ellipse(r, r, n=8), 0.012, 0.028, C.repere(p, d), mat=1,
                      anneaux=1, dos_plat=True)
        if k in (0, 3, 6):
            eclats.append(p + d * 0.02)
    return eclats


def noeud(bm):
    """Le nœud sur la nuque : deux petites boules de tissu (le nœud et sa boucle)."""
    a = math.pi
    p, d = sur_tete(a, lat_bord(a) + math.radians(4), ECART + 0.06)
    P.bm_ellipsoide(bm, p, (0.075, 0.06, 0.065), segments=8, anneaux=5, mat=0)
    for cote in (-1, 1):
        q = p + Vector((cote * 0.075, 0, 0.01)) + d * -0.01
        P.bm_ellipsoide(bm, q, (0.06, 0.04, 0.045), segments=6, anneaux=4, mat=0)
    return p, d


def pan(nom, p, d, cote, longueur):
    """Un pan qui tombe du nœud sur la crinière, s'écarte un peu et ondule."""
    bm = bmesh.new()
    lat = Vector((cote, 0, 0))
    chemin, hauts = [], []
    for k in range(6):
        t = k / 5
        q = (p + lat * (0.03 + 0.12 * t) + d * (0.04 + 0.07 * t)
             + Vector((0, 0, -longueur * t)) + lat * 0.02 * math.sin(t * math.pi * 2))
        chemin.append(q)
        hauts.append(d)
    larg = [(-0.05, -0.012), (0.05, -0.012), (0.05, 0.012), (-0.05, 0.012)]
    an = C.bm_balayage_surface(bm, chemin, hauts, larg, mat=0)
    # bout en pointe : le dernier anneau est resserré
    for v in an[-1]:
        v.co = chemin[-1] + (v.co - chemin[-1]) * 0.35
    C.bm_disque(bm, 0.03, 0.01, C.repere(chemin[2] + d * 0.014, d), cotes=6, mat=1)
    return C.piece(nom, bm, MATS, lissage="tout", pivot=p)


def construire():
    global ARBRE
    C.nouvelle_scene()
    ARBRE = P.arbre_sans_oreilles()
    cal = calotte()
    bm = bmesh.new()
    ourlet(bm)
    eclats = pois(bm)
    p_n, d_n = noeud(bm)
    det = C.objet_depuis_bmesh("Details", bm, MATS)
    bandana = C.joindre([cal, det], "Bandana")
    C.lisser_tout(bandana)
    C.deplacer_origine(bandana, C.U(*C.CHEVAL_ACCROCHES[POINT]))
    for nom, cote, lg in (("Anim_PanGauche", 1, 0.30), ("Anim_PanDroit", -1, 0.22)):
        C.parenter(pan(nom, p_n, d_n, cote, lg), bandana)
    for e in C.reperes_effets(eclats):
        C.parenter(e, bandana)
    return C.racine_v5(NOM, POINT, [bandana])


if __name__ == "__main__":
    P.finir_pirate(construire(), NOM, POINT)
