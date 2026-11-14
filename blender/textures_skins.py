"""
textures_skins.py — V7 : les vraies textures des skins à motif (point 10 de v7-a-faire.md).

Pourquoi ? Aujourd'hui, `SkinPatterns.cs` CALCULE chaque motif au démarrage, en 256 × 256, avec
des bords « tout ou rien » (un pixel est noir ou blanc) : de près, les rayures du zèbre sont en
escalier, les rosettes du léopard sont des anneaux de compas, le réseau de la girafe est raide.
Ce script fabrique, une fois pour toutes, des images 1024 × 1024 soignées :
  - bords ADOUCIS (chaque pixel est calculé 4 fois puis moyenné : anticrénelage) ;
  - formes naturelles (bruits doux, cellules irrégulières, rayures qui s'affinent) ;
  - SANS RACCORD autour du corps : l'image fait exactement un tour (U de 0 à 1) et ses bords
    gauche et droit se rejoignent (tous les bruits « bouclent » en U).

Correspondance avec le cheval (la même que SkinPatterns.cs) : U fait le tour du corps (0,5 =
devant, couture derrière), V monte du socle (0) aux oreilles (1). Le tour mesure 3,1 m et la
hauteur 2 m : on dessine en MÈTRES (x de 0 à 3,1, y de 0 à 2) pour que les taches restent
rondes sur le corps, même si l'image est carrée.
Les couleurs sont celles des fiches (Assets/Shop/Items/v5/*.asset) : elles sont « cuites »
dans l'image, la couleur de base du matériau reste blanche.

Lancer (Blender n'a pas besoin d'être ouvert) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/textures_skins.py
Seulement quelques skins :  SKINS="animal_zebre pirate_or_maudit" ...
Version « avant » (reproduction du motif calculé actuel, 256 px, pour les planches) : AVANT=1
Sorties : art/skins_textures/<id>.png (couleurs) et <id>_lumiere.png (émission, si le skin brille).
"""

import math
import os

import bpy
import numpy as np

RACINE = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SORTIE = os.environ.get("SORTIE") or os.path.join(RACINE, "art", "skins_textures")
TAILLE = 1024
SUR = 2                       # sur-échantillonnage (anticrénelage) : calcul en 2048, moyenne 2 × 2
TOUR, HAUT = 3.1, 2.0         # mètres


def hexa(h):
    h = h.lstrip("#")
    return np.array([int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)], dtype=np.float32)


def melange(a, b, t):
    t = np.clip(t, 0, 1)[..., None]
    return a * (1 - t) + b * t


def doux(a, b, x):
    """Passage doux de 0 à 1 quand x va de a à b (« smoothstep »)."""
    t = np.clip((x - a) / (b - a), 0, 1)
    return t * t * (3 - 2 * t)


# ---------------------------------------------------------------------------------------
# Bruits qui bouclent en U
# ---------------------------------------------------------------------------------------
class Bruits:
    def __init__(self, n, graine):
        self.n = n
        self.rng = np.random.default_rng(graine)
        i = (np.arange(n) + 0.5) / n
        self.x, self.y = np.meshgrid(i * TOUR, i * HAUT)     # ligne 0 = bas (v = 0)

    def valeur(self, cu, cv, x=None, y=None):
        """Bruit de valeur lissé : `cu` cellules sur le tour complet (entier : il boucle),
        `cv` cellules par mètre en hauteur. Résultat entre 0 et 1."""
        x = self.x if x is None else x
        y = self.y if y is None else y
        cu = max(1, int(round(cu)))
        ny = int(math.ceil(HAUT * cv + 4)) + 2
        g = self.rng.random((ny, cu)).astype(np.float32)
        X = (x / TOUR) * cu
        Y = y * cv + 1.0
        i0 = np.floor(X).astype(int)
        j0 = np.clip(np.floor(Y).astype(int), 0, ny - 2)
        fx, fy = X - np.floor(X), Y - np.floor(Y)
        fx = fx * fx * fx * (fx * (fx * 6 - 15) + 10)
        fy = fy * fy * fy * (fy * (fy * 6 - 15) + 10)
        a, b = g[j0, i0 % cu], g[j0, (i0 + 1) % cu]
        c, d = g[j0 + 1, i0 % cu], g[j0 + 1, (i0 + 1) % cu]
        return (a * (1 - fx) + b * fx) * (1 - fy) + (c * (1 - fx) + d * fx) * fy

    def fbm(self, cu, cv, octaves=4, x=None, y=None):
        s, amp, tot = 0.0, 1.0, 0.0
        for k in range(octaves):
            s = s + amp * self.valeur(cu * 2 ** k, cv * 2 ** k, x, y)
            tot += amp
            amp *= 0.5
        return s / tot

    def cellules(self, cu, cv, rayon=None, presence=1.0):
        """Cellules de Worley (points semés, un par case, qui bouclent en U).
        Renvoie (F1, F2, valeur au hasard de la cellule la plus proche, dx, dy vers elle)
        et, si `rayon` = (min, max), le masque « dans un disque de rayon propre »."""
        cu = int(round(cu))
        nv = int(math.ceil(HAUT * cv)) + 2
        px = self.rng.random((nv, cu)).astype(np.float32)
        py = self.rng.random((nv, cu)).astype(np.float32)
        val = self.rng.random((nv, cu)).astype(np.float32)
        ray = self.rng.random((nv, cu)).astype(np.float32)
        ici = self.rng.random((nv, cu)) < presence
        lu, lv = TOUR / cu, 1.0 / cv
        I = np.floor(self.x / lu).astype(int)
        J = np.floor(self.y / lv).astype(int)
        F1 = np.full(self.x.shape, 9.0, np.float32)
        F2 = np.full(self.x.shape, 9.0, np.float32)
        V = np.zeros(self.x.shape, np.float32)
        DX = np.zeros(self.x.shape, np.float32)
        DY = np.zeros(self.x.shape, np.float32)
        R = np.zeros(self.x.shape, np.float32)
        for dj in (-1, 0, 1):
            for di in (-1, 0, 1):
                ii, jj = (I + di) % cu, np.clip(J + dj, 0, nv - 1)
                cx = (I + di + px[jj, ii]) * lu
                cy = (J + dj + py[jj, ii]) * lv
                dx = self.x - cx
                dx = dx - np.round(dx / TOUR) * TOUR
                dy = self.y - cy
                d = np.sqrt(dx * dx + dy * dy)
                d = np.where(ici[jj, ii], d, 9.0)
                plus_pres = d < F1
                F2 = np.where(plus_pres, F1, np.minimum(F2, d))
                V = np.where(plus_pres, val[jj, ii], V)
                DX = np.where(plus_pres, dx, DX)
                DY = np.where(plus_pres, dy, DY)
                if rayon:
                    R = np.where(plus_pres, rayon[0] + (rayon[1] - rayon[0]) * ray[jj, ii], R)
                F1 = np.minimum(F1, d)
        return F1, F2, V, DX, DY, R


# ---------------------------------------------------------------------------------------
# Les motifs (une fonction par skin) : renvoient (couleurs, lumière ou None)
# ---------------------------------------------------------------------------------------
def rayures(B, n, clair, fonce, seuil, variation, flou, casse=0.35, fond_bruit=0.04):
    y, x = B.y, B.x
    s = (y / HAUT) * n + 0.55 * (B.fbm(4, 1.6) - 0.5) * 2 + 0.12 * np.sin(2 * np.pi * x / TOUR * 3)
    bande = 0.5 + 0.5 * np.cos(2 * np.pi * s)
    # le seuil varie : les rayures s'affinent, se cassent et se terminent en pointe
    t = seuil + variation * (B.fbm(9, 3.5) - 0.5) * 2 + casse * (B.valeur(5, 2.2) - 0.5)
    m = doux(t - flou, t + flou, bande)
    base = melange(clair * (1 - fond_bruit), clair, B.fbm(30, 20, 3))
    return melange(base, fonce, m), None


def zebre(B):
    return rayures(B, 16, hexa("#F4EEE6"), hexa("#1F1730"), 0.52, 0.16, 0.03)


def tigre(B):
    c, _ = rayures(B, 12, hexa("#F28C38"), hexa("#1F1730"), 0.66, 0.16, 0.03, casse=0.45)
    # V7 (point 17) : POITRAIL BLANC devant (comme l'icône), qui monte jusqu'au museau.
    # Les rayures s'arrêtent net sur le blanc, avec un bord un peu irrégulier.
    # (retour programmeur) : plastron en GOUTTE, large en haut sous le menton et qui
    # s'affine vers le bas ; bords doux et irréguliers (plus de bande droite du socle au cou)
    largeur = 0.08 + 0.18 * doux(0.55, 1.30, B.y)
    bord = largeur * (1 + 0.25 * (B.fbm(10, 6, 3) - 0.5) * 2)
    devant = doux(bord + 0.04, bord - 0.04, np.abs(B.x - TOUR / 2)) * doux(1.72, 1.50, B.y) * doux(0.5, 0.75, B.y)
    museau = doux(0.22, 0.16, np.hypot(B.x - TOUR / 2, (B.y - 1.48) * 1.4))
    blanc = np.maximum(devant, museau)
    return melange(c, hexa("#F7F1E8"), blanc), None


def chat_roux(B):
    c, _ = rayures(B, 10, hexa("#E8873A"), hexa("#B8602A"), 0.55, 0.12, 0.12, casse=0.25,
                   fond_bruit=0.08)
    return c, None


def dalmatien(B):
    F1, F2, V, dx, dy, R = B.cellules(30, 10, rayon=(0.012, 0.040), presence=0.5)
    bord = R * (1 + 0.22 * (B.fbm(60, 40, 2) - 0.5) * 2)
    m = doux(bord + 0.003, bord - 0.003, F1)
    base = melange(hexa("#E8E2DA"), hexa("#F4EEE6"), B.fbm(25, 18, 3))
    return melange(base, hexa("#1F1730"), m), None


def vache(B):
    n = B.fbm(4, 1.4, 5) + 0.10 * (B.fbm(20, 14, 3) - 0.5)
    m = doux(0.565, 0.575, n)
    base = melange(hexa("#E8E2DA"), hexa("#F4EEE6"), B.fbm(25, 18, 3))
    tache = melange(hexa("#5A3C2A"), hexa("#6B4A35"), B.fbm(18, 12, 3))
    return melange(base, tache, m), None


def leopard(B):
    F1, F2, V, dx, dy, R = B.cellules(17, 5.5, rayon=(0.075, 0.105), presence=0.9)
    a = np.arctan2(dy, dx)
    k = 3 + np.floor(V * 3)
    trou = np.sin(a * k + V * 20) > 0.55 - 0.3 * V      # 3 à 5 morceaux par rosette
    bruit = 0.12 * (B.fbm(50, 32, 2) - 0.5) * 2
    rr = F1 / np.maximum(R, 1e-3) + bruit
    anneau = doux(0.48, 0.55, rr) * doux(1.0, 0.92, rr) * (~trou)
    centre = doux(0.62, 0.52, rr)
    base = melange(hexa("#D8A660"), hexa("#E8BC7A"), B.fbm(12, 8, 4))
    c = melange(base, hexa("#B07A3A"), 0.85 * centre)
    # petites taches pleines entre les rosettes
    P1, _, _, _, _, PR = B.cellules(30, 10, rayon=(0.012, 0.020), presence=0.25)
    pois = doux(PR + 0.003, PR - 0.003, P1) * (F1 > R * 1.15)
    return melange(c, hexa("#4A2E1A"), np.maximum(anneau, pois)), None


def girafe(B):
    w = 0.25 * (B.fbm(14, 10, 3) - 0.5)
    F1, F2, V, _, _, _ = B.cellules(26, 8.5)
    ligne = F2 - F1 + 0.02 * w
    m = doux(0.020, 0.030, ligne)               # 0 sur la ligne claire, 1 dans la tache
    tache = melange(hexa("#7A4A28"), hexa("#9A6638"), V * 0.7 + 0.3 * B.fbm(30, 20, 2))
    base = melange(hexa("#DCCB98"), hexa("#E8D8A8"), B.fbm(25, 18, 3))
    return melange(base, tache, m), None


def bois_flotte(B):
    g = B.fbm(55, 1.0, 4) + 0.25 * B.fbm(8, 3, 3)
    c = melange(hexa("#B9AE9C"), hexa("#8C8170"), doux(0.55, 0.78, g))
    fibre = 0.5 + 0.5 * np.sin(2 * np.pi * (B.x * 22 + 3 * B.fbm(10, 2, 3)))
    c = melange(c, hexa("#7A705F"), 0.35 * doux(0.86, 0.96, fibre))
    c = melange(c, hexa("#D2C8B6"), 0.25 * doux(0.6, 0.85, B.fbm(6, 4, 3)))
    # 10 petits coquillages roses (spirale) posés sur le bois
    rng = np.random.default_rng(9)
    for _ in range(10):
        sx, sy, r = rng.random() * TOUR, 0.2 + rng.random() * 1.5, 0.040 + rng.random() * 0.015
        dx = B.x - sx
        dx = dx - np.round(dx / TOUR) * TOUR
        dy = B.y - sy
        d = np.sqrt(dx * dx + dy * dy)
        a = np.arctan2(dy, dx)
        spire = 0.5 + 0.5 * np.cos(2 * np.pi * (d / r * 2.2) - a)
        coq = doux(r + 0.002, r - 0.002, d)
        teinte = melange(hexa("#F2B8A8"), hexa("#D88A7A"), 0.6 * doux(0.75, 0.95, spire))
        c = melange(c, teinte, coq)
    return c, None


def corail(B):
    base = melange(hexa("#F06A5C"), hexa("#FF8A7A"), B.fbm(10, 7, 4))
    F1, F2, V, _, _, R = B.cellules(60, 19, rayon=(0.014, 0.022), presence=0.8)
    trou = doux(R + 0.002, R - 0.002, F1)
    creux = doux(R * 0.6, R * 0.35, F1)
    c = melange(base, hexa("#FFC2B0"), trou)
    c = melange(c, hexa("#E05A50"), creux * 0.8)
    return c, None


def vert_de_gris(B):
    cuivre = melange(hexa("#9A5A2C"), hexa("#C88048"), B.fbm(16, 12, 4))
    cuivre = melange(cuivre, hexa("#E0A070"), 0.4 * doux(0.62, 0.8, B.fbm(40, 30, 2)))
    n = B.fbm(7, 5, 5) + 0.1 * (B.fbm(40, 30, 2) - 0.5)
    m = doux(0.52, 0.56, n)
    patine = melange(hexa("#4FA890"), hexa("#7FD0B8"), B.fbm(30, 22, 3))
    c = melange(cuivre, patine, m)
    bord = doux(0.50, 0.53, n) * doux(0.58, 0.54, n)
    return melange(c, hexa("#3E7E6A"), 0.5 * bord), None


def carte(B):
    n = B.fbm(8, 6, 5)
    c = melange(hexa("#E9D3A0"), hexa("#F4E2B8"), doux(0.4, 0.7, n))
    c = melange(c, hexa("#C8A870"), 0.45 * doux(0.62, 0.8, B.fbm(5, 4, 4)))   # taches de thé
    c = melange(c, hexa("#D8BE88"), 0.3 * doux(0.5, 0.9, B.fbm(80, 3, 2)))    # fibres
    # chemin en pointillés qui fait le tour (sinusoïde), traits arrondis
    yc = (0.45 + 0.18 * np.sin(2 * np.pi * B.x / TOUR * 2 + 1)) * HAUT
    d = np.abs(B.y - yc)
    tiret = (np.mod(B.x * 9.5, 1.0) < 0.55)
    c = melange(c, hexa("#C0392B"), doux(0.016, 0.010, d) * tiret)
    for ux, vy in ((0.62, 0.3), (0.2, 0.65), (0.9, 0.55)):
        du = B.x - ux * TOUR
        du = du - np.round(du / TOUR) * TOUR
        dv = B.y - vy * HAUT
        dans = (np.abs(du) < 0.075) & (np.abs(dv) < 0.075)
        trait = np.minimum(np.abs(du - dv), np.abs(du + dv)) / math.sqrt(2)
        c = melange(c, hexa("#C0392B"), doux(0.016, 0.009, trait) * dans)
    return c, None


def tempete(B):
    q = B.fbm(4, 3, 4)
    r = B.fbm(5, 3.5, 4)
    n = B.fbm(4, 3, 5, x=B.x + 0.0, y=B.y + 0.6 * q + 0.3 * r)
    s = np.abs(np.sin((B.y / HAUT * 5 + n * 3.2) * np.pi))
    c = melange(hexa("#3E4660"), hexa("#7A86A8"), doux(0.25, 0.95, s))
    c = melange(c, hexa("#CFE0FF"), 0.5 * doux(0.93, 0.995, s))
    return c, None


def or_maudit(B):
    g = B.fbm(10, 8, 4)
    c = melange(hexa("#D89A20"), hexa("#FFD25A"), g)
    c = melange(c, hexa("#FFF0B0"), 0.35 * doux(0.68, 0.85, B.fbm(30, 22, 2)))
    v = np.zeros_like(g)
    n = B.fbm(5, 3.5, 3)
    crete = 1 - np.abs(2 * n - 1)
    v = doux(0.972, 0.99, crete)            # fines veines (une seule famille, pas de taches)
    vert = hexa("#3CFF9A")
    c = melange(c, melange(c, vert, np.ones_like(v)), 0.75 * v)
    lueur = vert * (v[..., None] * 1.0)
    return c, lueur


def ecailles_motif(B, bas, haut, liseret, rangs, cols, plume=False, oeil=None):
    """Écailles (ou plumes) qui se chevauchent comme des tuiles : chaque écaille est un disque
    (ellipse pour une plume) accroché par son HAUT ; son bord arrondi du bas recouvre le haut
    de l'écaille du dessous (les rangées du haut sont posées par-dessus)."""
    v = B.y / HAUT
    Y, X = v * rangs, (B.x / TOUR) * cols
    j = np.floor(Y)
    rx, ry = (0.62, 1.25) if plume else (0.68, 0.80)
    choisi = np.zeros(Y.shape, bool)
    dyn = np.zeros(Y.shape, np.float32)
    dxn = np.zeros(Y.shape, np.float32)
    rn = np.ones(Y.shape, np.float32) * 2
    for dj in (1, 0, -1):                       # du haut (posé dessus) vers le bas
        jj = j + dj
        dec = 0.5 * np.mod(jj, 2)
        ii = np.round(X - dec)
        for di in (-1, 0, 1):
            cx = ii + di + dec
            dx = X - cx
            dx = dx - np.round(dx / cols) * cols
            dy = (jj + 1) - Y                   # 0 en haut de l'écaille, ry au bas
            r = np.sqrt((dx / rx) ** 2 + (dy / ry) ** 2)
            ok = (~choisi) & (r < 1) & (dy > -0.05)
            dyn = np.where(ok, dy / ry, dyn)
            dxn = np.where(ok, dx / rx, dxn)
            rn = np.where(ok, r, rn)
            choisi |= ok
    base = melange(bas, haut, v)
    # ombre sous l'écaille du dessus (en haut), plus clair vers le bord libre (en bas)
    c = melange(base * 0.72, base * 1.06, doux(0.0, 0.75, dyn))
    rim = doux(0.80, 0.90, rn) * doux(1.0, 0.94, rn)
    c = melange(c, liseret, 0.65 * rim)
    c = melange(c, base * 0.55, doux(0.95, 1.0, rn))          # fin trait d'ombre au bord
    lum = None
    if plume:
        rachis = doux(0.07, 0.03, np.abs(dxn)) * doux(0.05, 0.2, dyn)
        c = melange(c, liseret, 0.45 * rachis)
        barbes = 0.5 + 0.5 * np.cos(2 * np.pi * (dyn * 7 - np.abs(dxn) * 3))
        c = melange(c, c * 0.9, 0.35 * barbes)
    if oeil is not None:                     # « œil » de paon près du bout de chaque plume
        d = np.sqrt((dxn * 1.0) ** 2 + ((dyn - 0.70) * 1.6) ** 2)
        c = melange(c, oeil[0], doux(0.42, 0.38, d))
        c = melange(c, oeil[1], doux(0.30, 0.26, d))
        c = melange(c, oeil[2], doux(0.17, 0.13, d))
        anneau = doux(0.42, 0.38, d) * doux(0.24, 0.30, d)
        lum = liseret * (0.45 * np.maximum(anneau, 0.5 * rim))[..., None]   # lueur discrète
    return c, lum


def ecailles(B):
    return ecailles_motif(B, hexa("#2BB8C4"), hexa("#3B5BD9"), hexa("#BFF4FF"), 16, 26)


def flamant(B):
    return ecailles_motif(B, hexa("#E0507A"), hexa("#FF8FB0"), hexa("#FFD0DE"), 12, 18, plume=True)


def paon(B):
    return ecailles_motif(B, hexa("#1E6EB0"), hexa("#1E9EB0"), hexa("#7FE0F0"), 12, 18, plume=True,
                          oeil=(hexa("#C89A3A"), hexa("#2E8A6A"), hexa("#142A6A")))


def abysses(B):
    c = melange(hexa("#060C22"), hexa("#0E1C44"), B.y / HAUT * 0.6 + 0.4 * B.fbm(6, 4, 4))
    lum = np.zeros(c.shape, np.float32)
    for k in range(5):
        yc = (0.15 + 0.17 * k + 0.04 * np.sin(2 * np.pi * B.x / TOUR * (3 + k) + k)) * HAUT
        d = np.abs(B.y - yc)
        col = hexa("#3D9BFF") if k % 2 == 0 else hexa("#E04BD8")
        trait = doux(0.012, 0.006, d)
        halo = 0.35 * np.exp(-(d / 0.035) ** 2)
        c = melange(c, col, 0.6 * trait + 0.3 * halo)
        lum = lum + col * (trait + halo)[..., None]
    F1, _, _, _, _, R = B.cellules(50, 16, rayon=(0.005, 0.010), presence=0.15)
    plancton = doux(R + 0.002, R - 0.002, F1)
    c = melange(c, hexa("#BFFFF5"), plancton)
    lum = lum + hexa("#BFFFF5") * plancton[..., None]
    return c, np.clip(lum, 0, 1)


# --- V7, point 17 : 5 skins de plus qui ne ressemblaient pas à leur icône ---------------
def lagon(B):
    """Eau de lagon : dégradé turquoise (haut) -> bleu profond (bas), réseau de reflets
    d'eau clairs (caustiques : lignes entre cellules de Worley, déformées)."""
    t = doux(0.1, 1.8, B.y + 0.15 * (B.fbm(6, 3, 3) - 0.5))
    base = melange(hexa("#1F5FA8"), hexa("#2BC4C4"), t)
    F1, F2, _, _, _, _ = B.cellules(22, 7)
    ligne = F2 - F1 + 0.012 * (B.fbm(40, 26, 2) - 0.5)
    reflet = doux(0.010, 0.002, ligne) * (0.45 + 0.55 * t)
    c = melange(base, hexa("#E0FFFF"), 0.7 * reflet)
    return c, None


def grenouille(B):
    """Grenouille : vert vif, taches vert foncé arrondies sur le dos et la tête, ventre
    jaune-vert clair devant en bas."""
    base = melange(hexa("#3CC84A"), hexa("#56D45E"), B.fbm(10, 7, 3))
    F1, _, _, _, _, R = B.cellules(18, 6, rayon=(0.04, 0.085), presence=0.55)
    bord = R * (1 + 0.25 * (B.fbm(40, 26, 2) - 0.5) * 2)
    tache = doux(bord + 0.004, bord - 0.004, F1)
    ventre = doux(0.55, 0.35, np.abs(B.x - TOUR / 2)) * doux(0.9, 0.45, B.y)
    tache = tache * (1 - ventre)
    c = melange(base, hexa("#1E8A3A"), tache)
    return melange(c, hexa("#D8F08A"), ventre), None


def cameleon(B):
    """Caméléon : dégradé jaune (tête) -> turquoise (milieu) -> vert menthe (socle), petits
    points clairs en quinconce, plus nombreux vers la crinière (derrière)."""
    v = B.y / HAUT + 0.05 * (B.fbm(5, 3, 3) - 0.5)
    c = np.where((v < 0.45)[..., None],
                 melange(hexa("#5CD6A0"), hexa("#3CC8C8"), doux(0.0, 0.45, v)),
                 melange(hexa("#3CC8C8"), hexa("#FFD84A"), doux(0.45, 0.85, v)))
    F1, _, _, _, _, R = B.cellules(40, 13, rayon=(0.012, 0.02), presence=0.6)
    derriere = doux(0.6, 1.2, np.abs(B.x - TOUR / 2))
    pois = doux(R + 0.003, R - 0.003, F1) * (0.35 + 0.65 * derriere)
    return melange(c, hexa("#2FA0B0"), 0.8 * pois), None


def coccinelle(B):
    """Coccinelle : rouge verni et GROS points noirs ronds (≈ 14 visibles), aucun sur le
    museau ni autour des yeux (devant, en haut)."""
    base = melange(hexa("#C82030"), hexa("#E03040"), B.fbm(8, 6, 3))
    F1, _, _, _, _, R = B.cellules(9, 3.2, rayon=(0.09, 0.13), presence=0.75)
    visage = doux(0.55, 0.35, np.hypot(B.x - TOUR / 2, (B.y - 1.55) * 0.8))
    point = doux(R + 0.004, R - 0.004, F1) * (1 - visage)
    return melange(base, hexa("#1F1730"), point), None


def sucre_orge(B):
    """Sucre d'orge : 16 bandes fines rouge / blanc cassé, légèrement en spirale, bords nets."""
    s = (B.y / HAUT) * 16 + (B.x / TOUR) * 1.0
    m = doux(0.47, 0.53, 0.5 + 0.5 * np.cos(2 * np.pi * s))
    return melange(hexa("#F4EEE6"), hexa("#D64545"), m), None


SKINS = {
    "animal_zebre": zebre, "animal_tigre": tigre, "animal_chat_roux": chat_roux,
    "animal_dalmatien": dalmatien, "animal_vache": vache, "animal_leopard": leopard,
    "animal_girafe": girafe, "animal_flamant": flamant, "animal_paon": paon,
    "pirate_bois_flotte": bois_flotte, "pirate_corail": corail,
    "pirate_vert_de_gris": vert_de_gris, "pirate_carte": carte, "pirate_tempete": tempete,
    "pirate_or_maudit": or_maudit, "pirate_ecailles": ecailles, "pirate_abysses": abysses,
    # V7, point 17 (à brancher par le programmeur : docs/specs/v7-textures-skins.md)
    "pirate_lagon": lagon, "animal_grenouille": grenouille, "animal_cameleon": cameleon,
    "animal_coccinelle": coccinelle, "motif_sucre_orge": sucre_orge,
}


# ---------------------------------------------------------------------------------------
# « Avant » : reproduction du motif calculé actuel (SkinPatterns.cs) pour les planches.
# 256 px, bords tout-ou-rien, comme le code (les bruits diffèrent un peu de Mathf.Perlin).
# ---------------------------------------------------------------------------------------
def avant(nom, B):
    d = {"animal_zebre": ("ond", 16, "#F4EEE6", "#1F1730"), "animal_tigre": ("ond", 12, "#F28C38", "#1F1730"),
         "animal_chat_roux": ("ond", 10, "#E8873A", "#B8602A")}
    x, y = B.x, B.y
    u, v = x / TOUR, y / HAUT
    if nom in d:
        _, n, a, b = d[nom]
        s = v * n + 0.18 * np.sin(u * np.pi * 6 + v * 5) + 0.35 * (B.valeur(6, 3) - 0.5)
        w = 0.4 + 0.12 * np.sin(u * np.pi * 10)
        return melange(hexa(a)[None, None] * np.ones_like(x)[..., None], hexa(b), (s % 1) < w), None
    rng = np.random.default_rng(1)

    def semis(n, r0, r1, v0=0.0, v1=1.0):
        return [(rng.random(), v0 + (v1 - v0) * rng.random(), r0 + (r1 - r0) * rng.random()) for _ in range(n)]

    def dist(su, sv):
        du = np.abs(u - su)
        du = np.minimum(du, 1 - du) * TOUR
        return np.sqrt(du * du + ((v - sv) * HAUT) ** 2)

    def fond(h):
        return np.ones(x.shape + (3,), np.float32) * hexa(h)
    if nom in ("animal_dalmatien", "animal_vache"):
        c = fond("#F4EEE6")
        pts = semis(70, 0.02, 0.07) if nom == "animal_dalmatien" else semis(14, 0.14, 0.28)
        wob = 0 if nom == "animal_dalmatien" else 0.18 * (B.valeur(10, 5) - 0.5)
        m = np.zeros(x.shape, bool)
        for su, sv, r in pts:
            m |= dist(su, sv) < r + wob
        return melange(c, hexa("#1F1730" if nom == "animal_dalmatien" else "#6B4A35"), m), None
    if nom == "animal_leopard":
        c = fond("#E0B070")
        for su, sv, r in semis(60, 0.04, 0.07):
            dd = dist(su, sv)
            c = np.where((dd < r * 0.55)[..., None], hexa("#B07A3A"), np.where((dd < r)[..., None], hexa("#4A2E1A"), c))
        return c, None
    if nom == "animal_girafe":
        pts = semis(90, 0, 0)
        ds = np.sort(np.stack([dist(a, b) for a, b, _ in pts]), axis=0)
        return melange(fond("#8C5933"), hexa("#E8D8A8"), (ds[1] - ds[0]) < 0.03), None
    if nom in ("pirate_ecailles", "animal_flamant", "animal_paon"):
        p = nom != "pirate_ecailles"
        c1, c2, c3 = {"pirate_ecailles": ("#3B5BD9", "#2BB8C4", "#BFF4FF"), "animal_flamant": ("#E0507A", "#FF8FB0", "#FFD0DE"),
                      "animal_paon": ("#1E6EB0", "#1E9EB0", "#7FE0F0")}[nom]
        rows, cols = (12, 18) if p else (16, 26)
        ry = v * rows
        row = np.floor(ry)
        cx = u * cols + np.where(row % 2 == 0, 0, 0.5)
        fx, fy = cx % 1 - 0.5, ry % 1
        r = np.sqrt(fx * fx * (1.6 if p else 1) + fy * fy)
        base = melange(hexa(c2), hexa(c1), v) if not p else melange(hexa(c1), hexa(c2), v)
        c = np.where((r <= 0.42)[..., None], melange(base * 0.92, base, fy), np.where((r < 0.5)[..., None], melange(base, hexa(c3), np.full(r.shape, 0.6)), base * 0.82))
        return c, None
    if nom == "pirate_bois_flotte":
        g = B.valeur(70, 1.5)
        return melange(fond("#B9AE9C"), hexa("#8C8170"), doux(0.55, 0.75, g)), None
    if nom == "pirate_corail":
        c = fond("#FF7A6B")
        for su, sv, r in semis(260, 0.012, 0.022):
            dd = dist(su, sv)
            c = np.where((dd < r)[..., None], np.where((dd > r * 0.55)[..., None], hexa("#FFC2B0"), melange(hexa("#FF7A6B"), hexa("#FFC2B0"), np.full(dd.shape, 0.4))), c)
        return c, None
    if nom == "pirate_vert_de_gris":
        n = B.valeur(9, 3.5) * 0.7 + B.valeur(25, 10) * 0.3
        return np.where((n > 0.52)[..., None], hexa("#5FB8A0"), melange(hexa("#B0703A"), hexa("#B0703A") * 1.15, n)), None
    if nom == "pirate_carte":
        c = melange(fond("#E9D3A0"), hexa("#8C5933"), 0.35 * doux(0.5, 0.8, B.valeur(10, 5)))
        pv = 0.45 + 0.18 * np.sin(u * np.pi * 4 + 1)
        return np.where(((np.abs(v - pv) * HAUT < 0.012) & ((u * 60) % 1 < 0.5))[..., None], hexa("#C0392B"), c), None
    if nom == "pirate_tempete":
        n = B.valeur(4, 1.5) + 0.5 * B.valeur(12, 4.5)
        s = np.abs(np.sin((v * 6 + n * 3) * np.pi))
        return melange(fond("#4A5470"), hexa("#7A86A8"), doux(0.3, 0.9, s)), None
    if nom == "pirate_or_maudit":
        c = melange(fond("#FFC83A"), hexa("#FFC83A") * 0.85, B.valeur(10, 5))
        n = B.valeur(7, 4.5)
        m = np.abs(n - 0.5) < 0.018
        c = np.where(m[..., None], melange(hexa("#FFC83A"), hexa("#3CFF9A"), np.full(n.shape, 0.6)), c)
        return c, hexa("#3CFF9A") * m[..., None]
    if nom == "pirate_abysses":
        c = fond("#0A1430")
        lum = np.zeros(c.shape, np.float32)
        for k in range(5):
            lv = 0.15 + 0.17 * k + 0.04 * np.sin(u * np.pi * 2 * (3 + k) + k)
            m = np.abs(v - lv) * HAUT < 0.012
            col = hexa("#3D9BFF" if k % 2 == 0 else "#E04BD8")
            c = np.where(m[..., None], melange(hexa("#0A1430"), col, np.full(m.shape, 0.5)), c)
            lum = np.where(m[..., None], col, lum)
        return c, lum
    return None, None


# ---------------------------------------------------------------------------------------
def ecrire(chemin, rgb):
    h, w = rgb.shape[:2]
    out = np.concatenate([np.clip(rgb, 0, 1), np.ones((h, w, 1), np.float32)], axis=2)
    img = bpy.data.images.new(os.path.basename(chemin), w, h, alpha=False)
    img.pixels.foreach_set(np.ascontiguousarray(out, dtype=np.float32).ravel())   # ligne 0 = bas
    img.filepath_raw = chemin
    img.file_format = "PNG"
    img.save()
    bpy.data.images.remove(img)


def reduire(a, f):
    h, w = a.shape[:2]
    return a.reshape(h // f, f, w // f, f, -1).mean(axis=(1, 3))


def principal():
    os.makedirs(SORTIE, exist_ok=True)
    choix = os.environ.get("SKINS", "").split() or list(SKINS)
    vieux = os.environ.get("AVANT") == "1"
    for nom in choix:
        graine = sum(ord(ch) for ch in nom) * 7      # même dessin à chaque lancement
        if vieux:
            B = Bruits(256, graine)
            c, lum = avant(nom, B)
            if c is None:
                continue
            # agrandi « au plus proche » en 1024 : ce que montre le jeu de près
            c = np.repeat(np.repeat(c, 4, 0), 4, 1)
            lum = None if lum is None else np.repeat(np.repeat(lum, 4, 0), 4, 1)
        else:
            B = Bruits(TAILLE * SUR, graine)
            c, lum = SKINS[nom](B)
            c = reduire(c, SUR)
            lum = None if lum is None else reduire(lum, SUR)
        ecrire(os.path.join(SORTIE, f"{nom}.png"), c)
        if lum is not None:
            ecrire(os.path.join(SORTIE, f"{nom}_lumiere.png"), lum)
        print("TEXTURE", nom, "lumière" if lum is not None else "")


if __name__ == "__main__":
    principal()
