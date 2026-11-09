"""
sol.py — Modélise le sol de l'étage (le sommet de la tour) et l'exporte en FBX.

Fiche suivie : docs/art-bible.md §6.8 ; référence visuelle : art/concept/concept_etage.png.
Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/sol.py

Hiérarchie produite (repère Unity : dessus du sol à y = 0, origine au centre du dessus) :
    Sol                 objet vide, origine (0 ; 0 ; 0) = centre du dessus
    ├── Dalles          dalle centrale + 2 couronnes de dalles (M_PierreDalle, M_PierreDalleVariante)
    ├── Rosace          étoile à 8 branches incrustée au centre (M_PierreClaire, M_Incrustation)
    ├── Margelle        16 blocs clairs surélevés au bord (M_PierreClaire)
    ├── Flanc           muraille de la tour : 4 rangées de gros blocs décalés (M_PierreFlanc)
    └── Joints          le « noyau » sombre visible entre les dalles et les blocs (M_PierreJoint)

Cotes principales (mètres) :
    - disque Ø 10 (margelle de Ø 9,3 à Ø 10), dessus des dalles à 0, margelle à +0,04 ;
    - dalle centrale Ø 3, couronne 2 jusqu'à Ø 6 (16 dalles), couronne 3 jusqu'à Ø 9,3 (24 dalles) ;
    - joints : 3 cm de large, 2 cm de creux ; biseau des dalles 2 cm ;
    - flanc : de -0,30 à -2,50 (la caméra du jeu voit la muraille sur les côtés de l'écran).

Astuce « budget de triangles » :
    Au lieu d'un modificateur Bevel (qui biseauterait aussi les coins verticaux et ajouterait
    beaucoup de faces), chaque dalle est construite directement « en coussin » : un dessus
    plat, entouré d'une bande inclinée qui descend jusqu'au fond du joint. Le biseau est donc
    DANS la forme, pour 16 triangles par dalle seulement. On n'a pas besoin de face de dessous :
    personne ne regarde sous le sol.
"""

import math
import os
import random
import sys

import bmesh
import bpy
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402

# --- Dimensions -------------------------------------------------------------------------
R_CENTRE = 1.50          # dalle centrale : Ø 3
R_COURONNE2 = 3.00       # fin de la 2e couronne : Ø 6
R_COURONNE3 = 4.65       # fin de la 3e couronne : Ø 9,3 (début de la margelle)
R_MARGELLE = 5.00        # bord extérieur : Ø 10
R_FLANC = 4.92           # face avant des blocs du flanc (8 cm en retrait sous la margelle)
R_NOYAU = 4.88           # noyau sombre, derrière les joints

DEMI_JOINT = 0.015       # joint de 3 cm = 1,5 cm de chaque côté de la ligne de découpe
BISEAU = 0.02            # biseau des dalles (2 cm)
CREUX = -0.02            # fond des joints (2 cm sous le dessus)
VARIATION = 0.012        # les dalles descendent au hasard de 0 à 1,2 cm (jamais au-dessus de 0)
PART_VARIANTE = 0.33     # environ 1 dalle sur 3 en teinte variante

HAUT_MARGELLE = 0.04     # margelle surélevée de 4 cm
BAS_MARGELLE = -0.30     # la margelle descend jusque-là sur le flanc
BISEAU_MARGELLE = 0.03

BAS_FLANC = -2.50        # bas de la muraille
RANGEES_FLANC = 4        # 4 rangées de 0,55 m
BLOCS_PAR_RANGEE = 20    # blocs de ≈ 1,55 m de long
BISEAU_FLANC = 0.025

# Rosace (incrustation à fleur du sol : 4 mm de relief, les coffres peuvent s'y poser)
R_ETOILE = 1.26          # pointes de l'étoile (Ø ≈ 2,5)
R_CREUX_ETOILE = 0.74    # creux entre les branches
R_CERCLE = 1.31          # cercle violet autour de l'étoile (de 1,26 à 1,31)
FILET = 0.045            # largeur du filet violet qui borde l'étoile
RELIEF_ROSACE = 0.004

# Zones qui doivent rester PARFAITEMENT à y = 0 (coffres et personnage posés dessus).
# Rectangles en coordonnées Unity : (x min, x max, z min, z max).
ZONES_PLATES_UNITY = [
    (-3.5, 3.5, -0.9, 0.9),     # les 3 coffres en x = -2,6 / 0 / 2,6 (1,40 x 1,00 chacun)
    (-0.6, 0.6, 1.4, 2.6),      # le personnage en (0 ; 0 ; 2)
]


# --- Surfaces paramétrées ---------------------------------------------------------------
# On décrit chaque dalle ou bloc par 2 coordonnées (u, v) « sur » une surface + une hauteur h.
def point_sol(u, v, h):
    """Sol : u = angle, v = rayon, h = altitude."""
    return Vector((v * math.cos(u), v * math.sin(u), h))


def point_flanc(u, v, h):
    """Flanc (cylindre) : u = angle, v = altitude, h = rayon (la « hauteur » sort du mur)."""
    return Vector((h * math.cos(u), h * math.sin(u), v))


def contour_secteur(u0, u1, v0, v1, s, retrait, rayon):
    """Contour d'un morceau de couronne (u0→u1 en angle, v0→v1), rentré de `retrait` mètres
    de tous les côtés. `s` = nombre de segments le long de l'arc. `rayon(v)` sert à convertir
    un retrait en mètres en un retrait en angle (un mètre = 1/rayon radian).
    Ordre des points : bord v0 dans le sens des u croissants, puis bord v1 en revenant."""
    pts = []
    va, vb = v0 + retrait, v1 - retrait
    for v, sens in ((va, 1), (vb, -1)):
        du = retrait / rayon(v)
        a, b = u0 + du, u1 - du
        for k in range(s + 1):
            t = k / s if sens == 1 else 1 - k / s
            pts.append((a + (b - a) * t, v))
    return pts


def paver(bm, surface, couches, mat, inverser=False):
    """Construit un pavé « en coussin » : des contours successifs (du bas vers le haut)
    reliés par des bandes de quads, et le dernier fermé par un polygone (le dessus).
    couches = [(contour, h), ...]. `inverser` retourne l'ordre des sommets (donc les
    normales) quand la surface paramétrée est « à l'envers » (cas du sol)."""
    rangs = [[bm.verts.new(surface(u, v, h)) for (u, v) in contour] for contour, h in couches]
    faces = []
    n = len(rangs[0])
    for A, B in zip(rangs, rangs[1:]):
        for i in range(n):
            j = (i + 1) % n
            faces.append([A[i], A[j], B[j], B[i]])
    faces.append(rangs[-1])
    for sommets in faces:
        f = bm.faces.new(sommets[::-1] if inverser else sommets)
        f.material_index = mat


def dans_zone_plate(points_blender):
    for p in points_blender:
        ux, uz = -p.x, -p.y   # Blender -> Unity (x, z)
        for x0, x1, z0, z1 in ZONES_PLATES_UNITY:
            if x0 <= ux <= x1 and z0 <= uz <= z1:
                return True
    return False


# --- Les parties du sol -----------------------------------------------------------------
def construire_dalles():
    """Dalle centrale + couronnes 2 et 3."""
    bm = bmesh.new()
    # Indices de matériaux de l'objet Dalles : 0 = principale, 1 = variante.

    # Dalle centrale : un disque à 32 côtés « en coussin ». Toujours à 0 (coffre du milieu).
    # Ici les cercles tournent déjà dans le sens inverse des aiguilles d'une montre vu de
    # dessus : pas besoin d'inverser (contrairement aux morceaux de couronne).
    n = 32
    def cercle(r):
        return [(2 * math.pi * k / n, r) for k in range(n)]
    paver(bm, point_sol, [(cercle(R_CENTRE - DEMI_JOINT), CREUX),
                          (cercle(R_CENTRE - DEMI_JOINT - BISEAU), 0.0)], mat=0, inverser=False)

    # Couronnes : (rayon intérieur, extérieur, nombre de dalles, segments par dalle, décalage).
    # Les décalages évitent que les joints de deux couronnes voisines tombent dans le
    # prolongement l'un de l'autre (comme un vrai dallage, on « croise » les joints).
    couronnes = [
        (R_CENTRE, R_COURONNE2, 16, 2, math.radians(11.25)),
        (R_COURONNE2, R_COURONNE3, 24, 2, 0.0),
    ]
    for r0, r1, nb, s, decalage in couronnes:
        pas = 2 * math.pi / nb
        for k in range(nb):
            u0 = decalage + k * pas
            u1 = u0 + pas
            dessus = contour_secteur(u0, u1, r0, r1, s, DEMI_JOINT + BISEAU, lambda v: v)
            # Échantillons de la dalle pour savoir si un coffre ou le personnage s'y pose.
            echantillons = [point_sol(u0 + (u1 - u0) * a / 4, r0 + (r1 - r0) * b / 4, 0)
                            for a in range(5) for b in range(5)]
            h = 0.0 if dans_zone_plate(echantillons) else -random.uniform(0, VARIATION)
            mat = 1 if random.random() < PART_VARIANTE else 0
            paver(bm, point_sol,
                  [(contour_secteur(u0, u1, r0, r1, s, DEMI_JOINT, lambda v: v), CREUX + h * 0.5),
                   (dessus, h)], mat=mat, inverser=True)
    obj = C.objet_depuis_bmesh("Dalles", bm, ["M_PierreDalle", "M_PierreDalleVariante"])
    C.lisser_angle(obj, 30)
    return obj


def decaler_polygone(points, d):
    """Rentre un polygone (sens inverse des aiguilles d'une montre vu de dessus) de `d` mètres
    vers l'intérieur, avec des angles en onglet (chaque côté reste parallèle à l'original)."""
    res = []
    n = len(points)
    for i in range(n):
        p_av, p, p_ap = points[i - 1], points[i], points[(i + 1) % n]
        e1 = (p - p_av).normalized()
        e2 = (p_ap - p).normalized()
        n1 = Vector((-e1.y, e1.x, 0))   # normale vers l'intérieur (à gauche du côté)
        n2 = Vector((-e2.y, e2.x, 0))
        res.append(p + (n1 + n2) * (d / (1 + n1.dot(n2))))
    return res


def construire_rosace():
    """Étoile à 8 branches : remplissage clair, filet violet autour, cercle violet.
    Tout est plat, 4 mm au-dessus de la dalle centrale (pas de côtés : invisibles à 8 m)."""
    bm = bmesh.new()
    z = RELIEF_ROSACE
    # Étoile : 16 points, une pointe tous les 45° (dont une vers la caméra et une vers
    # chaque coffre latéral).
    etoile = []
    for k in range(16):
        a = k * math.pi / 8
        r = R_ETOILE if k % 2 == 0 else R_CREUX_ETOILE
        etoile.append(Vector((r * math.cos(a), r * math.sin(a), z)))
    interieur = decaler_polygone(etoile, FILET)

    E = [bm.verts.new(p) for p in etoile]
    I = [bm.verts.new(p) for p in interieur]
    centre = bm.verts.new((0, 0, z))
    for i in range(16):
        j = (i + 1) % 16
        bm.faces.new((E[i], E[j], I[j], I[i])).material_index = 1    # filet violet
        bm.faces.new((centre, I[i], I[j])).material_index = 0         # remplissage clair

    # Cercle violet (32 segments : un sommet sur chaque pointe de l'étoile).
    n = 32
    def cercle(r):
        return [bm.verts.new((r * math.cos(2 * math.pi * k / n),
                              r * math.sin(2 * math.pi * k / n), z)) for k in range(n)]
    dedans, dehors = cercle(R_ETOILE), cercle(R_CERCLE)
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((dehors[i], dehors[j], dedans[j], dedans[i])).material_index = 1
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)  # pointes de l'étoile = cercle
    return C.objet_depuis_bmesh("Rosace", bm, ["M_PierreClaire", "M_Incrustation"])


def construire_margelle():
    """16 blocs clairs : dessus à +4 cm, biseau 3 cm, face extérieure qui descend à -0,30."""
    bm = bmesh.new()
    nb, s = 16, 3
    pas = 2 * math.pi / nb
    decalage = math.radians(11.25)   # joints jamais alignés avec ceux de la couronne 3
    v1 = R_MARGELLE + DEMI_JOINT      # pour que la face extérieure tombe pile en r = 5
    for k in range(nb):
        u0 = decalage + k * pas
        u1 = u0 + pas
        def c(retrait):
            return contour_secteur(u0, u1, R_COURONNE3, v1, s, retrait, lambda v: v)
        paver(bm, point_sol, [
            (c(DEMI_JOINT), BAS_MARGELLE),
            (c(DEMI_JOINT), HAUT_MARGELLE - BISEAU_MARGELLE),
            (c(DEMI_JOINT + BISEAU_MARGELLE), HAUT_MARGELLE),
        ], mat=0, inverser=True)
    obj = C.objet_depuis_bmesh("Margelle", bm, ["M_PierreClaire"])
    C.lisser_angle(obj, 30)
    return obj


def construire_flanc():
    """Muraille : 4 rangées de 20 gros blocs, décalées d'un demi-bloc une rangée sur deux."""
    bm = bmesh.new()
    pas = 2 * math.pi / BLOCS_PAR_RANGEE
    hauteur = (BAS_MARGELLE - BAS_FLANC) / RANGEES_FLANC
    for rangee in range(RANGEES_FLANC):
        haut = BAS_MARGELLE - rangee * hauteur
        bas = haut - hauteur
        decalage = (rangee % 2) * pas / 2
        for k in range(BLOCS_PAR_RANGEE):
            u0 = decalage + k * pas
            u1 = u0 + pas
            def c(retrait):
                return contour_secteur(u0, u1, bas, haut, 2, retrait, lambda v: R_FLANC)
            paver(bm, point_flanc, [
                (c(DEMI_JOINT), R_FLANC - BISEAU_FLANC),
                (c(DEMI_JOINT + BISEAU_FLANC), R_FLANC),
            ], mat=0, inverser=False)
    obj = C.objet_depuis_bmesh("Flanc", bm, ["M_PierreFlanc"])
    C.lisser_angle(obj, 30)  # les faces avant des blocs restent lisses (bloc courbe)
    return obj


def construire_joints():
    """Noyau sombre : un cylindre fermé (dessus au fond des joints, en z = -0,02) qu'on ne voit
    qu'entre les dalles et entre les blocs. Il évite de voir « à travers » les joints."""
    bm = bmesh.new()
    hauteur = CREUX - BAS_FLANC
    bmesh.ops.create_cone(bm, cap_ends=True, segments=48, radius1=R_NOYAU, radius2=R_NOYAU,
                          depth=hauteur)
    bmesh.ops.translate(bm, verts=bm.verts, vec=(0, 0, CREUX - hauteur / 2))
    return C.objet_depuis_bmesh("Joints", bm, ["M_PierreJoint"])


def verifier_normales(obj, attendu):
    """Contrôle : compte les faces dont la normale ne va pas dans le sens attendu.
    `attendu(centre_face)` renvoie la direction « dehors » attendue."""
    mauvaises = 0
    for f in obj.data.polygons:
        if f.normal.dot(attendu(Vector(f.center))) < -1e-4:
            mauvaises += 1
    print(f"  normales {obj.name:<9}: {mauvaises} face(s) à l'envers sur {len(obj.data.polygons)}")
    return mauvaises


def construire():
    C.nouvelle_scene()
    C.graine(7)  # même script = même dallage (mêmes teintes, mêmes hauteurs)
    racine = C.vide("Sol")
    parties = [construire_dalles(), construire_rosace(), construire_margelle(),
               construire_flanc(), construire_joints()]
    for o in parties:
        C.parenter(o, racine)

    # Contrôle des normales : le dessus vers le haut, le flanc vers l'extérieur.
    print("\n=== Contrôle des normales ===")
    haut = lambda p: Vector((0, 0, 1))
    radial = lambda p: Vector((p.x, p.y, 0)).normalized()
    verifier_normales(bpy.data.objects["Rosace"], haut)
    verifier_normales(bpy.data.objects["Flanc"], radial)
    for nom in ("Dalles", "Margelle"):
        # Un pavé « en coussin » : toutes ses faces regardent vers le haut ou vers le côté,
        # jamais vers le bas.
        obj = bpy.data.objects[nom]
        bas = sum(1 for f in obj.data.polygons if f.normal.z < -1e-4)
        print(f"  normales {nom:<9}: {bas} face(s) tournée(s) vers le bas")
    return racine


def rendus_de_controle():
    """Rendus de vérification avec les vrais coffres et le personnage (importés depuis leurs
    FBX, puis supprimés : ils ne font PAS partie de sol.fbx)."""
    importes = []
    for x in (-2.6, 0.0, 2.6):
        importes += C.importer_fbx(os.path.join(C.DOSSIER_MODELES, "coffre.fbx"), (x, 0, 0))
    perso = os.path.join(C.DOSSIER_MODELES, "personnage.fbx")
    if os.path.exists(perso):
        importes += C.importer_fbx(perso, (0, 0, 2))
    C.rendre_vue_jeu("blender_sol.png")
    for o in importes:
        bpy.data.objects.remove(o, do_unlink=True)
    # Vue d'ensemble plus lointaine (même angle) : sol seul, pour juger la tour entière.
    C.rendre_vue_jeu("blender_sol_ensemble.png", camera_unity=(0, 6.5, -10.5), inclinaison=30)


if __name__ == "__main__":
    racine = construire()
    C.rapport(racine)
    rendus_de_controle()
    chemin = C.exporter_fbx(racine, "sol.fbx")
    C.verifier_fbx(chemin)
