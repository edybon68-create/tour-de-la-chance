"""
aureole.py — Auréole lumineuse (boîte d'accessoires n°1 : `aureole`, Légendaire, Tête).
VERSION v5 « plus jeu moderne » (docs/art-bible.md §13.8 fiche 15), faite pour le cheval.

Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/aureole.py
Sortie : blender/exports/aureole.fbx et docs/captures/v5_cheval_aureole.png.

Les trois niveaux de forme (bible §13.2) :
  - primaire   : l'anneau Ø 0,70, section épaisse (10 cm), en DEUX matières : un cœur en doré
                 déco (le dessous et l'extérieur, métal) et une bande lumineuse #FFF1A8
                 (le dessus et l'intérieur, émission) : « objet précieux » au lieu de « néon » ;
  - secondaire : les 8 perles Ø 8 cm posées sur le cœur doré (la guirlande v3) ;
  - tertiaire  : 4 petits rayons (losanges plats de 10 cm) qui pointent vers l'extérieur,
                 entre les perles.

Hiérarchie (le code Unity retrouve les pièces par leur nom ; noms v3 gardés pour
AccessoryMotion : il cherche « Anneau » et « Perle_0 » à « Perle_7 », et le matériau
« M_Aureole » dont il allume l'émission) :
    aureole              vide, origine = PointChapeau (attachScale 1, attachOffset 0)
    └── Anneau           l'anneau ; origine = son centre, position locale (0 ; 0,30 ; 0)
        │                (flotte 22 cm au-dessus du sommet de la tête ; le code le fait
        │                flotter ±4 cm, tourner à 30°/s et suivre la tête avec retard)
        ├── Perle_0 … Perle_7   les perles (matériau M_Aureole : chenillard du code)
        ├── Anim_Rayons  les 4 rayons ; origine = centre de l'anneau. Ils doivent tourner en
        │                sens inverse de l'anneau (−20°/s dans le monde, soit −50°/s en
        │                rotation locale puisque l'anneau tourne déjà à +30°/s)
        ├── Eclat_1..3   vides : points d'éclat sur l'anneau (1 toutes les 1,5 s)
        └── Fx_Emetteur  vide : centre de l'anneau, d'où tombe la pluie de lumière (≤ 8)
"""

import math
import os
import sys

import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402

NOM = "aureole"
POINT = "PointChapeau"
MATS = ["M5_DoreDeco", "M_Aureole"]  # 0, 1
HAUTEUR_FLOTTE = 0.22      # V7 : 0,30 -> 0,22 (le QA : « flotte haut au-dessus de la tête »)
R_TUBE = 0.05             # section de 10 cm
R_ANNEAU = 0.35 - R_TUBE  # Ø extérieur 0,70


def construire():
    C.nouvelle_scene()
    centre = C.U(*C.CHEVAL_ACCROCHES[POINT]) + Vector((0, 0, HAUTEUR_FLOTTE))
    bm = bmesh.new()
    seg, tube = 40, 12
    C.bm_tore(bm, R_ANNEAU, R_TUBE, segments=seg, segments_tube=tube, mat=0)
    # Matières selon la position dans la section : le dessus et l'intérieur brillent
    # (angle du tube entre ~70° et ~250°, où 90° = le dessus et 180° = l'intérieur).
    for f in bm.faces:
        c = f.calc_center_median()
        r = math.hypot(c.x, c.y) - R_ANNEAU
        b = math.degrees(math.atan2(c.z, r)) % 360
        f.material_index = 1 if 60 <= b <= 250 else 0
    bmesh.ops.translate(bm, vec=centre, verts=bm.verts)
    anneau = C.piece("Anneau", bm, MATS, lissage="tout", pivot=centre)

    perles = []
    for i in range(8):
        a = 2 * math.pi * i / 8
        p = centre + Vector((math.cos(a) * (R_ANNEAU + 0.035), math.sin(a) * (R_ANNEAU + 0.035),
                             R_TUBE * 0.55))
        bm = bmesh.new()
        C.bm_sphere(bm, 0.04, p, segments=8, anneaux=5, mat=1)
        perles.append(C.piece(f"Perle_{i}", bm, MATS, lissage="tout", pivot=p))
    bm = bmesh.new()
    for i in range(4):
        a = 2 * math.pi * (i + 0.5) / 4 + math.pi / 8
        d = Vector((math.cos(a), math.sin(a), 0))
        p = centre + d * (R_ANNEAU + R_TUBE + 0.06)
        los = [(0.0, -0.025), (0.06, 0.0), (0.0, 0.025), (-0.06, 0.0)]
        # losange couché à plat, la pointe vers l'extérieur
        m = Matrix.Translation(p) @ Matrix((d, Vector((0, 0, 1)), d.cross(Vector((0, 0, 1))) * -1)
                                           ).transposed().to_4x4()
        C.bm_coussin(bm, los, 0.02, 0.04, m, mat=1, anneaux=1)
    rayons = C.piece("Anim_Rayons", bm, MATS, lissage="angle", pivot=centre)
    eclats = [centre + Vector((math.cos(a) * 0.34, math.sin(a) * 0.34, 0.05))
              for a in (0.5, 2.6, 4.4)]
    for o in perles + [rayons] + C.reperes_effets(eclats, fx=centre):
        C.parenter(o, anneau)
    return C.racine_v5(NOM, POINT, [anneau])


if __name__ == "__main__":
    C.finir_v5(construire(), NOM, POINT)
