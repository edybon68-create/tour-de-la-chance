"""
objets_icones_v7.py — Objets des icônes V7 rendues dans Blender (au lieu de Gemini).

Ce fichier ne lance rien tout seul : il prête des fonctions `construire_...()` que
`rendu_icones.py` appelle (source = ("fonction", "objets_icones_v7.construire_...")).
Même style « jouet verni » et même lumière que les autres icônes rendues ; transparence
parfaite (aucun fond à détourer).

Point 15 (Boutique) — 3 icônes NEUTRES de catégories de boîtes (ni skin, ni accessoire) :
  - construire_categorie_classiques() : une grosse étoile de fête dorée et bombée, avec deux
    petites étoiles et des confettis ;
  - construire_categorie_pirates()    : une carte au trésor roulée, à moitié ouverte (chemin
    en pointillés et croix rouge), nouée d'un ruban rouge ;
  - construire_categorie_animaux()    : une empreinte de patte (coussinet + 4 doigts) posée
    sur une grande feuille verte.

Lots d'échange (docs/specs/v7.md §1) — une même famille d'images, la quantité de pièces
grandit nettement de l'une à l'autre :
  poignée < pochette < sac < seau < coffret < malle < tonneau < montagne.
Toutes les pièces sont la MÊME pièce d'or (copies liées d'un seul maillage) : rebord, étoile
en relief au centre, comme l'icône `piece` du jeu.

Repère : Z en haut, l'avant de l'objet regarde vers +Y (comme les autres icônes rendues).
L'échelle n'a pas d'importance : la caméra cadre l'objet. 1 unité ≈ le diamètre d'une pièce.
"""

import math
import os
import random
import sys

import bmesh
import bpy
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402
from objets_lot9 import matiere, objet, revolution, etincelle, vers_camera_icone  # noqa: E402


# =========================================================================================
# La pièce d'or (un seul maillage, copié partout)
# =========================================================================================
R_PIECE = 0.5          # rayon d'une pièce
E_PIECE = 0.12         # épaisseur


def mats_or():
    return (matiere("Or", "#FFC83A", 0.30, metal=0.55, vernis=0.35),
            matiere("OrFonce", "#E09A1E", 0.35, metal=0.5, vernis=0.3))


def maillage_piece(or_, or_fonce):
    """Pièce bombée : tranche foncée, rebord en relief, étoile en relief au centre (2 tons)."""
    bm = bmesh.new()
    r, e = R_PIECE, E_PIECE
    profil = [(0.0, e / 2 - 0.015), (r * 0.62, e / 2 - 0.015), (r * 0.70, e / 2 + 0.012),
              (r * 0.86, e / 2 + 0.012), (r * 0.97, e / 2), (r, e / 4), (r, -e / 4),
              (r * 0.97, -e / 2), (r * 0.86, -e / 2 - 0.012), (r * 0.70, -e / 2 - 0.012),
              (r * 0.62, -e / 2 + 0.015), (0.0, -e / 2 + 0.015)]
    mats = [0, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0]
    C.bm_revolution(bm, profil, 32, mats=mats)
    # étoile en relief sur les deux faces
    for signe in (1, -1):
        m = Matrix.Translation((0, 0, signe * (e / 2 - 0.015))) @ \
            Matrix.Rotation(math.radians(-90 if signe > 0 else 90), 4, "X")
        C.bm_etoile(bm, r * 0.42, 0.03, m, mat=1, bombe=0.012)
    me = bpy.data.meshes.new("Piece")
    bm.to_mesh(me)
    bm.free()
    me.materials.append(or_)
    me.materials.append(or_fonce)
    me.shade_smooth()
    me.set_sharp_from_angle(angle=math.radians(35))
    return me


class Pieces:
    """Pose des copies liées de la pièce. `poser(position, normale)` couche la pièce sur
    une surface (avec un peu de hasard), `debout(position, angle)` la met sur la tranche."""

    def __init__(self, racine, graine=1):
        self.or_, self.or_fonce = mats_or()
        self.me = maillage_piece(self.or_, self.or_fonce)
        self.racine = racine
        self.h = random.Random(graine)
        self.n = 0

    def _copie(self):
        o = bpy.data.objects.new(f"Piece_{self.n:03d}", self.me)
        bpy.context.scene.collection.objects.link(o)
        C.parenter(o, self.racine)
        self.n += 1
        return o

    def poser(self, p, normale=(0, 0, 1), penche=0.35, echelle=1.0):
        o = self._copie()
        n = Vector(normale).normalized()
        q = n.to_track_quat("Z", "Y")
        rot = q.to_matrix().to_4x4() @ Matrix.Rotation(self.h.uniform(-penche, penche), 4, "X") \
            @ Matrix.Rotation(self.h.uniform(-penche, penche), 4, "Y") \
            @ Matrix.Rotation(self.h.uniform(0, 6.28), 4, "Z")
        o.matrix_world = Matrix.Translation(p) @ rot @ Matrix.Scale(echelle, 4)
        return o

    def debout(self, p, angle_z=0.0, penche=0.0):
        o = self._copie()
        rot = Matrix.Rotation(angle_z, 4, "Z") @ Matrix.Rotation(math.radians(90) + penche, 4, "X")
        o.matrix_world = Matrix.Translation(p) @ rot
        return o

    def pile(self, x, y, z0, nombre):
        """Une pile droite de `nombre` pièces (légèrement décalées)."""
        for k in range(nombre):
            self.poser(Vector((x + self.h.uniform(-0.04, 0.04), y + self.h.uniform(-0.04, 0.04),
                               z0 + E_PIECE * 0.5 + k * E_PIECE * 1.02)), penche=0.04)

    def tas(self, cx, cy, z0, rayon, hauteur, nombre, graine_angle=0.0):
        """Un tas en dôme : les pièces sont posées sur la surface z = h·(1 − (r/R)²),
        couchées selon la pente (plus serrées en haut)."""
        for k in range(nombre):
            t = math.sqrt((k + 0.5) / nombre)                # répartition uniforme en surface
            r = rayon * t
            a = k * 2.399963 + graine_angle                  # angle d'or : pas de trous
            x, y = cx + r * math.cos(a), cy + r * math.sin(a)
            z = z0 + hauteur * (1 - t * t)
            pente = 2 * hauteur * t / rayon                   # dz/dr
            n = Vector((-math.cos(a) * pente, -math.sin(a) * pente, 1.0))
            self.poser(Vector((x, y, z + E_PIECE * 0.5)), n, penche=0.25)


def eclats(racine, points, taille=0.22):
    blanc = matiere("Eclat", "#FFFFFF", 0.3, emission=3.0)
    cam = vers_camera_icone()
    for i, p in enumerate(points):
        etincelle(f"Eclat_{i}", p, cam, taille, blanc, racine)


# =========================================================================================
# Lots d'échange
# =========================================================================================
def construire_echange_poignee():
    C.nouvelle_scene()
    racine = C.vide("EchangePoignee")
    P = Pieces(racine, 2)
    P.tas(0, 0, 0, 0.9, 0.35, 5)
    P.debout(Vector((0.55, 0.35, R_PIECE)), 0.5, -0.25)
    return racine


def construire_echange_pochette():
    """Petite bourse de cuir fermée par un cordon, 3 pièces devant."""
    C.nouvelle_scene()
    racine = C.vide("EchangePochette")
    cuir = matiere("Cuir", "#9A5A32", 0.5, vernis=0.15)
    cuir_f = matiere("CuirFonce", "#6E3C20", 0.5, vernis=0.1)
    corde = matiere("Cordon", "#E8C27A", 0.6, vernis=0.05)
    # corps de la bourse : révolution « poire », col froncé, collerette évasée
    profil = [(0.0, 0.0), (0.55, 0.05), (0.82, 0.35), (0.86, 0.7), (0.66, 1.05), (0.30, 1.30),
              (0.24, 1.38), (0.42, 1.62), (0.36, 1.66), (0.0, 1.50)]
    o = revolution("Bourse", profil, [0, 0, 0, 0, 0, 0, 1, 1, 1], [cuir, cuir_f], racine, 32)
    # cordon autour du col + deux bouts qui pendent
    bm = bmesh.new()
    C.bm_tore(bm, 0.27, 0.05, segments=24, segments_tube=8, mat=0,
              matrice=Matrix.Translation((0, 0, 1.33)))
    for s in (-1, 1):
        C.bm_tube(bm, [Vector((s * 0.12, 0.25, 1.30)), Vector((s * 0.22, 0.42, 1.0)),
                       Vector((s * 0.20, 0.50, 0.78))], 0.04, cotes=8, mat=0)
        C.bm_sphere(bm, 0.08, (s * 0.20, 0.50, 0.74), segments=12, anneaux=8, mat=0)
    objet("Cordon", bm, [corde], racine)
    P = Pieces(racine, 3)
    P.debout(Vector((0.9, 0.55, R_PIECE)), 0.6, -0.15)
    P.poser(Vector((-0.75, 0.75, E_PIECE / 2)), penche=0.1)
    P.poser(Vector((-0.55, 0.85, E_PIECE * 1.5)), penche=0.15)
    return racine


def construire_echange_sac():
    """Sac de toile ouvert qui déborde de pièces."""
    C.nouvelle_scene()
    racine = C.vide("EchangeSac")
    toile = matiere("Toile", "#C9A46A", 0.7, vernis=0.05)
    toile_f = matiere("ToileFonce", "#9C7A48", 0.7, vernis=0.05)
    corde = matiere("Corde", "#7A4A2A", 0.6, vernis=0.05)
    profil = [(0.0, 0.0), (0.9, 0.08), (1.18, 0.55), (1.15, 1.05), (0.95, 1.40), (1.05, 1.55),
              (0.98, 1.60), (0.85, 1.45), (0.0, 1.30)]
    revolution("Sac", profil, [0, 0, 0, 0, 1, 1, 1, 1], [toile, toile_f], racine, 32)
    bm = bmesh.new()
    C.bm_tore(bm, 0.97, 0.05, segments=32, segments_tube=8, mat=0,
              matrice=Matrix.Translation((0, 0, 1.38)))
    objet("Corde", bm, [corde], racine)
    P = Pieces(racine, 4)
    P.tas(0, 0, 1.30, 0.95, 0.45, 16)
    for x, y, a in ((1.4, 0.6, 0.3), (-1.3, 0.8, -0.4)):
        P.debout(Vector((x, y, R_PIECE)), a, -0.2)
    P.poser(Vector((0.6, 1.3, E_PIECE / 2)), penche=0.1)
    P.poser(Vector((-0.4, 1.4, E_PIECE / 2)), penche=0.1)
    return racine


def construire_echange_seau():
    """Seau de bois cerclé de fer, plein à ras bord, avec une anse."""
    C.nouvelle_scene()
    racine = C.vide("EchangeSeau")
    bois = matiere("Bois", "#B0743E", 0.55, vernis=0.15)
    bois_f = matiere("BoisFonce", "#7E4F28", 0.55, vernis=0.1)
    fer = matiere("Fer", "#5A6378", 0.35, metal=0.6, vernis=0.2)
    R0, R1, H = 1.0, 1.25, 1.5
    profil = [(0.0, 0.0), (R0, 0.0), (R1, H), (R1 - 0.12, H), (R0 - 0.12, 0.12), (0.0, 0.12)]
    o = revolution("Seau", profil, [0, 0, 1, 1, 1], [bois, bois_f], racine, 36, angle=50)
    # douelles : rainures sombres verticales
    bm = bmesh.new()
    for k in range(12):
        a = 2 * math.pi * k / 12
        c, s = math.cos(a), math.sin(a)
        C.bm_tube(bm, [Vector((c * (R0 + 0.01), s * (R0 + 0.01), 0.05)),
                       Vector((c * (R1 + 0.01), s * (R1 + 0.01), H - 0.05))], 0.02, cotes=4, mat=0)
    objet("Douelles", bm, [bois_f], racine)
    bm = bmesh.new()
    for z, r in ((0.3, R0 + (R1 - R0) * 0.2), (H - 0.3, R0 + (R1 - R0) * 0.8)):
        C.bm_tore(bm, r + 0.02, 0.06, segments=36, segments_tube=6, mat=0,
                  matrice=Matrix.Translation((0, 0, z)))
    # anse
    arc = [Vector((math.cos(t) * (R1 + 0.02), 0.0, H - 0.1 + math.sin(t) * 1.0))
           for t in [math.pi * k / 10 for k in range(11)]]
    C.bm_tube(bm, arc, 0.05, cotes=8, mat=0)
    objet("Fer", bm, [fer], racine)
    P = Pieces(racine, 5)
    P.tas(0, 0, H - 0.1, R1 - 0.05, 0.55, 26)
    for x, y, a in ((1.6, 0.7, 0.4), (-1.5, 0.9, -0.5), (0.4, 1.6, 0.1)):
        P.debout(Vector((x, y, R_PIECE)), a, -0.2)
    P.pile(1.0, 1.3, 0.0, 3)
    return racine


def coffret(racine, largeur, profondeur, hauteur, couleur, couleur_f, nom):
    """Caisse ouverte (boîte creuse) avec un couvercle bombé basculé en arrière."""
    corps = matiere(nom, couleur, 0.45, vernis=0.3)
    corps_f = matiere(nom + "Fonce", couleur_f, 0.5, vernis=0.2)
    laiton = matiere("Laiton", "#F0B53A", 0.3, metal=0.55, vernis=0.3)
    L, Pp, H, e = largeur, profondeur, hauteur, 0.12
    bm = bmesh.new()
    C.bm_boite(bm, (L, Pp, e), (0, 0, e / 2), mat=1)
    for sx in (-1, 1):
        C.bm_boite(bm, (e, Pp, H), (sx * (L - e) / 2, 0, H / 2), mat=0)
    for sy in (-1, 1):
        C.bm_boite(bm, (L, e, H), (0, sy * (Pp - e) / 2, H / 2), mat=0)
    objet("Caisse", bm, [corps, corps_f], racine, biseau=0.04, seg=2, angle=30)
    # couvercle bombé basculé en arrière (charnière au bord arrière du haut)
    bm = bmesh.new()
    prof = []
    for k in range(9):
        t = math.pi * k / 8
        prof.append((Pp / 2 * math.cos(t), H * 0.35 * math.sin(t)))
    C.bm_prisme(bm, [(y, z) for y, z in prof], L, axe="X", decalage=-L / 2, mat=0)
    couv = objet("Couvercle", bm, [corps], racine, biseau=0.03, seg=2, angle=35)
    couv.matrix_world = Matrix.Translation((0, -Pp / 2, H)) @ Matrix.Rotation(math.radians(-105), 4, "X") \
        @ Matrix.Translation((0, Pp / 2, 0))
    # coins et bandes de laiton
    bm = bmesh.new()
    for sx in (-1, 1):
        for sy in (-1, 1):
            C.bm_boite(bm, (0.2, 0.2, H + 0.02), (sx * (L / 2 - 0.08), sy * (Pp / 2 - 0.08), H / 2), mat=0)
    C.bm_boite(bm, (0.28, 0.06, 0.34), (0, Pp / 2 + 0.02, H - 0.2), mat=0)
    objet("Laiton", bm, [laiton], racine, biseau=0.03, seg=2, angle=30)
    return H


def construire_echange_coffre():
    """Coffret rouge ouvert plein de pièces (pas un coffre d'étage)."""
    C.nouvelle_scene()
    racine = C.vide("EchangeCoffre")
    H = coffret(racine, 2.2, 1.5, 1.0, "#C8343A", "#8A1E24", "Coffret")
    P = Pieces(racine, 6)
    P.tas(0, 0, H - 0.15, 0.95, 0.55, 34)
    for x, y, a in ((1.5, 1.0, 0.3), (-1.5, 1.1, -0.4)):
        P.debout(Vector((x, y, R_PIECE)), a, -0.2)
    P.pile(0.9, 1.4, 0.0, 4)
    P.pile(-0.6, 1.5, 0.0, 2)
    return racine


def construire_echange_malle():
    """Grande malle de voyage bleue à coins de laiton, qui déborde."""
    C.nouvelle_scene()
    racine = C.vide("EchangeMalle")
    H = coffret(racine, 3.2, 1.9, 1.6, "#2F5FB0", "#1E3C78", "Malle")
    P = Pieces(racine, 7)
    P.tas(0, 0, H - 0.1, 1.45, 0.85, 60)
    P.tas(0.2, 1.6, 0.0, 0.9, 0.35, 10, 1.0)
    for x, y, a in ((2.1, 1.0, 0.3), (-2.0, 1.3, -0.4), (-1.0, 1.9, 0.2)):
        P.debout(Vector((x, y, R_PIECE)), a, -0.2)
    P.pile(1.5, 1.6, 0.0, 5)
    eclats(racine, [Vector((1.3, 0.5, H + 1.2)), Vector((-1.4, 0.4, H + 0.9))])
    return racine


def construire_echange_tonneau():
    """Tonneau de pirate couché et éventré : les pièces coulent en cascade."""
    C.nouvelle_scene()
    racine = C.vide("EchangeTonneau")
    bois = matiere("Bois", "#A8682F", 0.55, vernis=0.15)
    bois_f = matiere("BoisFonce", "#6A3E1C", 0.55, vernis=0.1)
    fer = matiere("Fer", "#4A5266", 0.35, metal=0.6, vernis=0.2)
    # tonneau bombé, axe Y couché (l'ouverture vers l'avant +Y), creux
    L, Rm, Rb = 2.6, 1.25, 1.0
    prof_ext = [(Rb + (Rm - Rb) * math.sin(math.pi * k / 10), -L / 2 + L * k / 10) for k in range(11)]
    prof = prof_ext + [(Rb - 0.12, L / 2 - 0.02), (Rb - 0.12, -L / 2 + 0.12), (0.0, -L / 2 + 0.12)]
    prof = [(0.0, -L / 2)] + prof
    bm = bmesh.new()
    C.bm_revolution(bm, prof, 32, mats=[1] + [0] * 10 + [1, 1, 1])
    bmesh.ops.rotate(bm, verts=bm.verts, cent=(0, 0, 0), matrix=Matrix.Rotation(math.radians(-90), 3, "X"))
    bmesh.ops.translate(bm, vec=(0, -0.3, Rm), verts=bm.verts)
    objet("Tonneau", bm, [bois, bois_f], racine, angle=50)
    bm = bmesh.new()
    for yy, r in ((-0.85, Rb + (Rm - Rb) * 0.85), (0.85, Rb + (Rm - Rb) * 0.85), (1.22, Rb + 0.03)):
        C.bm_tore(bm, r + 0.03, 0.07, segments=32, segments_tube=6, mat=0,
                  matrice=Matrix.Translation((0, yy - 0.3, Rm)) @ Matrix.Rotation(math.radians(90), 4, "X"))
    objet("Cercles", bm, [fer], racine)
    P = Pieces(racine, 8)
    # pièces dans l'ouverture et cascade devant
    P.tas(0, 0.75, Rm - 0.6, 0.85, 0.5, 14)
    P.tas(0, 2.1, 0.0, 1.6, 0.8, 60, 0.7)
    for x, y, a in ((1.9, 2.4, 0.3), (-1.8, 2.5, -0.4), (0.9, 3.4, 0.1), (-0.8, 3.3, 0.5)):
        P.debout(Vector((x, y, R_PIECE)), a, -0.2)
    P.pile(-1.9, 1.2, 0.0, 6)
    eclats(racine, [Vector((1.4, 2.2, 1.6)), Vector((-1.2, 1.8, 2.6))])
    return racine


def construire_echange_montagne():
    """Une montagne de pièces qui brille, avec des piles autour."""
    C.nouvelle_scene()
    racine = C.vide("EchangeMontagne")
    P = Pieces(racine, 9)
    # un noyau doré (pour qu'on ne voie pas à travers le tas) puis 3 couches de pièces
    or_, _ = P.or_, P.or_fonce
    prof = [(0.0, 0.0), (2.4, 0.0)] + [(2.4 * (1 - k / 8), 2.0 * (1 - (1 - k / 8) ** 2) - 0.12)
                                        for k in range(1, 9)]
    revolution("Noyau", prof, [0] * (len(prof) - 1), [P.or_fonce], racine, 32)
    P.tas(0, 0, 0.0, 2.5, 2.0, 170)
    P.tas(0, 0, 0.12, 2.0, 1.9, 60, 1.3)
    for x, y, n in ((2.4, 1.4, 7), (-2.5, 1.2, 5), (1.2, 2.5, 4), (-1.0, 2.6, 3), (3.0, -0.2, 6)):
        P.pile(x, y, 0.0, n)
    for x, y, a in ((0.2, 3.0, 0.2), (2.0, 2.4, 0.6), (-2.1, 2.2, -0.5)):
        P.debout(Vector((x, y, R_PIECE)), a, -0.2)
    eclats(racine, [Vector((0.9, 0.4, 2.8)), Vector((-1.4, 0.6, 2.2)), Vector((1.9, 0.3, 1.6)),
                    Vector((-0.2, 0.2, 3.3))], taille=0.3)
    return racine


# =========================================================================================
# Point 15 : icônes de catégories (neutres)
# =========================================================================================
def construire_categorie_classiques():
    """Grosse étoile de fête dorée, bien bombée, avec deux petites étoiles et des confettis."""
    C.nouvelle_scene()
    racine = C.vide("CategorieClassiques")
    or_ = matiere("Or", "#FFC83A", 0.28, metal=0.5, vernis=0.4)
    or_f = matiere("OrFonce", "#E89A1E", 0.32, metal=0.5, vernis=0.3)
    rose = matiere("Rose", "#FF6FB5", 0.4, vernis=0.3)
    turq = matiere("Turquoise", "#2BB8C4", 0.4, vernis=0.3)
    viol = matiere("Violet", "#8A5CF0", 0.4, vernis=0.3)
    # étoile bombée (coussin), face vers +Y, debout
    face = Matrix.Rotation(math.radians(-8), 4, "Z")
    bm = bmesh.new()
    contour = C.contour_etoile(1.0, 5, 0.50)
    # contour plus fin (arrondi des pointes : on suréchantillonne)
    fin = []
    for i in range(len(contour)):
        a, b = Vector(contour[i]), Vector(contour[(i + 1) % len(contour)])
        for t in (0.0, 0.33, 0.66):
            fin.append(tuple(a.lerp(b, t)))
    C.bm_coussin(bm, fin, 0.22, 0.55, face @ Matrix.Translation((0, 0, 1.15)), mat=0,
                 mat_dos=1, anneaux=4)
    objet("Etoile", bm, [or_, or_f], racine, angle=60, biseau=0.04, seg=3)
    # liseré : une étoile un peu plus grande et plus foncée derrière (2e ton)
    bm = bmesh.new()
    C.bm_coussin(bm, [(x * 1.08, z * 1.08) for x, z in fin], 0.18, 0.2,
                 face @ Matrix.Translation((0, -0.12, 1.15)), mat=0, anneaux=1)
    objet("Lisere", bm, [or_f], racine, angle=60, biseau=0.03, seg=2)
    # deux petites étoiles
    for (x, z, r, m) in ((1.25, 2.0, 0.32, rose), (-1.3, 0.55, 0.26, turq)):
        bm = bmesh.new()
        C.bm_coussin(bm, C.contour_etoile(r, 5, 0.5), 0.07, 0.16,
                     face @ Matrix.Translation((x, 0.1, z)), mat=0, anneaux=2)
        objet("PetiteEtoile", bm, [m], racine, angle=60, biseau=0.02, seg=2)
    # confettis (petites bandes et ronds)
    h = random.Random(5)
    for k, (x, z) in enumerate(((-1.2, 2.0), (1.35, 0.6), (-0.6, 2.45), (0.9, 2.55), (1.55, 1.35))):
        bm = bmesh.new()
        m = [rose, turq, viol][k % 3]
        if k % 2:
            C.bm_sphere(bm, 0.09, (0, 0, 0), segments=12, anneaux=8, mat=0)
        else:
            C.bm_boite(bm, (0.28, 0.06, 0.1), (0, 0, 0), mat=0)
        o = objet("Confetti", bm, [m], racine, angle=40, biseau=0.02 if k % 2 == 0 else 0, seg=2)
        o.location = (x, 0.3, z)
        o.rotation_euler = (h.uniform(-1, 1), h.uniform(-1, 1), h.uniform(-1, 1))
    return racine


def construire_categorie_pirates():
    """Carte au trésor roulée, à moitié ouverte : chemin en pointillés et croix rouge,
    nouée d'un ruban rouge. Aucune tête de mort."""
    C.nouvelle_scene()
    racine = C.vide("CategoriePirates")
    papier = matiere("Parchemin", "#F3D58E", 0.6, vernis=0.1)
    papier_f = matiere("ParcheminFonce", "#C9963E", 0.6, vernis=0.1)
    rouge = matiere("Rouge", "#D63A3A", 0.4, vernis=0.3)
    bleu = matiere("Mer", "#5CB8D8", 0.5, vernis=0.1)
    vert = matiere("Ile", "#6CC46A", 0.5, vernis=0.1)
    brun = matiere("Encre", "#7A4A2A", 0.5, vernis=0.1)
    # la partie déroulée : une feuille légèrement ondulée (grille), dans le plan XY, posée
    W, Lg, nx, ny = 2.6, 1.9, 16, 10
    bm = bmesh.new()
    grille = []
    for j in range(ny + 1):
        rang = []
        for i in range(nx + 1):
            x = -W / 2 + W * i / nx
            y = -Lg / 2 + Lg * j / ny
            z = 0.06 * math.sin(i / nx * math.pi * 2) + 0.04 * math.sin(j / ny * math.pi * 1.5)
            rang.append(bm.verts.new((x, y, z)))
        grille.append(rang)
    for j in range(ny):
        for i in range(nx):
            bm.faces.new((grille[j][i], grille[j][i + 1], grille[j + 1][i + 1], grille[j + 1][i]))
    bmesh.ops.solidify(bm, geom=bm.faces[:], thickness=0.05)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    for f in bm.faces:
        f.material_index = 0 if f.normal.z > 0.3 else 1
    feuille = objet("Feuille", bm, [papier, papier_f], racine, angle=50)
    # les deux rouleaux aux bouts (gauche gros, droite petit)
    for x, r in ((-W / 2 - 0.15, 0.32), (W / 2 + 0.1, 0.22)):
        bm = bmesh.new()
        prof = [(0.0, -Lg / 2 - 0.05), (r, -Lg / 2 - 0.05), (r, Lg / 2 + 0.05), (0.0, Lg / 2 + 0.05)]
        C.bm_revolution(bm, prof, 28, mats=[1, 0, 1])
        bmesh.ops.rotate(bm, verts=bm.verts, cent=(0, 0, 0), matrix=Matrix.Rotation(math.radians(-90), 3, "X"))
        bmesh.ops.translate(bm, vec=(x, 0, r - 0.05), verts=bm.verts)
        # spirale visible au bout : un tore aplati plus foncé
        objet("Rouleau", bm, [papier, papier_f], racine, angle=50)
    # dessin sur la carte : mer, île, chemin en pointillés, croix
    z0 = 0.09
    bm = bmesh.new()
    ile = [((0.62 + 0.12 * math.sin(3 * t) + 0.06 * math.cos(5 * t)) * math.cos(t),
            (0.45 + 0.08 * math.cos(4 * t)) * math.sin(t)) for t in [2 * math.pi * k / 32 for k in range(32)]]
    C.bm_coussin(bm, ile, 0.015, 0.06,
                 Matrix.Translation((0.45, 0.1, z0)) @ Matrix.Rotation(math.radians(90), 4, "X"), mat=0, anneaux=2)
    objet("Ile", bm, [vert], racine, angle=60)
    bm = bmesh.new()
    for k in range(9):              # chemin en pointillés qui mène à la croix
        t = k / 9
        x, y = -1.0 + 1.35 * t, -0.45 + 0.6 * t + 0.25 * math.sin(t * math.pi * 1.5)
        C.bm_boite(bm, (0.11, 0.06, 0.03), (x, y, z0 + 0.03), mat=0)
    # croix rouge
    for a in (45, -45):
        v = C.bm_boite(bm, (0.34, 0.08, 0.04), (0, 0, 0), mat=0)
        bmesh.ops.transform(bm, verts=v, matrix=Matrix.Translation((0.5, 0.15, z0 + 0.05))
                            @ Matrix.Rotation(math.radians(a), 4, "Z"))
    objet("Croix", bm, [rouge], racine, biseau=0.01, seg=2)
    bm = bmesh.new()
    for x, y, r in ((-0.7, 0.55, 0.18), (0.95, -0.6, 0.14)):
        C.bm_coussin(bm, C.contour_ellipse(r * 1.6, r, n=16), 0.005, 0.015,
                     Matrix.Translation((x, y, z0)) @ Matrix.Rotation(math.radians(90), 4, "X"), mat=0, anneaux=1)
    objet("Vagues", bm, [bleu], racine, angle=60)
    # rose des vents simplifiée : petite étoile brune
    bm = bmesh.new()
    C.bm_coussin(bm, C.contour_etoile(0.18, 4, 0.3), 0.01, 0.03,
                 Matrix.Translation((-0.85, -0.05, z0)) @ Matrix.Rotation(math.radians(90), 4, "X"), mat=0, anneaux=1)
    objet("RoseVents", bm, [brun], racine, angle=60)
    # ruban rouge noué autour du gros rouleau
    bm = bmesh.new()
    x = -W / 2 - 0.15
    C.bm_tore(bm, 0.335, 0.05, segments=24, segments_tube=8, mat=0,
              matrice=Matrix.Translation((x, 0.0, 0.27)) @ Matrix.Rotation(math.radians(90), 4, "X"))
    for s in (-1, 1):
        # deux boucles du nœud, sur le dessus du rouleau, tournées vers la caméra (+Y)
        C.bm_coussin(bm, C.contour_ellipse(0.2, 0.12, n=16), 0.05, 0.1,
                     Matrix.Translation((x, 0.1, 0.62)) @ Matrix.Rotation(math.radians(s * 30), 4, "Y")
                     @ Matrix.Translation((s * 0.2, 0, 0)), mat=0, anneaux=2)
    C.bm_sphere(bm, 0.09, (x, 0.12, 0.62), segments=12, anneaux=8, mat=0)
    objet("Ruban", bm, [rouge], racine, angle=60)
    # la carte se redresse vers la caméra (on lit le dessin), un peu de travers
    racine.rotation_euler = (math.radians(-50), 0, math.radians(-8))
    return racine


def construire_categorie_animaux():
    """Empreinte de patte (coussinet + 4 doigts) posée sur une grande feuille verte."""
    C.nouvelle_scene()
    racine = C.vide("CategorieAnimaux")
    vert = matiere("Feuille", "#4CC26A", 0.45, vernis=0.25)
    vert_f = matiere("FeuilleFonce", "#2E8A48", 0.45, vernis=0.2)
    patte = matiere("Patte", "#FF8FB8", 0.38, vernis=0.4)
    patte_f = matiere("PatteFonce", "#E0608E", 0.4, vernis=0.3)
    debout = Matrix.Rotation(math.radians(90), 4, "X")     # le plan XZ (face vers −Y) -> on tourne
    face = Matrix.Identity(4)                             # bm_coussin regarde déjà +Y (la caméra)
    # feuille pointue, bombée, inclinée de 28° DERRIÈRE la patte (y négatif = plus loin de
    # la caméra), avec une nervure centrale foncée posée sur sa face avant
    L = 3.3
    gauche, droite = [], []
    for k in range(17):
        t = k / 16
        w = 1.05 * math.sin(math.pi * t) ** 0.75 * (1 - 0.25 * t)
        z = -L / 2 + L * t
        droite.append((w, z))
        gauche.append((-w, z))
    pts = droite + list(reversed(gauche[1:-1]))
    m_f = Matrix.Translation((0.45, -0.35, 1.75)) @ Matrix.Rotation(math.radians(-28), 4, "Y")
    bm = bmesh.new()
    C.bm_coussin(bm, pts, 0.06, 0.14, m_f, mat=0, mat_dos=1, anneaux=3)
    objet("Feuille", bm, [vert, vert_f], racine, angle=60, biseau=0.02, seg=2)
    bm = bmesh.new()
    C.bm_tube(bm, [m_f @ Vector((0, 0.08, -L / 2 - 0.35)), m_f @ Vector((0, 0.08, -L / 2 + 0.2)),
                   m_f @ Vector((0, 0.09, 0.0)), m_f @ Vector((0, 0.07, L / 2 - 0.25))],
              0.05, cotes=8, mat=0, rayons=[1.0, 1.0, 0.8, 0.3])
    objet("Nervure", bm, [vert_f], racine)
    # empreinte : grand coussinet (triangle arrondi) + 4 doigts ovales, devant la feuille
    m0 = face @ Matrix.Translation((-0.15, 0.45, 1.35))   # devant la feuille (côté caméra)
    coussinet = []
    for k in range(32):
        t = 2 * math.pi * k / 32
        r = 0.62 * (1 + 0.12 * math.cos(3 * t + math.pi / 2))
        coussinet.append((r * math.cos(t) * 1.1, r * math.sin(t) * 0.85))
    bm = bmesh.new()
    C.bm_coussin(bm, coussinet, 0.12, 0.30, m0 @ Matrix.Translation((0, 0, -0.35)), mat=0,
                 mat_dos=1, anneaux=4)
    for x, z, a in ((-0.78, 0.45, 25), (-0.30, 0.82, 8), (0.30, 0.82, -8), (0.78, 0.45, -25)):
        C.bm_coussin(bm, C.contour_ellipse(0.24, 0.31, n=24), 0.1, 0.24,
                     m0 @ Matrix.Translation((x, 0, z)) @ Matrix.Rotation(math.radians(a), 4, "Y"),
                     mat=0, mat_dos=1, anneaux=3)
    objet("Patte", bm, [patte, patte_f], racine, angle=60, biseau=0.02, seg=2)
    return racine


# =========================================================================================
# Point 10 : pictogramme « pas encore vu » de l'Index
# =========================================================================================
def construire_pas_encore_vu():
    """Un gros point d'interrogation dodu en vinyle violet (lettre extrudée et très arrondie),
    posé sur un petit socle rond lavande cerclé d'or, avec deux éclats. Lisible à 96 px sur
    une case sombre : une seule forme claire et contrastée, un liseré clair (le socle)."""
    C.nouvelle_scene()
    racine = C.vide("PasEncoreVu")
    violet = matiere("Violet", "#9B5CF6", 0.32, vernis=0.5)
    violet_f = matiere("VioletFonce", "#6A34C8", 0.36, vernis=0.4)
    socle_m = matiere("Socle", "#D9CCF5", 0.4, vernis=0.3)
    or_ = matiere("Or", "#FFC83A", 0.3, metal=0.5, vernis=0.35)
    # le « ? » construit à la main (la police de Blender donnait un crochet pointu et un
    # point collé à la tige) : un gros tube rond qui suit le crochet, et une boule séparée.
    # Plan XZ, face vers +Y (la caméra des icônes).
    chemin = []
    for k in range(15):                      # crochet : de -150° (gauche) à -30° (bas droite)
        a = math.radians(160 - 230 * k / 14)
        chemin.append(Vector((-0.48 * math.cos(a), 0, 2.05 + 0.48 * math.sin(a))))   # (−x : vu de face)
    fin = chemin[-1]
    chemin += [fin.lerp(Vector((0.0, 0, 1.32)), t) for t in (0.4, 0.75)] + [Vector((0.0, 0, 1.25)),
                                                                         Vector((0.0, 0, 1.12))]
    bm = bmesh.new()
    C.bm_tube(bm, chemin, 0.25, cotes=20, mat=0, haut=Vector((0, 1, 0)))
    for bout in (chemin[0], chemin[-1]):     # bouts arrondis
        C.bm_sphere(bm, 0.25, bout, segments=20, anneaux=12, mat=0)
    C.bm_sphere(bm, 0.28, (0.0, 0.0, 0.52), segments=24, anneaux=14, mat=0)   # le point
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-4)
    q = objet("Question", bm, [violet, violet_f], racine, angle=60)
    q.rotation_euler = (0, math.radians(-8), 0)          # un peu penché (vivant)
    # socle rond, tranche dorée
    prof = [(0.0, 0.0), (0.78, 0.0), (0.84, 0.06), (0.84, 0.16), (0.78, 0.22), (0.0, 0.22)]
    revolution("Socle", prof, [0, 1, 1, 1, 0], [socle_m, or_], racine, 40)
    eclats(racine, [Vector((1.05, 0.3, 2.45)), Vector((-1.0, 0.3, 1.2))], taille=0.25)
    return racine
