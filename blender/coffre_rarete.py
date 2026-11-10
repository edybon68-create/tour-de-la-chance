"""
coffre_rarete.py — Le « moule » commun des coffres de rareté (Bronze, Argent, Or, Diamant).

Ce fichier n'est pas lancé seul : les scripts coffre_bronze.py, coffre_argent.py,
coffre_or.py et coffre_diamant.py l'importent, choisissent leurs matières et ajoutent
leurs ornements. Le coffre en bois reste celui de coffre.py (on n'y touche pas).

Pourquoi un moule commun ?
    La spec v3 (§16) exige que les 5 coffres aient EXACTEMENT le même gabarit, la même
    hiérarchie et les mêmes pivots : le code Unity `Chest` échange un modèle contre un
    autre sans rien régler. Le plus sûr est donc de construire le corps, l'intérieur, le
    couvercle et la charnière au même endroit, avec les cotes de coffre.py, et de ne laisser
    varier que les matières et les décorations.

Hiérarchie produite (identique à coffre.fbx, plus des gemmes facultatives) :
    Coffre              origine au sol, au centre (0 ; 0 ; 0)
    ├── Corps           UN SEUL matériau (repeint en or / rouge à la révélation)
    ├── Ferrures        bandes, coins, serrure, socle, rivets… (toutes les décorations fixes)
    ├── Interieur       doublure intérieure (velours sombre pour les raretés hautes)
    ├── Gemme_1 … _4    (Diamant seulement) gemmes des coins, pivot au centre de la gemme
    └── Charniere       objet vide sur l'axe de rotation, Unity (0 ; 0,8 ; 0,5)
        ├── Couvercle   couvercle complet, modélisé fermé, pivot sur la charnière
        └── Gemme_0     (Diamant seulement) grosse gemme du couvercle, pivot en son centre

Règle importante : AUCUNE décoration précieuse dans `Corps`. Le code repeint tout le Corps
d'une seule couleur ; une gemme dedans deviendrait rouge ou or avec lui.
Pas d'émission (lumière propre) sur les coffres : règle v3 de la bible (§4.1).
"""

import math
import os
import sys

import bmesh
import bpy
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402
import coffre as B  # noqa: E402  (coffre en bois : on reprend ses cotes, pas son script)

# --- Cotes reprises du coffre en bois (une seule source de vérité : coffre.py) ----------
LARGEUR, PROFONDEUR, HAUTEUR = B.LARGEUR, B.PROFONDEUR, B.HAUTEUR
AVANT, PAROI, FOND_INTERIEUR = B.AVANT, B.PAROI, B.FOND_INTERIEUR
RAINURE, PROF_RAINURE = B.RAINURE, B.PROF_RAINURE
CHARNIERE = B.CHARNIERE
COUV_X, COUV_ARRIERE, COUV_AVANT = B.COUV_X, B.COUV_ARRIERE, B.COUV_AVANT
COUV_BORD, COUV_DOME = B.COUV_BORD, B.COUV_DOME
X_BANDES, L_BANDE, RELIEF = B.X_BANDES, B.L_BANDE, B.RELIEF
FACE_BANDE = AVANT + RELIEF          # devant des bandes du corps (y = 0,52)

# --- Matières des raretés (spec v3 §5.3.1). Ajoutées à la palette commune au chargement.
# nom : (couleur hex sRGB, metallic, roughness). Roughness = 1 - smoothness Unity.
# Aucune n'a de 4e nombre : pas d'émission sur les coffres.
MATERIAUX_RARETE = {
    # Bronze : bois verni roux + bronze
    "M_CorpsBronze": ("#7A4A2E", 0.0, 0.50),
    "M_CouvercleBronze": ("#5E3620", 0.0, 0.50),
    "M_Bronze": ("#B0703A", 1.0, 0.45),        # smoothness 0,55
    # Argent : bois peint bleu nuit + argent poli
    "M_CorpsArgent": ("#3E5C8A", 0.0, 0.55),
    "M_CouvercleArgent": ("#2F4870", 0.0, 0.55),
    "M_Argent": ("#C9CED8", 1.0, 0.15),        # smoothness 0,85
    "M_VeloursNuit": ("#1E2438", 0.0, 0.95),   # doublure intérieure
    # Or : laque violet royal + doré + rubis
    "M_CorpsOr": ("#5B2D82", 0.0, 0.30),
    "M_CouvercleOr": ("#4A2370", 0.0, 0.30),
    "M_Dore": ("#E8B830", 1.0, 0.20),          # smoothness 0,8 ; ≠ or trésor #FFC83A
    "M_VeloursRouge": ("#4A1424", 0.0, 0.95),
    # Diamant : nacre glacée + argent + gemmes bleu glace
    "M_CorpsDiamant": ("#E6F0F7", 0.0, 0.30),  # smoothness 0,7
    "M_CouvercleDiamant": ("#D2E3F0", 0.0, 0.30),
    "M_Gemme": ("#9FE3FF", 0.0, 0.05),         # smoothness 0,95
    "M_VeloursBleu": ("#1E2A4A", 0.0, 0.95),
}
C.MATERIAUX.update(MATERIAUX_RARETE)


# =========================================================================================
# 1. Petites briques de géométrie propres aux coffres
# =========================================================================================
def profil_dome(segments=6, decalage=0.0, z0=HAUTEUR):
    """Comme coffre.profil_dome(), avec un nombre de segments réglable
    (6 = bombé lisse du bois ; 4 = couvercle « taillé » en facettes du Diamant)."""
    yc = (COUV_AVANT + COUV_ARRIERE) / 2
    a = (COUV_AVANT - COUV_ARRIERE) / 2 + decalage
    b = COUV_DOME + decalage
    pts = [(yc + a, z0)]
    for k in range(segments + 1):
        t = math.pi * k / segments
        pts.append((yc + a * math.cos(t), z0 + COUV_BORD + b * math.sin(t)))
    pts.append((yc - a, z0))
    return pts


def point_dome(t, segments=6, decalage=0.0):
    """Point (y, z) du dessus du couvercle à l'« angle » t (0 = devant, π = derrière) et sa
    normale, en suivant les segments réels (pour poser rivets et pierres SUR les facettes)."""
    pts = profil_dome(segments, decalage)[1:-1]       # les points de l'arrondi
    pos = t / math.pi * segments
    i = min(int(pos), segments - 1)
    f = pos - i
    (y0, z0), (y1, z1) = pts[i], pts[i + 1]
    p = Vector((0, y0 + (y1 - y0) * f, z0 + (z1 - z0) * f))
    tangente = Vector((0, y1 - y0, z1 - z0)).normalized()
    normale = Vector((0, -tangente.z, tangente.y))      # perpendiculaire, vers le haut
    if normale.z < 0:
        normale = -normale
    return p, normale


def orientation(normale):
    """Matrice de rotation qui envoie l'axe Z local sur `normale` (pour poser un rivet ou
    une gemme modélisés « debout » sur une surface penchée)."""
    return Vector(normale).normalized().to_track_quat("Z", "Y").to_matrix().to_4x4()


def bm_rivet(bm, centre, normale, rayon=0.022, hauteur=0.014, mat=0, segments=6):
    """Rivet rond : petit dôme à 6 côtés, base enfoncée de 2 mm dans la bande.
    18 triangles. `normale` = direction vers laquelle il dépasse."""
    avant = set(bm.verts)
    C.bm_revolution(bm, [(rayon, -0.002), (rayon * 0.75, hauteur * 0.7), (0.0, hauteur)],
                    segments, mats=[mat, mat])
    nouveaux = [v for v in bm.verts if v not in avant]
    m = Matrix.Translation(Vector(centre)) @ orientation(normale)
    bmesh.ops.transform(bm, matrix=m, verts=nouveaux)


def contour_etoile(r_ext, r_int, branches=5, cx=0.0, cz=0.0):
    """Contour 2D (x, z) d'une étoile, une pointe vers le haut, sens anti-horaire."""
    pts = []
    for k in range(2 * branches):
        r = r_ext if k % 2 == 0 else r_int
        a = math.pi / 2 + math.pi * k / branches
        pts.append((cx + r * math.cos(a), cz + r * math.sin(a)))
    return pts


def bm_etoile(bm, centre, normale, r_ext, r_int, epaisseur, mat=0):
    """Étoile en relief posée sur une surface : modélisée dans le plan XZ (épaisseur en Y),
    puis tournée pour que son épaisseur suive `normale`."""
    verts = C.bm_prisme(bm, contour_etoile(r_ext, r_int), epaisseur, axe="Y",
                        decalage=-0.003, mat=mat)
    # Y local -> normale, Z local au plus près du haut du monde.
    haut = Vector((0, 0, 1)) if abs(Vector(normale).z) < 0.9 else Vector((0, 1, 0))
    m = C.repere(centre, normale, haut)
    bmesh.ops.transform(bm, matrix=m, verts=verts)


def bm_gemme(bm, rayon, mat=0, cotes=8):
    """Gemme taillée façon « brillant », centrée sur l'origine, table vers +Z :
    table (octogone) + couronne + rondiste + pavillon pointu. 30 triangles.
    Faces plates (pas de lissage) : ce sont les facettes qui accrochent la lumière."""
    table_r, couronne_h, pavillon_h = rayon * 0.55, rayon * 0.45, rayon * 0.85
    a0 = math.pi / cotes  # une arête bien horizontale en haut
    table = [bm.verts.new((table_r * math.cos(a0 + 2 * math.pi * k / cotes),
                           table_r * math.sin(a0 + 2 * math.pi * k / cotes), couronne_h))
             for k in range(cotes)]
    rondiste = [bm.verts.new((rayon * math.cos(a0 + 2 * math.pi * k / cotes),
                              rayon * math.sin(a0 + 2 * math.pi * k / cotes), 0.0))
                for k in range(cotes)]
    pointe = bm.verts.new((0, 0, -pavillon_h))
    faces = [bm.faces.new(table)]
    for k in range(cotes):
        j = (k + 1) % cotes
        faces.append(bm.faces.new((rondiste[k], rondiste[j], table[j], table[k])))
        faces.append(bm.faces.new((rondiste[j], rondiste[k], pointe)))
    for f in faces:
        f.material_index = mat
    bmesh.ops.recalc_face_normals(bm, faces=faces)
    return table + rondiste + [pointe]


def gemme(nom, rayon, centre, normale, mats):
    """Gemme en OBJET SÉPARÉ, pivot au centre de la gemme (au niveau du rondiste) :
    le code Unity peut la faire tourner sur elle-même ou pulser sans toucher au reste.
    Le maillage est déjà orienté (table vers `normale`) ; l'objet n'a aucune rotation."""
    bm = bmesh.new()
    verts = bm_gemme(bm, rayon)
    bmesh.ops.transform(bm, matrix=orientation(normale), verts=verts)
    o = C.objet_depuis_bmesh(nom, bm, mats)
    o.location = Vector(centre)
    return o


def bm_chaton(bm, centre, normale, rayon, hauteur=0.02, mat=0, cotes=8):
    """« Chaton » : la petite couronne de métal qui sertit une gemme. Disque à 8 côtés
    posé sur la surface (base enfoncée de 5 mm), axe le long de `normale`."""
    avant = set(bm.verts)
    a0 = math.pi / cotes
    C.bm_revolution(bm, [(0.0, -0.005), (rayon, -0.005), (rayon, hauteur), (0.0, hauteur)],
                    cotes, mats=[mat] * 3, angle_depart=a0)
    nouveaux = [v for v in bm.verts if v not in avant]
    bmesh.ops.transform(bm, matrix=Matrix.Translation(Vector(centre)) @ orientation(normale),
                        verts=nouveaux)


def bm_lisere(bm, z0, z1, relief, mat=0):
    """Liseré de métal autour du bord vertical du couvercle (devant + deux côtés ; on ne
    voit jamais l'arrière). Bande de (z1 - z0) de haut qui dépasse de `relief`."""
    h, zc = z1 - z0, (z0 + z1) / 2
    C.bm_boite(bm, (2 * COUV_X + 2 * relief, relief + 0.01, h),
               (0, COUV_AVANT + (relief - 0.01) / 2, zc), mat=mat)
    longueur = COUV_AVANT - COUV_ARRIERE
    for sx in (-1, 1):
        C.bm_boite(bm, (relief + 0.01, longueur + relief, h),
                   (sx * (COUV_X + (relief - 0.01) / 2), (COUV_AVANT + COUV_ARRIERE + relief) / 2, zc),
                   mat=mat)


# =========================================================================================
# 2. Le coffre de base, paramétré
# =========================================================================================
def construire(style):
    """Construit le corps, l'intérieur, les ferrures de base et le couvercle de base.

    `style` (dictionnaire) :
        corps, couvercle, metal, interieur : noms de matériaux
        segments_dome : 6 (lisse) ou 4 (facettes)
        plaque : "ecusson" | "etoile" | "losange"   (forme de la plaque de serrure)
        coins  : "equerres" (petites, comme le bois) | "cornieres" (pleine hauteur)
        socle  : True pour une plinthe de métal tout autour du bas du corps
        gonds_ronds : rayon des charnières (0,04 comme le bois)

    Renvoie un dictionnaire de listes d'objets à compléter par le script de la rareté :
        {"ferrures": [...], "couvercle": [...], "gemmes_corps": [], "gemmes_couvercle": []}
    puis on appelle assembler()."""
    C.nouvelle_scene()
    seg = style.get("segments_dome", 6)
    MAT_F = [style["metal"], "M_TrouSerrure"]          # 0 = métal, 1 = trou de serrure
    pieces = {"ferrures": [], "couvercle": [], "gemmes_corps": [], "gemmes_couvercle": [],
              "style": style}

    # ---------------------------------------------------------------- Corps (un matériau)
    # Exactement le corps du coffre en bois (3 planches + noyau), seule la couleur change.
    h_planche = (HAUTEUR - 2 * RAINURE) / 3
    morceaux = []
    for i in range(3):
        z0 = i * (h_planche + RAINURE)
        bm = bmesh.new()
        if i < 2:
            C.bm_boite(bm, (LARGEUR, PROFONDEUR, h_planche), (0, 0, z0 + h_planche / 2))
        else:
            ex, ey = LARGEUR / 2, PROFONDEUR / 2
            ix, iy = ex - PAROI, ey - PAROI

            def rect(x, y, z):
                return [(-x, -y, z), (x, -y, z), (x, y, z), (-x, y, z)]
            C.bm_anneaux(bm, [rect(ex, ey, z0), rect(ex, ey, HAUTEUR), rect(ix, iy, HAUTEUR),
                              rect(ix, iy, z0), rect(ex, ey, z0)])
            bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
            bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        o = C.objet_depuis_bmesh(f"Planche{i}", bm, [style["corps"]])
        C.biseau(o, largeur=0.02, segments=1, angle=30)
        C.appliquer(o)
        morceaux.append(o)
    morceaux.append(C.boite("Noyau", (LARGEUR - 2 * PROF_RAINURE, PROFONDEUR - 2 * PROF_RAINURE,
                                      0.54), (0, 0, 0.01 + 0.27), style["corps"]))
    corps = C.joindre(morceaux, "Corps")
    C.lisser_angle(corps, 30)
    pieces["corps"] = corps

    # ---------------------------------------------------------------- Interieur
    bm = bmesh.new()
    ix, iy = LARGEUR / 2 - PAROI - 0.002, PROFONDEUR / 2 - PAROI - 0.002
    h = HAUTEUR - 0.004 - FOND_INTERIEUR
    C.bm_boite(bm, (2 * ix, 2 * iy, h), (0, 0, FOND_INTERIEUR + h / 2))
    bmesh.ops.delete(bm, geom=[f for f in bm.faces if f.normal.z > 0.9], context="FACES")
    bmesh.ops.reverse_faces(bm, faces=bm.faces)
    pieces["interieur"] = C.objet_depuis_bmesh("Interieur", bm, [style["interieur"]])

    # ---------------------------------------------------------------- Ferrures de base
    ferr = pieces["ferrures"]
    # Bandes verticales avant (comme le bois).
    bm = bmesh.new()
    for sx in (-1, 1):
        C.bm_boite(bm, (L_BANDE, RELIEF + 0.01, HAUTEUR),
                   (sx * X_BANDES, AVANT + (RELIEF - 0.01) / 2, HAUTEUR / 2))
    o = C.objet_depuis_bmesh("Bandes", bm, MAT_F)
    C.biseau(o, 0.008, 1, 30)
    C.appliquer(o)
    ferr.append(o)

    # Coins.
    bm = bmesh.new()
    if style.get("coins", "equerres") == "equerres":
        for sx in (-1, 1):
            for zc in (0.075, HAUTEUR - 0.075):
                C.bm_boite(bm, (0.12, 0.03, 0.15), (sx * (LARGEUR / 2 - 0.05), AVANT + 0.005, zc))
                C.bm_boite(bm, (0.03, 0.12, 0.15), (sx * (LARGEUR / 2 + 0.005), AVANT - 0.05, zc))
    else:
        # Cornières pleine hauteur sur les 2 arêtes verticales avant : elles « encadrent »
        # la face avant, on les lit de loin comme un coffre plus solide, plus riche.
        for sx in (-1, 1):
            C.bm_boite(bm, (0.13, 0.03, HAUTEUR), (sx * (LARGEUR / 2 - 0.055), AVANT + 0.005,
                                                   HAUTEUR / 2))
            C.bm_boite(bm, (0.03, 0.13, HAUTEUR), (sx * (LARGEUR / 2 + 0.005), AVANT - 0.055,
                                                   HAUTEUR / 2))
    # Pas de biseau sur les coins (comme le coffre en bois) : petites pièces, peu vues.
    ferr.append(C.objet_depuis_bmesh("Coins", bm, MAT_F))

    # Socle : plinthe de métal de 7 cm tout autour du bas (2 cm de relief).
    if style.get("socle"):
        bm = bmesh.new()
        e = 0.02
        ex, ey, z1 = LARGEUR / 2 + e, PROFONDEUR / 2 + e, 0.07

        def rect(x, y, z):
            return [(-x, -y, z), (x, -y, z), (x, y, z), (-x, y, z)]
        ix, iy = LARGEUR / 2 - 0.01, PROFONDEUR / 2 - 0.01
        C.bm_anneaux(bm, [rect(ix, iy, 0.0), rect(ex, ey, 0.0), rect(ex, ey, z1),
                          rect(ix, iy, z1), rect(ix, iy, 0.0)])
        bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        # Pas de biseau ici : sur un cadre, il multiplierait les triangles (x10) pour un
        # détail posé au ras du sol que la caméra voit à peine.
        ferr.append(C.objet_depuis_bmesh("Socle", bm, MAT_F))

    # Plaque de serrure + trou.
    ferr += plaque_serrure(style.get("plaque", "ecusson"), MAT_F)

    # Charnières fixes (gonds), sur l'axe de rotation, comme le bois.
    bm = bmesh.new()
    for sx in (-1, 1):
        cercle = [(CHARNIERE.y - 0.015 + 0.04 * math.cos(2 * math.pi * k / 8),
                   CHARNIERE.z + 0.01 + 0.04 * math.sin(2 * math.pi * k / 8)) for k in range(8)]
        C.bm_prisme(bm, cercle, 0.16, axe="X", decalage=sx * 0.45 - 0.08)
    ferr.append(C.objet_depuis_bmesh("Gonds", bm, MAT_F))

    # ---------------------------------------------------------------- Couvercle de base
    MAT_C = [style["couvercle"], style["metal"], style["interieur"]]
    couv = pieces["couvercle"]
    bm = bmesh.new()
    C.bm_prisme(bm, profil_dome(seg), 2 * COUV_X, axe="X", decalage=-COUV_X, mat=0)
    for f in bm.faces:
        if f.normal.z < -0.9:
            f.material_index = 2       # dessous du couvercle = doublure
    o = C.objet_depuis_bmesh("Dome", bm, MAT_C)
    C.biseau(o, 0.03, 1, 50)
    C.appliquer(o)
    couv.append(o)

    # Bandes de métal sur le dessus, qui suivent le profil (lisse ou à facettes).
    bm = bmesh.new()
    ext = profil_dome(seg, decalage=RELIEF)[1:-1]
    inte = profil_dome(seg, decalage=-0.005)[1:-1]
    ext = [(ext[0][0], HAUTEUR + 0.005)] + ext + [(ext[-1][0], HAUTEUR + 0.005)]
    inte = [(inte[0][0], HAUTEUR + 0.005)] + inte + [(inte[-1][0], HAUTEUR + 0.005)]
    for sx in (-1, 1):
        x0, x1 = sx * X_BANDES - L_BANDE / 2, sx * X_BANDES + L_BANDE / 2
        sections = [[(x0, e[0], e[1]), (x1, e[0], e[1]), (x1, i[0], i[1]), (x0, i[0], i[1])]
                    for e, i in zip(ext, inte)]
        rangs = C.bm_anneaux(bm, sections, mats=[1] * len(sections))
        for bout in (rangs[0], rangs[-1]):
            bm.faces.new(bout).material_index = 1
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    o = C.objet_depuis_bmesh("BandesCouvercle", bm, MAT_C)
    C.biseau(o, 0.006, 1, 40)
    C.appliquer(o)
    couv.append(o)

    # Fermoir qui descend sur la plaque de serrure.
    bm = bmesh.new()
    C.bm_boite(bm, (0.10, 0.035, 0.16), (0, COUV_AVANT + 0.012, HAUTEUR - 0.02), mat=1)
    o = C.objet_depuis_bmesh("Fermoir", bm, MAT_C)
    C.biseau(o, 0.01, 1, 30)
    C.appliquer(o)
    couv.append(o)
    return pieces


def plaque_serrure(forme, mats):
    """Plaque de serrure (métal de la rareté) + trou de serrure sombre.
    ecusson : l'écusson du coffre en bois (26 x 30 cm) ;
    etoile  : une grande étoile à 5 branches (Argent), le trou au centre ;
    losange : un losange taillé (Diamant), comme une pierre vue de face."""
    y0 = AVANT - 0.005                 # la plaque part de 5 mm dans le bois
    ep = 0.035                         # 3,5 cm d'épaisseur -> 3 cm de relief
    if forme == "ecusson":
        demi, haut, rayon = 0.13, HAUTEUR - 0.02, 0.13
        centre_arc = haut - 0.30 + rayon
        contour = [(demi, haut), (-demi, haut)]
        for k in range(7):
            t = math.pi + math.pi * k / 6
            contour.append((rayon * math.cos(t), centre_arc + rayon * math.sin(t)))
        z_trou = centre_arc + 0.01
    elif forme == "etoile":
        z_trou = 0.565
        contour = contour_etoile(0.19, 0.095, cz=z_trou - 0.005)
    else:  # losange
        z_trou = 0.59
        contour = [(0.0, z_trou - 0.20), (0.15, z_trou), (0.0, z_trou + 0.19), (-0.15, z_trou)]
    bm = bmesh.new()
    C.bm_prisme(bm, contour, ep, axe="Y", decalage=y0, mat=0)
    o = C.objet_depuis_bmesh("Plaque", bm, mats)
    if forme != "etoile":   # sur les 10 pointes de l'étoile, le biseau coûterait trop cher
        C.biseau(o, 0.01, 1, 30)
        C.appliquer(o)

    bm = bmesh.new()
    y_plaque = y0 + ep
    rond = [(0.032 * math.cos(2 * math.pi * k / 8), z_trou + 0.032 * math.sin(2 * math.pi * k / 8))
            for k in range(8)]
    C.bm_prisme(bm, rond, 0.006, axe="Y", decalage=y_plaque, mat=1)
    trapeze = [(-0.014, z_trou), (-0.026, z_trou - 0.075), (0.026, z_trou - 0.075), (0.014, z_trou)]
    C.bm_prisme(bm, trapeze, 0.006, axe="Y", decalage=y_plaque, mat=1)
    return [o, C.objet_depuis_bmesh("Trou", bm, mats)]


# =========================================================================================
# 3. Assemblage, contrôle, miniature, export
# =========================================================================================
def assembler(pieces):
    """Joint les morceaux, pose les pivots et range tout dans la hiérarchie attendue."""
    racine = C.vide("Coffre")
    ferrures = C.joindre(pieces["ferrures"], "Ferrures")
    C.lisser_angle(ferrures, 30)
    couvercle = C.joindre(pieces["couvercle"], "Couvercle")
    C.lisser_angle(couvercle, 40 if pieces["style"].get("segments_dome", 6) > 4 else 30)

    charniere = C.vide("Charniere", CHARNIERE)
    C.deplacer_origine(couvercle, CHARNIERE)
    for o in (pieces["corps"], ferrures, pieces["interieur"], charniere):
        C.parenter(o, racine)
    C.parenter(couvercle, charniere)
    # Gemmes : objets séparés. Celles du couvercle suivent la charnière (elles s'ouvrent
    # avec lui) ; celles du corps sont rangées sous la racine.
    for g in pieces["gemmes_couvercle"]:
        C.parenter(g, charniere)
    for g in pieces["gemmes_corps"]:
        C.parenter(g, racine)
    return racine


def ouvrir(racine, angle=110.0):
    """Ouvre le couvercle de `angle` degrés (comme Unity : rotation de la Charniere sur
    son axe X). Sert seulement aux rendus de contrôle ; ouvrir(racine, 0) referme."""
    charniere = next(o for o in C.descendants(racine) if o.name == "Charniere")
    charniere.rotation_euler = (math.radians(angle), 0, 0)
    bpy.context.view_layer.update()


def finir(racine, rarete):
    """Fin commune : rapport des triangles, miniatures (fermé et ouvert) vues de la caméra
    du jeu, export FBX dans blender/exports/ (Unity n'est pas touché), vérification."""
    total = C.rapport(racine)
    objs = C.descendants(racine)
    C.rendre_camera_jeu(objs, f"blender_coffre_{rarete}.png", marge=1.15)
    ouvrir(racine, 110)
    C.rendre_camera_jeu(objs, f"blender_coffre_{rarete}_ouvert.png", marge=1.15)
    ouvrir(racine, 0)
    chemin = C.exporter_fbx(racine, f"coffre_{rarete}.fbx", dossier=C.DOSSIER_EXPORTS)
    C.verifier_fbx(chemin)
    print(f"RESUME coffre_{rarete} triangles={total}")
    return total
