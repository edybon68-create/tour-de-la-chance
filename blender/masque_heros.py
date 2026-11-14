"""
masque_heros.py — Masque de héros (boîte d'accessoires n°1 : `masque_heros`, Rare, Visage).
VERSION CHEVAL (v5), framboise #D63A6E à galon jaune #FFD54A (arbitrage du DA, bible §2.8).

Lancer (Blender fermé, ou en tout cas pas besoin de l'ouvrir) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/masque_heros.py
Sortie : blender/exports/masque_heros.fbx (dossier d'attente) et docs/captures/v5_cheval_masque_heros.png.

Pourquoi refait ?
    L'ancien masque était un bandeau posé seulement DEVANT : de grands triangles plats
    laissaient la peau du cheval ressortir à travers (taches bleues sur le masque), le bas
    pendait comme une visière devant le museau, et les deux « pans » sortaient sur les côtés
    comme des ailerons, sans rien tenir. Et il était encore bleu, la couleur abandonnée.

Comment il est construit :
  1. on dessine le masque À PLAT, comme un patron de couture, dans un repère (s, h) en
     mètres : s = le tour de la tête (0 devant, ±0,94 derrière), h = la hauteur (0 à la
     hauteur des yeux). Devant, une large plaque percée de deux trous ovales autour des
     yeux ; sur les côtés, elle devient un lien de 9 cm qui fait TOUT le tour de la tête ;
     le patron est rempli de triangles par Blender (`triangle_fill`) ;
  2. on « enroule » ce patron sur la tête : chaque point (s, h) devient une direction vue
     depuis un point au centre de la tête (longitude s/R, latitude h/R), et on pose le
     point sur la VRAIE surface du cheval par un rayon (commun.sur_cheval_lisse). Le
     lissage « tente » fait passer le masque par-dessus le toupet, la racine du museau et
     la crinière au lieu de rentrer dans chaque creux : plus de peau qui ressort ;
  3. un Solidify lui donne 2 cm d'épaisseur ; sa tranche est jaune ;
  4. un GALON jaune de 4,4 cm (bible : au moins 4 cm) fait le tour de la plaque et le
     tour des deux trous. Il dépasse de 1,5 cm : c'est lui qui encadre les yeux noirs ;
  5. derrière la tête, sur la crinière, un petit nœud et deux bouts qui pendent.

Refonte v5 (bible §13.8 fiche 11) :
  - les coins extérieurs REMONTENT EN POINTE (forme dynamique, « vitesse ») ;
  - le galon jaune est en relief et VERNI (M5_GalonVerni, smoothness 0,80) contre le
    bandeau satiné (0,50) : contraste de matière et de valeur ;
  - une rainure au milieu du bandeau, au-dessus du museau (deux petits bourrelets) ;
  - deux pans noués derrière, plus longs (0,32), qui DÉPASSENT sur les côtés : on les voit
    de face. Ce sont des pièces séparées, animées par le code.

Réglage Unity : enfant de `PointVisage`, décalage (0 ; 0 ; 0), échelle 1.
Hiérarchie (le code Unity retrouve les pièces par leur nom) :
    masque_heros        vide, origine = PointVisage
    └── Masque          bandeau + galons + nœud ; origine = PointVisage
        ├── Anim_PanGauche  pan à gauche de l'image ; origine = le nœud derrière la tête
        ├── Anim_PanDroit   (ressort, et « flottent au vent » ±10° en 1,2 s, décalés 0,08 s)
        └── Eclat_1..3  vides : sur le galon (un éclat qui court sur le bord jaune, 1 / 4 s)
Matériaux : M_MasqueFramboise (bandeau), M5_GalonVerni (galon).
"""

import math
import os
import sys

import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402

NOM = "masque_heros"
POINT = "PointVisage"
MATS = ["M_MasqueFramboise", "M5_GalonVerni"]  # indices 0, 1

CENTRE = (0.0, C.CHEVAL_YEUX_Y, 0.03)   # point d'où l'on « enroule » (Unity), à hauteur des yeux
R = 0.30                 # rayon d'enroulement : s/R = longitude, h/R = latitude (radians)
S_PLAQUE = 0.33          # au-delà de |s| = 0,33, la plaque devient le lien
S_DOS = math.pi * R      # |s| du milieu de la nuque
LIEN = 0.045             # demi-hauteur du lien (9 cm)
ECART = 0.015            # le masque est posé 1,5 cm au-dessus de la peau
EPAISSEUR = 0.035          # V7 (retour 2) : tissu épais et plein
# V7 (point 17) : galon plus fin et plus fin et à flancs inclinés (3 cm × 2,4 cm), lissé : l'ancien
# (4,4 cm, section carrée) mangeait le bandeau et faisait « lunettes jaunes ».
GALON = [(-0.008, 0.0), (0.008, 0.0), (0.006, 0.012), (-0.006, 0.012)]   # V7 (retour 2) : liseré FIN (1,6 cm)
# Trous : centrés sur les yeux, assez grands pour que le galon (qui déborde de 2,2 cm vers
# l'intérieur) ne touche pas l'œil (17 × 21 cm, qui dépasse de 3 cm du front).
TROU_S, TROU_H = 0.098, 0.122   # V7 : trous un peu plus serrés autour des yeux
# La PLAQUE (devant) est posée par un « projecteur » placé devant et au-dessus du visage
# (Blender), comme un vidéoprojecteur qui projette le patron sur le visage. Pourquoi pas
# le même rayon depuis le centre de la tête que pour le lien ? Sous les yeux, ce rayon
# descend en biais et ressort par le DESSOUS du museau : le bas du masque partait alors
# en visière devant le nez. Le projecteur, lui, touche toujours la première surface qu'il
# voit : le front, puis le dessus du museau (le chanfrein), juste sous les yeux.
PROJECTEUR = Vector((0.0, 1.6, 2.1))
Y_ECRAN = 0.26           # plan vertical (Blender y) où l'on « dessine » la plaque
S_OEIL = R * math.asin(C.CHEVAL_YEUX_X / (1.05 * R))
ARBRE = None


def lisse(t):
    t = max(0.0, min(1.0, t))
    return t * t * (3 - 2 * t)


def centre_lien(u):
    """Le lien descend un peu en allant vers la nuque (passe sous les oreilles)."""
    return -0.10 * max(0.0, u - S_PLAQUE) / (S_DOS - S_PLAQUE)


def haut(s):
    """Bord du haut. Petit creux au milieu (sous le toupet), coins relevés et ARRONDIS
    au-dessus des yeux, puis il redescend en douceur vers le lien."""
    u = abs(s)
    if u <= S_PLAQUE:
        # V7 : bandeau plus HAUT (comme l'icône : une large bande framboise autour des yeux)
        monte = 0.21 + 0.06 * lisse(u / 0.14)                  # 0,21 au milieu -> 0,27 (bord du haut presque droit, comme l'icône)
        # v5 : la POINTE du coin extérieur (+4 cm vers u = 0,225, en douceur), puis on redescend
        pointe = 0.03 * 0.5 * (1 + math.cos(math.pi * min(1.0, abs(u - 0.225) / 0.09)))
        descend = lisse((u - 0.235) / (S_PLAQUE - 0.235))      # 0 -> 1 sur le côté
        return monte + pointe + (LIEN - monte) * descend
    return centre_lien(u) + LIEN


def bas(s):
    """Bord du bas. Encoche au milieu (sur le chanfrein), arrondi sous chaque œil."""
    u = abs(s)
    if u <= S_PLAQUE:
        creux = -0.10 - 0.17 * lisse(u / 0.15)                  # -0,10 -> -0,27 (V7)
        remonte = lisse((u - 0.21) / (S_PLAQUE - 0.21))
        return creux + (-LIEN - creux) * remonte
    return centre_lien(u) - LIEN


def abscisses():
    """Abscisses s du patron : serrées sur la plaque (2,5 cm), plus lâches sur le lien."""
    devant = [-S_PLAQUE + k * (2 * S_PLAQUE / 30) for k in range(30)]   # sans le bout droit (V7 : 30)
    lien = [S_PLAQUE + (S_DOS - S_PLAQUE) * k / 9 for k in range(10)]
    return [-x for x in reversed(lien[1:])] + devant + lien


def distance_projecteur(cible):
    """Distance du projecteur à la première surface du cheval vers `cible` (ou None)."""
    d = (cible - PROJECTEUR).normalized()
    hit, _, _, t = ARBRE.ray_cast(PROJECTEUR, d)
    return d, t


def vers_tete(s, h, ecart, pente=0.55, fenetre=0.06, pas=0.03):
    """Point (s, h) du patron -> (point posé sur la tête, direction vers l'extérieur).
    Plaque : par le projecteur ; lien : par un rayon depuis le centre de la tête.
    Les deux sont « lissés » (forme de tente) pour passer par-dessus les petits creux."""
    # Sur les côtés de la plaque, le projecteur voit la joue en rasant (il la rate même
    # parfois) : on passe en douceur au rayon depuis le centre entre |s| = 0,17 et 0,30.
    poids = lisse((abs(s) - 0.17) / 0.13)
    if poids >= 1.0:
        return C.sur_cheval_lisse(ARBRE, CENTRE, s / R, h / R, ecart, pente=pente,
                                  fenetre=(0.30, 0.20), pas=0.10)
    res = projeter(s, h, ecart, pente, fenetre, pas)
    if res is None:              # le projecteur passe au-dessus de la tête : rayon du centre
        poids = 1.0
        res = (Vector(), Vector((0, 1, 0)))
    p_proj, n_proj = res
    if poids <= 0.0:
        return p_proj, n_proj
    p_sph, n_sph = C.sur_cheval_lisse(ARBRE, CENTRE, s / R, h / R, ecart, pente=pente,
                                      fenetre=(0.30, 0.20), pas=0.10)
    return p_proj.lerp(p_sph, poids), n_proj.lerp(n_sph, poids).normalized()


def projeter(s, h, ecart, pente, fenetre, pas):
    """Pose (s, h) sur le visage par le projecteur (voir PROJECTEUR)."""
    x = 1.05 * R * math.sin(s / R)
    z = CENTRE[1] + h
    d, t = distance_projecteur(Vector((x, Y_ECRAN, z)))
    t = t if t is not None else math.inf
    n = int(round(fenetre / pas))
    for i in range(-n, n + 1):
        for j in range(-n, n + 1):
            if i or j:
                dj, tj = distance_projecteur(Vector((x + i * pas, Y_ECRAN, z + j * pas)))
                if tj is not None:
                    # le voisin plus PROCHE du projecteur « soulève » le point
                    t = min(t, tj + pente * pas * math.hypot(i, j))
    if math.isinf(t):
        return None
    return PROJECTEUR + d * (t - ecart * 1.2), -d


def plaque():
    bm = bmesh.new()
    xs = abscisses()
    contour = [(s, haut(s)) for s in xs] + [(s, bas(s)) for s in reversed(xs)]
    boucles = [contour]
    for cote in (-1, 1):
        boucles.append(C.contour_ellipse(TROU_S, TROU_H, n=20, cx=cote * S_OEIL, cz=0.0))
    aretes = []
    for boucle in boucles:
        vs = [bm.verts.new((s, h, 0)) for s, h in boucle]
        for i in range(len(vs)):
            aretes.append(bm.edges.new((vs[i], vs[(i + 1) % len(vs)])))
    bmesh.ops.triangle_fill(bm, use_beauty=True, use_dissolve=False, edges=aretes,
                            normal=(0, 0, 1))
    for f in list(bm.faces):      # sécurité : rien ne doit boucher les trous
        c = f.calc_center_median()
        for cote in (-1, 1):
            if ((c.x - cote * S_OEIL) / TROU_S) ** 2 + (c.y / TROU_H) ** 2 < 1:
                bm.faces.remove(f)
                break
    # Les grands triangles de la plaque sont recoupés en 4 (ceux du lien restent longs :
    # le lien est étroit, il suit déjà bien la tête).
    grandes = [e for e in bm.edges if e.calc_length() > 0.09 and
               all(abs(v.co.x) < S_PLAQUE + 0.01 for v in e.verts)]
    bmesh.ops.subdivide_edges(bm, edges=grandes, cuts=1)
    bmesh.ops.triangulate(bm, faces=bm.faces[:])
    for v in bm.verts:
        v.co = vers_tete(v.co.x, v.co.y, ECART)[0]
    # Les deux bouts du patron (s = ±0,94) tombent au même endroit derrière : on les soude.
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.004)
    centre = C.depuis_unity(*CENTRE)
    for f in bm.faces:
        f.material_index = 0
        if f.normal.dot(f.calc_center_median() - centre) < 0:
            f.normal_flip()
    o = C.objet_depuis_bmesh("Plaque", bm, MATS)
    epais = o.modifiers.new("Epaisseur", "SOLIDIFY")
    epais.thickness = EPAISSEUR
    epais.offset = 1.0                 # vers l'extérieur (sens des normales)
    epais.use_rim = True
    epais.material_offset_rim = 0      # V7 : tranche en tissu (plus de bande jaune épaisse)
    epais.use_even_offset = False     # (True fait des pics sur les triangles très fins)
    epais.use_quality_normals = True
    C.appliquer(o)
    return o


def galon(points_sh, bm):
    chemin, hauts = [], []
    for s, h in points_sh:
        p, d = vers_tete(s, h, ECART + EPAISSEUR - 0.008)   # V7 : posé sur le bord du tissu
        chemin.append(p)
        hauts.append(d)
    C.bm_balayage_surface(bm, chemin, hauts, GALON, mat=1, ferme=True)


def galons():
    bm = bmesh.new()
    # Tour de la plaque : le haut, la « couture » qui coupe le lien, le bas, l'autre couture.
    # Mêmes abscisses que le bord du patron : le galon recouvre exactement le bord.
    xs = [-S_PLAQUE + k * (2 * S_PLAQUE / 30) for k in range(31)]
    tour = [(s, haut(s)) for s in xs]
    tour += [(S_PLAQUE, LIEN * 0.33), (S_PLAQUE, -LIEN * 0.33)]
    tour += [(s, bas(s)) for s in reversed(xs)]
    tour += [(-S_PLAQUE, -LIEN * 0.33), (-S_PLAQUE, LIEN * 0.33)]
    galon(tour, bm)
    for cote in (-1, 1):
        galon(C.contour_ellipse(TROU_S, TROU_H, n=20, cx=cote * S_OEIL), bm)
    o = C.objet_depuis_bmesh("Galons", bm, MATS)
    return o


def noeud():
    """Petit nœud sur la crinière, derrière la tête. Renvoie (objet, centre, direction)."""
    bm = bmesh.new()
    h0 = centre_lien(S_DOS)
    p, d = vers_tete(S_DOS, h0, ECART + EPAISSEUR + 0.025)
    rot = d.to_track_quat("Y", "Z").to_matrix().to_4x4()
    v = C.bm_sphere(bm, 1.0, (0, 0, 0), segments=8, anneaux=5, echelle=(0.075, 0.05, 0.065))
    bmesh.ops.transform(bm, matrix=Matrix.Translation(p) @ rot, verts=v)
    return C.objet_depuis_bmesh("Noeud", bm, MATS), p, d


def pan(nom, cote, p, d):
    """Pan de 0,32 qui part du nœud vers le côté, un peu vers le bas et l'arrière, en
    ondulant : il dépasse de la silhouette de la tête, on le voit de face."""
    bm = bmesh.new()
    lat = d.cross(Vector((0, 0, 1))).normalized() * cote
    section = [(-0.035, -0.011), (0.035, -0.011), (0.035, 0.011), (-0.035, 0.011)]
    chemin, hauts = [], []
    for k in range(7):
        t = k / 6
        q = (p + lat * (0.04 + 0.30 * t) + d * (0.10 * t) + Vector((0, 0, -0.10 * t))
             + Vector((0, 0, 0.025 * math.sin(t * math.pi * 2))))
        chemin.append(q)
        hauts.append(Vector((0, 0, 1)).lerp(d, 0.3).normalized())
    anneaux = C.bm_balayage_surface(bm, chemin, hauts, section, mat=0)
    # bout taillé en V (galon jaune sur la dernière tranche)
    for f in {f for vv in anneaux[-1] for f in vv.link_faces}:
        f.material_index = 1
    return C.piece(nom, bm, MATS, lissage="angle", pivot=p)


def rainure(bm):
    """Deux petits bourrelets verticaux au milieu, au-dessus du museau : ils dessinent une
    rainure (la couture du bandeau)."""
    for s_ in (-0.016, 0.016):
        chemin, hauts = [], []
        for k in range(5):
            h = bas(0.0) + 0.03 + (haut(0.0) - bas(0.0) - 0.06) * k / 4
            q, n = vers_tete(s_, h, ECART + EPAISSEUR)
            chemin.append(q)
            hauts.append(n)
        C.bm_balayage_surface(bm, chemin, hauts, [(-0.008, 0.0), (0.008, 0.0), (0.0, 0.012)],
                              mat=0)


def construire():
    global ARBRE
    C.nouvelle_scene()
    ARBRE = C.arbre_cheval()
    nd, p_noeud, d_noeud = noeud()
    bm = bmesh.new()
    rainure(bm)
    rain = C.objet_depuis_bmesh("Rainure", bm, MATS)
    obj = C.joindre([plaque(), galons(), nd, rain], "Masque")
    C.lisser_angle(obj, 75)   # V7 : tissu lisse, tranche nette
    C.deplacer_origine(obj, C.U(*C.CHEVAL_ACCROCHES[POINT]))
    for nom, cote in (("Anim_PanGauche", -1), ("Anim_PanDroit", 1)):
        C.parenter(pan(nom, cote, p_noeud, d_noeud), obj)
    eclats = [vers_tete(s_, haut(s_), ECART + 0.05)[0] for s_ in (-0.215, 0.10, 0.215)]
    for e in C.reperes_effets(eclats):
        C.parenter(e, obj)
    return C.racine_v5(NOM, POINT, [obj])


if __name__ == "__main__":
    C.finir_v5(construire(), NOM, POINT)
