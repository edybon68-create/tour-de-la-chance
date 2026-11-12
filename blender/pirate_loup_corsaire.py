"""
pirate_loup_corsaire.py — Loup de corsaire (boîte d'accessoires Pirates : `loup_corsaire`,
LÉGENDAIRE, Visage). Fiche : docs/specs/v5-catalogue.md §2 n° 08 ; règles : art-bible §13.

Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/pirate_loup_corsaire.py
Sortie : blender/exports/loup_corsaire.fbx et docs/captures/v5_cheval_loup_corsaire.png.

MÊME TECHNIQUE que le Masque corrigé (blender/masque_heros.py, v5 §4.3) : on RÉUTILISE ses
fonctions (patron dessiné à plat, enroulé sur la vraie tête par rayons, galon posé sur la
surface) en lui donnant d'autres réglages ; masque_heros.py n'est pas modifié.
Ce qui change pour en faire un « loup » de bal masqué :
  - les coins extérieurs remontent en GRANDES POINTES (+9 cm, au lieu de +4,5) qui
    s'évasent vers l'extérieur, et le bas fait une pointe sous chaque œil ;
  - le lien qui fait le tour de la tête est fin (7 cm) : c'est un ruban, pas un bandeau ;
  - velours violet très mat (smoothness 0,10) contre une bordure d'OR qui luit (0,6) :
    le contraste de matière le plus fort possible ;
  - deux pierres rouges taillées aux pointes, deux longs rubans (0,45) qui flottent.
Les deux trous laissent les yeux du cheval entièrement visibles (galon compris).

Les trois niveaux de forme (bible §13.2) :
  - primaire   : la plaque de velours en ailes de papillon autour des yeux ;
  - secondaire : la bordure d'or en relief (4,4 cm) et les deux longs rubans ;
  - tertiaire  : 2 pierres rouges (Ø 7 cm) aux pointes, le nœud derrière la tête.
Matières (3) : velours violet (0,10), or luisant (M5_PirOrLuit, metallic 1, 0,80,
émission 0,6 : le code fait défiler des reflets), pierre rouge (0,95).

Hiérarchie (le code Unity retrouve les pièces par leur nom) :
    loup_corsaire       vide, origine = PointVisage (attachScale 1, attachOffset 0)
    └── Loup            plaque + bordure + pierres + nœud ; origine = PointVisage
        ├── Anim_RubanGauche  ruban à gauche de l'image ; origine = le nœud (ressort A, et
        ├── Anim_RubanDroit   flottent ±12° en 1,3 s, décalés de 0,08 s : bible §13.5 B)
        └── Eclat_1..3  vides : sur la bordure et une pierre (1 éclat / 1,5 s, émission 1,2)
"""

import math
import os
import sys

import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402
import commun_pirates as P  # noqa: E402
import masque_heros as M  # noqa: E402  (on réutilise ses fonctions, sans le modifier)

NOM = "loup_corsaire"
POINT = "PointVisage"
MATS = ["M5_PirVelours", "M5_PirOrLuit", "M5_PirRubis"]   # 0, 1, 2

# --- nos réglages, donnés au module du masque -----------------------------------------
M.MATS = MATS
M.LIEN = 0.035              # lien fin (7 cm) : un ruban autour de la tête
M.S_PLAQUE = 0.37           # plaque plus large : la descente vers le lien est plus douce


def haut_loup(s):
    """Bord du haut : creux au milieu, GRANDE pointe relevée au coin extérieur."""
    u = abs(s)
    if u <= M.S_PLAQUE:
        monte = 0.10 + 0.135 * M.lisse(u / 0.13)                  # 0,10 -> 0,235
        pointe = 0.085 * 0.5 * (1 + math.cos(math.pi * min(1.0, abs(u - 0.25) / 0.10)))
        descend = M.lisse((u - 0.27) / (M.S_PLAQUE - 0.27))
        return monte + pointe + (M.LIEN - monte) * descend
    return M.centre_lien(u) + M.LIEN


def bas_loup(s):
    """Bord du bas : encoche sur le chanfrein, petite pointe sous chaque œil."""
    u = abs(s)
    if u <= M.S_PLAQUE:
        creux = -0.07 - 0.145 * M.lisse(u / 0.14)                 # -0,07 -> -0,215
        pointe = -0.035 * 0.5 * (1 + math.cos(math.pi * min(1.0, abs(u - 0.17) / 0.07)))
        remonte = M.lisse((u - 0.25) / (M.S_PLAQUE - 0.25))
        return creux + pointe + (-M.LIEN - creux) * remonte
    return M.centre_lien(u) - M.LIEN


M.haut = haut_loup
M.bas = bas_loup


def ruban(nom, cote, p, d):
    """Long ruban (0,45) qui part du nœud vers le côté et ondule ; bout doré."""
    bm = bmesh.new()
    lat = d.cross(Vector((0, 0, 1))).normalized() * cote
    section = [(-0.04, -0.011), (0.04, -0.011), (0.04, 0.011), (-0.04, 0.011)]
    chemin, hauts = [], []
    for k in range(8):
        t = k / 7
        q = (p + lat * (0.04 + 0.36 * t) + d * (0.16 * t) + Vector((0, 0, -0.16 * t))
             + Vector((0, 0, 0.035 * math.sin(t * math.pi * 2.2))))
        chemin.append(q)
        hauts.append(Vector((0, 0, 1)).lerp(d, 0.3).normalized())
    an = C.bm_balayage_surface(bm, chemin, hauts, section, mat=0)
    for f in {f for vv in an[-1] + an[-2] for f in vv.link_faces}:
        f.material_index = 1
    for v in an[-1]:                      # bout taillé en pointe
        v.co = chemin[-1] + (v.co - chemin[-1]) * 0.3
    return C.piece(nom, bm, MATS, lissage="angle", pivot=p)


def construire():
    C.nouvelle_scene()
    M.ARBRE = P.arbre()
    nd, p_noeud, d_noeud = M.noeud()
    bm = bmesh.new()
    eclats = []
    for cote in (-1, 1):                  # pierres rouges aux pointes
        s = cote * 0.25
        p, n = M.vers_tete(s, haut_loup(s) - 0.06, M.ECART + M.EPAISSEUR + 0.015)
        m = C.repere(p, n)
        C.bm_coussin(bm, C.contour_ellipse(0.035, 0.045, n=8), 0.02, 0.05, m, mat=2,
                     anneaux=1)
        eclats.append(p + n * 0.03)
    pierres = C.objet_depuis_bmesh("Pierres", bm, MATS)
    obj = C.joindre([M.plaque(), M.galons(), nd, pierres], "Loup")
    C.lisser_angle(obj, 45)
    C.deplacer_origine(obj, C.U(*C.CHEVAL_ACCROCHES[POINT]))
    for nom, cote in (("Anim_RubanGauche", -1), ("Anim_RubanDroit", 1)):
        C.parenter(ruban(nom, cote, p_noeud, d_noeud), obj)
    eclats.append(M.vers_tete(0.0, haut_loup(0.0), M.ECART + 0.05)[0])
    for e in C.reperes_effets(eclats):
        C.parenter(e, obj)
    return C.racine_v5(NOM, POINT, [obj])


if __name__ == "__main__":
    P.finir_pirate(construire(), NOM, POINT)
