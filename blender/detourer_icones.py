"""Détourage des icônes du shop : rend transparent le fond lavande (#E6E1EE) et ses ombres.

Lancer avec :
  /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/detourer_icones.py
Seulement quelques icônes :   ICONES="piece tricorne" .../Blender --background --python ...
Écrire ailleurs (essais) :     SORTIE=<dossier> ...
Voir le masque de fond (rouge) : DEBUG=1 (écrit <nom>_debug.png à côté).

On utilise le Python de Blender parce qu'il contient déjà numpy et sait lire/écrire des PNG.

V7 — NOUVELLE MÉTHODE (l'ancienne effaçait les blancs et les gris des objets)
L'ancienne version prenait pour du fond tout pixel « de la couleur du fond », et pour de
l'ombre tout pixel gris un peu sombre. Or les rayures blanches du zèbre ou la coquille du
poussin ont presque la couleur du fond : elles disparaissaient.
La nouvelle idée : le fond est LISSE (sa couleur change très lentement d'un pixel à l'autre),
alors que le bord d'un objet est une MARCHE nette, même entre deux couleurs proches.
1. On modélise la couleur du fond, qui n'est pas parfaitement uni (léger dégradé) : une
   surface douce (polynôme) ajustée sur les bords de l'image.
2. On calcule la « pente » de chaque pixel : son plus grand écart avec ses voisins.
3. On remplit depuis les bords de l'image, mais le remplissage ne passe QUE par des pixels
   lisses ET qui ressemblent au fond ou à une ombre du fond (même couleur, plus sombre).
   Il s'arrête donc au contour de l'objet, même si l'objet est blanc. Une ombre portée,
   douce et reliée au fond, est remplie et devient transparente ; un gris DANS l'objet est
   séparé du fond par le bord de l'objet : il reste opaque.
4. Le bord (1 à 3 pixels) reçoit une transparence calculée par « matting » : on compare
   chaque pixel à la couleur du fond voisin et à celle de l'objet voisin, et on retire le
   lavande mélangé au bord (plus de liseré clair, plus d'encoches noires).
5. Marge : si l'objet touche presque le bord, on réduit l'image pour que le contour noir
   tienne en entier.
6. Contour noir épais façon « autocollant ».
Les icônes RENDUES dans Blender (rendu_icones.py) ont en plus leur propre transparence,
parfaite, dans art/icones_alpha/ : on l'utilise directement à la place des étapes 1 à 4.
Les fichiers d'origine sont conservés ; les versions détourées vont dans art/icones_detourees/.
"""
import os
import bpy
import numpy as np

RACINE = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SOURCE = os.path.join(RACINE, "art", "icones")
ALPHA = os.path.join(RACINE, "art", "icones_alpha")     # rendus Blender sur fond transparent
SORTIE = os.environ.get("SORTIE") or os.path.join(RACINE, "art", "icones_detourees")
DEBUG = os.environ.get("DEBUG") == "1"

# --- Réglages généraux (valables pour presque toutes les icônes) -------------------------
PENTE_MAX = 0.018   # écart max entre voisins (après léger flou) pour qu'un pixel soit « lisse »
PENTE_OMBRE = 0.060 # pente tolérée dans une ombre portée (son dégradé est plus rapide)
SEUIL_FOND = 0.045  # distance de couleur au fond (0..1) en dessous de laquelle c'est du fond
# Ombre portée : pixel plus sombre que le fond (facteur entre OMBRE_MIN et 1) et de même
# teinte (lavande / violet) : angle de teinte inférieur à OMBRE_ANGLE degrés.
OMBRE_MIN = 0.30
OMBRE_ANGLE = 28.0
CONTACT_PX = 12      # débordement dans l'ombre de contact (pixels)
CONTACT_ANGLE = 50.0 # écart de teinte toléré dans l'ombre de contact (degrés)
OMBRE_HAUTEUR = 0.45  # l'ombre commence au plus haut à 45 % de la hauteur de l'objet
CONTOUR_PX = 30     # épaisseur du contour noir (pixels, pour 1024 px). 0 = pas de contour.
MARGE_PX = CONTOUR_PX + 8   # distance minimale entre l'objet et le bord de l'image
COULEUR_CONTOUR = np.array([0x11, 0x0D, 0x1F], dtype=np.float32) / 255.0

# --- Réglages par icône -----------------------------------------------------------------
# Objets en anneau ou troués : le fond visible AU MILIEU (trou de la chaîne, de l'écharpe,
# yeux du masque) n'est pas relié au bord de l'image. Pour eux, les zones lisses et très
# proches du fond enfermées par l'objet deviennent aussi transparentes.
OBJETS_TROUES = {
    "echarpe.png", "collier_fleurs.png", "aureole.png", "masque_heros.png", "icone_rebirth.png",
    # V7 : chaînes, lanières, collier, écharpe renard, yeux du loup
    "ancre_doree.png", "boussole.png", "cache_oeil.png", "longue_vue.png",
    "collier_medaille.png", "echarpe_renard.png", "loup_corsaire.png", "foulard_marin.png",
}
SEUIL_TROU = 0.06
TROU_MIN_PX = 3     # un trou doit faire au moins ~2 × 3 px de large (pas un simple reflet)
# Réglages particuliers : nom -> dictionnaire de réglages qui remplacent ceux du haut.
#  - "pente" : objets au bord très doux (on serre la pente) ;
#  - "ombre": False : objet gris clair ou argenté dont le côté éclairé ressemble à une ombre.
REGLAGES = {
    # Objets blancs ou pastel dont le côté ombré a presque la couleur du fond (pompon,
    # plume, nacre…) : on n'accepte comme fond que les pixels TRÈS proches du lavande.
    **{n: {"fond": 0.03} for n in [
        "chapeau_fete.png", "echarpe.png", "collier_fleurs.png", "lunettes_coeur.png",
        "matiere_arc_en_ciel.png", "matiere_cristal.png", "boite_skins_1.png",
        "boite_accessoires_1.png", "chapeau_bonnet.png", "icone_inventaire.png",
        "icone_index.png", "icone_boutique.png", "motif_pois.png", "motif_sucre_orge.png",
        "chapeau_capitaine.png", "bocal_poisson.png", "icone_mes_skins.png"]},
    # bébé kraken : son corps est VIOLET comme une ombre ; on n'accepte que l'ombre claire
    "kraken_bebe.png": {"ombre_min": 0.72, "pente_ombre": 0.03, "contact": 0},
    "icone_reglages.png": {"ombre": False},
    "icone_effets.png": {"ombre": False},
    "icone_rotation.png": {"ombre": False},
    "matiere_argent.png": {"ombre": False},
    "couleur_nacre.png": {"ombre": False},
    "medaille_argent.png": {"ombre": False},
    # Objets troués posés « à plat » : leur ombre tombe aussi DANS la boucle et sur le côté,
    # même en haut de l'image (ombre_hauteur 0 = l'ombre peut être partout).
    "longue_vue.png": {"trou_ombre": True, "ombre_hauteur": 0.0},
    "cache_oeil.png": {"trou_ombre": True},
    "boussole.png": {"trou_ombre": True},
    "ancre_doree.png": {"trou_ombre": True},
    # yeux du loup : le fond vu par les trous est ombré. Le masque lui-même est violet foncé
    # (k ≈ 0,33) : on n'accepte que des ombres plus claires que lui.
    "loup_corsaire.png": {"trou": 0.15, "trou_ombre": True, "ombre_min": 0.65},
    # médaille de bronze : un halo orangé flou à gauche de la médaille (reflet du générateur)
    "medaille_bronze.png": {"fond": 0.10},
    # sucre d'orge : le côté ombré des rayures blanches a presque la couleur du fond, seule
    # une pente plus stricte les sépare
    "motif_sucre_orge.png": {"fond": 0.03, "pente": 0.010, "ombre": False},
    # navire fantôme : halo jaune flou autour de la lanterne
    "navire_fantome.png": {"fond": 0.10},
    # cheval des abysses : halo crème flou autour de la lanterne
    "pirate_abysses.png": {"fond": 0.14},
    # or maudit : brume vert pâle aux bords très doux
    "pirate_or_maudit.png": {"fond": 0.03, "pente": 0.010},
    # bonnet : le dessous ombré du revers blanc ressemble à une ombre ; pas d'ombre portée
    "chapeau_bonnet.png": {"fond": 0.03, "pente": 0.010, "ombre": False},
    # capsule violette : sa couleur est celle d'une ombre, et l'image n'a pas d'ombre portée
    "couleur_violet.png": {"ombre": False},
}


# ---------------------------------------------------------------------------------------
# Outils sur les images (numpy seulement : le Python de Blender n'a pas scipy)
# ---------------------------------------------------------------------------------------
def somme_boite(a, r):
    """Somme de chaque fenêtre carrée (2r+1)² autour de chaque pixel (bords : zéro).
    On passe par des sommes cumulées : c'est rapide quelle que soit la taille r."""
    a = a.astype(np.float32)
    pad = np.pad(a, [(r + 1, r)] + [(r + 1, r)] + [(0, 0)] * (a.ndim - 2))
    c = pad.cumsum(0).cumsum(1)
    n = 2 * r + 1
    return c[n:, n:] - c[:-n, n:] - c[n:, :-n] + c[:-n, :-n]


def dilater(m, r=1):
    return somme_boite(m, r) > 0.5


def eroder(m, r=1):
    return ~dilater(~m, r)


def remplir(graines, passable):
    """Remplissage (« pot de peinture ») : tous les pixels `passable` reliés aux graines.
    Astuce de vitesse : on remplit des segments entiers de ligne, puis de colonne, et on
    recommence jusqu'à ce que plus rien ne change (quelques dizaines de tours au lieu de
    milliers de tours pixel par pixel)."""
    rempli = graines & passable
    h, w = passable.shape
    # numéro de segment : il augmente à chaque pixel non passable (+ décalage par ligne)
    seg_l = np.cumsum(~passable, axis=1) + np.arange(h)[:, None] * (w + 1)
    seg_c = np.cumsum(~passable, axis=0) + np.arange(w)[None, :] * (h + 1)
    avant = -1
    while True:
        for seg in (seg_l, seg_c):
            touche = np.zeros(seg.max() + 1, dtype=bool)
            touche[seg[rempli]] = True
            rempli = passable & touche[seg]
        # diagonales : on autorise aussi le passage en diagonale (fonds en escalier)
        rempli = passable & (rempli | dilater(rempli, 1) & passable)
        n = int(rempli.sum())
        if n == avant:
            return rempli
        avant = n


def moyenne_masquee(img, masque, r):
    """Couleur moyenne des pixels du masque autour de chaque pixel (fenêtre de rayon r).
    Renvoie (moyenne, nombre de pixels trouvés)."""
    n = somme_boite(masque, r)
    s = somme_boite(img * masque[:, :, None], r)
    return s / np.maximum(n, 1)[:, :, None], n


def fond_lisse(px):
    """Modèle de la couleur du fond : polynôme de degré 2 en (x, y), par canal, ajusté sur
    une bande de 16 px le long des 4 bords. On écarte deux fois les pixels trop éloignés
    (un objet ou une ombre qui touche le bord) pour ne garder que le vrai fond."""
    h, w, _ = px.shape
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    xx, yy = xx / w - 0.5, yy / h - 0.5
    bord = np.zeros((h, w), dtype=bool)
    b = 16
    bord[:b, :] = bord[-b:, :] = bord[:, :b] = bord[:, -b:] = True
    base = np.stack([np.ones_like(xx), xx, yy, xx * xx, xx * yy, yy * yy], axis=-1)
    garde = bord.copy()
    mediane = np.median(px[bord], axis=0)
    garde &= np.sqrt(((px - mediane) ** 2).sum(2)) < 0.12
    for _ in range(3):
        A = base[garde]
        coef, *_ = np.linalg.lstsq(A, px[garde], rcond=None)
        modele = base @ coef
        ecart = np.sqrt(((px - modele) ** 2).sum(2))
        garde = bord & (ecart < max(0.02, 2.5 * float(np.median(ecart[garde]))))
    return modele


def redimensionner(img, taille):
    """Redimensionnement bilinéaire d'une image (h, w, c) carrée vers taille × taille."""
    h, w = img.shape[:2]
    y = (np.arange(taille) + 0.5) * h / taille - 0.5
    x = (np.arange(taille) + 0.5) * w / taille - 0.5
    y0 = np.clip(np.floor(y).astype(int), 0, h - 1)
    x0 = np.clip(np.floor(x).astype(int), 0, w - 1)
    y1, x1 = np.clip(y0 + 1, 0, h - 1), np.clip(x0 + 1, 0, w - 1)
    fy = np.clip(y - y0, 0, 1)[:, None, None]
    fx = np.clip(x - x0, 0, 1)[None, :, None]
    a = img[y0][:, x0] * (1 - fx) + img[y0][:, x1] * fx
    b = img[y1][:, x0] * (1 - fx) + img[y1][:, x1] * fx
    return a * (1 - fy) + b * fy


def lire(chemin):
    img = bpy.data.images.load(chemin)
    w, h = img.size
    px = np.empty(w * h * 4, dtype=np.float32)
    img.pixels.foreach_get(px)
    bpy.data.images.remove(img)
    return px.reshape(h, w, 4)[::-1].copy()   # Blender range les lignes de bas en haut


def ecrire(chemin, px):
    h, w = px.shape[:2]
    img = bpy.data.images.new(os.path.basename(chemin), w, h, alpha=True)
    img.pixels.foreach_set(np.ascontiguousarray(px[::-1], dtype=np.float32).ravel())
    img.filepath_raw = chemin
    img.file_format = "PNG"
    img.save()
    bpy.data.images.remove(img)


# ---------------------------------------------------------------------------------------
# Le détourage
# ---------------------------------------------------------------------------------------
def masque_fond(rgb, nom):
    """Renvoie (fond, ombre_ou_fond) : les pixels de fond (dont les ombres portées)."""
    reg = REGLAGES.get(nom, {})
    h, w, _ = rgb.shape
    # léger flou 5 × 5 (divisé par le vrai nombre de voisins, pour ne pas assombrir les bords).
    # Il efface le « grain » de certains fonds (motif tramé dans le coin en bas à droite).
    flou = somme_boite(rgb, 2) / somme_boite(np.ones((h, w), dtype=np.float32), 2)[:, :, None]
    modele = fond_lisse(rgb)
    # pente : plus grand écart (sur un canal) entre un pixel et ses voisins à 2 px de distance
    pente = np.zeros((h, w), dtype=np.float32)
    for dy, dx in ((0, 2), (2, 0), (2, 2), (2, -2)):
        d = np.abs(flou - np.roll(np.roll(flou, dy, 0), dx, 1)).max(2)
        pente = np.maximum(pente, np.maximum(d, np.roll(np.roll(d, -dy, 0), -dx, 1)))
    lisse = pente < reg.get("pente", PENTE_MAX)
    dist = np.sqrt(((flou - modele) ** 2).sum(2))
    ressemble = dist < reg.get("fond", SEUIL_FOND)
    passable = lisse & ressemble
    if reg.get("ombre", True):
        # Ombre portée : plus sombre que le fond (facteur k), et de la MÊME teinte que lui.
        # La teinte se lit dans l'écart de chaque canal à la moyenne (vecteur « chroma ») :
        # pour une ombre lavande ou violette, il pointe dans la même direction que celui du
        # fond (angle < OMBRE_ANGLE), même s'il est plus long (ombre plus saturée).
        k = flou.mean(2) / np.maximum(modele.mean(2), 1e-3)
        chroma = flou - flou.mean(2, keepdims=True)
        chroma_fond = modele - modele.mean(2, keepdims=True)
        n1 = np.sqrt((chroma ** 2).sum(2))
        n2 = np.sqrt((chroma_fond ** 2).sum(2))
        cos = (chroma * chroma_fond).sum(2) / np.maximum(n1 * n2, 1e-6)
        # (un gris neutre n'est PAS une ombre : c'est souvent le côté ombré d'un objet blanc)
        meme_teinte = (cos > np.cos(np.radians(reg.get("ombre_angle", OMBRE_ANGLE)))) & (n1 > 0.6 * n2)
        ombre = (k > reg.get("ombre_min", OMBRE_MIN)) & (k < 1.0) & meme_teinte
        # Une ombre portée est posée au SOL, donc dans la moitié basse de l'objet. Plus haut,
        # un pixel « gris lavande » est le côté à l'ombre d'un objet blanc (plume, pompon) :
        # on ne l'accepte que s'il est presque aussi clair que le fond (léger dégradé du fond).
        coeur = ~(dist < SEUIL_FOND) & ~ombre
        ys = np.nonzero(coeur.any(1))[0]
        if len(ys):
            haut, bas = ys.min(), ys.max()
            ligne = haut + reg.get("ombre_hauteur", OMBRE_HAUTEUR) * (bas - haut)
            dessus = (np.arange(h) < ligne)[:, None]
            ombre &= ~dessus | (k > 0.95)
        # Le dégradé d'une ombre est plus marqué que celui du fond (surtout tout près de
        # l'objet) : on y tolère une pente plus forte. Le bord entre l'objet et son ombre
        # reste net, car leurs couleurs diffèrent nettement.
        passable |= ombre & (pente < reg.get("pente_ombre", PENTE_OMBRE))
        ressemble |= ombre
        # zone de contact (sous l'objet) : couleur d'ombre, ou presque couleur du fond
        # L'ombre de contact est souvent un peu rosée (lumière renvoyée par l'objet) : on
        # tolère ici un écart de teinte plus grand, mais seulement pour des pixels pas trop
        # sombres (le dessous sombre d'un chapeau, k < 0,4, reste à l'objet).
        large = (k > reg.get("contact_kmin", 0.4)) & (k < 1.0) & (n1 > 0.6 * n2) & \
                (cos > np.cos(np.radians(reg.get("contact_angle", CONTACT_ANGLE))))
        zone_ombre = (large | (dist < 0.08)) & ~dessus if len(ys) else ombre
    else:
        zone_ombre = np.zeros((h, w), dtype=bool)
    graines = np.zeros((h, w), dtype=bool)
    graines[:2, :] = graines[-2:, :] = graines[:, :2] = graines[:, -2:] = True
    fond = remplir(graines, passable)
    # Tout près de l'objet, l'ombre de contact devient très vite sombre : sa pente dépasse le
    # seuil et un mince filet d'ombre restait collé sous l'objet. On laisse donc le fond
    # « déborder » de quelques pixels dans les pixels couleur d'ombre, quelle que soit la pente.
    for _ in range(reg.get("contact", CONTACT_PX)):
        fond |= dilater(fond, 1) & zone_ombre
    # Nettoyage : petites miettes opaques isolées dans le fond (grain, filigrane). On ne garde
    # que les morceaux d'objet reliés à une partie « épaisse » (au moins 13 × 13 px), et on
    # n'efface que des miettes de la couleur du fond ou de l'ombre.
    objet = ~fond
    principal = remplir(eroder(objet, 6), objet)
    fond |= objet & ~principal & (ressemble | (dist < 0.15))
    # Trous des objets en anneau : zones lisses très proches du fond, assez larges.
    if nom in OBJETS_TROUES:
        trou = (pente < 0.03) & (dist < reg.get("trou", SEUIL_TROU)) & ~fond
        if reg.get("trou_ombre"):
            # le fond vu par le trou porte l'ombre de l'objet : mêmes règles que dehors
            trou |= passable & ~fond
        coeur = eroder(trou, TROU_MIN_PX)
        fond |= remplir(coeur, trou)
    return fond, dist


def detourer_couleur(rgb, nom):
    """Détourage d'une image sur fond lavande. Renvoie (couleur nettoyée, alpha)."""
    fond, _ = masque_fond(rgb, nom)
    objet = ~fond
    # Bande de bord : pixels de l'objet à moins de 3 px du fond. Ils mélangent souvent
    # l'objet et le lavande (anticrénelage) : on calcule leur vraie transparence.
    bande = objet & dilater(fond, 3)
    coeur_fond = fond & ~dilater(objet, 2)
    coeur_objet = objet & ~bande
    B, nb = moyenne_masquee(rgb, coeur_fond, 6)      # couleur du fond tout près
    F, nf = moyenne_masquee(rgb, coeur_objet, 4)     # couleur de l'objet tout près
    B2, _ = moyenne_masquee(rgb, coeur_fond, 16)
    F2, _ = moyenne_masquee(rgb, coeur_objet, 10)
    B = np.where((nb > 0)[:, :, None], B, B2)
    F = np.where((nf > 0)[:, :, None], F, F2)
    FB = F - B
    norme2 = (FB ** 2).sum(2)
    a = ((rgb - B) * FB).sum(2) / np.maximum(norme2, 1e-6)
    # si l'objet a la couleur du fond, on ne peut pas trancher : on garde le pixel
    a = np.where(norme2 < 0.03 ** 2, 1.0, a)
    a = np.clip((a - 0.15) / 0.75, 0.0, 1.0)          # on durcit un peu le bord
    alpha = np.where(objet, 1.0, 0.0).astype(np.float32)
    alpha[bande] = a[bande]
    # couleur nettoyée : on retire la part de fond mélangée au pixel du bord
    propre = (rgb - (1 - alpha[:, :, None]) * B) / np.maximum(alpha[:, :, None], 1e-3)
    propre = np.where((alpha > 0.35)[:, :, None], np.clip(propre, 0, 1), F)
    couleur = np.where(bande[:, :, None], propre, rgb)
    return couleur, alpha, fond


def ajouter_marge(couleur, alpha):
    """Si l'objet est à moins de MARGE_PX du bord, on rétrécit l'image (en ajoutant du vide
    autour) pour que le contour noir ne soit jamais coupé."""
    h, w = alpha.shape
    ys, xs = np.nonzero(alpha > 0.05)
    if len(ys) == 0:
        return couleur, alpha, 0
    proche = min(ys.min(), xs.min(), h - 1 - ys.max(), w - 1 - xs.max())
    if proche >= MARGE_PX:
        return couleur, alpha, 0
    ajout = MARGE_PX - proche
    pre = np.concatenate([couleur * alpha[:, :, None], alpha[:, :, None]], axis=2)
    pre = np.pad(pre, [(ajout, ajout), (ajout, ajout), (0, 0)])
    pre = redimensionner(pre, h)
    a = pre[:, :, 3]
    c = pre[:, :, :3] / np.maximum(a, 1e-4)[:, :, None]
    return np.clip(c, 0, 1), np.clip(a, 0, 1), ajout


def contour(couleur, alpha, epaisseur=None):
    """Contour noir épais : on « grossit » la silhouette de CONTOUR_PX pixels (filtre
    maximum répété, en diagonale une fois sur deux pour un contour presque rond), puis on
    pose l'objet par-dessus. Le filtre travaille en nombres décimaux : bord anticrénelé.
    `epaisseur` : autre épaisseur (rendu_lot9.py s'en sert pour les décorations)."""
    grand = alpha.copy()
    for i in range(CONTOUR_PX if epaisseur is None else epaisseur):
        g = grand.copy()
        g[1:, :] = np.maximum(g[1:, :], grand[:-1, :])
        g[:-1, :] = np.maximum(g[:-1, :], grand[1:, :])
        g[:, 1:] = np.maximum(g[:, 1:], grand[:, :-1])
        g[:, :-1] = np.maximum(g[:, :-1], grand[:, 1:])
        if i % 2:
            g[1:, 1:] = np.maximum(g[1:, 1:], grand[:-1, :-1])
            g[:-1, :-1] = np.maximum(g[:-1, :-1], grand[1:, 1:])
            g[1:, :-1] = np.maximum(g[1:, :-1], grand[:-1, 1:])
            g[:-1, 1:] = np.maximum(g[:-1, 1:], grand[1:, :-1])
        grand = g
    a = alpha[:, :, None]
    rgb = couleur * a + COULEUR_CONTOUR * (1.0 - a)
    return rgb, grand


def detourer(nom):
    px = lire(os.path.join(SOURCE, nom))
    rgb = px[:, :, :3]
    chemin_alpha = os.path.join(ALPHA, nom)
    fond = None
    if os.path.exists(chemin_alpha):
        # Rendu Blender : la transparence du rendu est exacte, on la prend telle quelle.
        ra = lire(chemin_alpha)
        couleur, alpha, mode = ra[:, :, :3].copy(), ra[:, :, 3].copy(), "rendu Blender"
        # Parties transparentes ENFERMÉES dans l'objet (verre du bocal, ailes d'abeille) :
        # on y remet l'image d'origine (déjà posée sur son fond) et on les rend opaques,
        # sinon le contour noir se verrait au travers. Les vrais trous (alpha nul : anneau
        # du collier, de l'écharpe) restent transparents.
        bas = alpha < 0.5
        graines = np.zeros_like(bas)
        graines[0, :] = graines[-1, :] = graines[:, 0] = graines[:, -1] = True
        dedans = ~remplir(graines, bas)
        verre = dedans & (alpha < 0.999) & (alpha > 0.02)
        couleur[verre] = rgb[verre]
        alpha[verre] = 1.0
    else:
        couleur, alpha, fond = detourer_couleur(rgb, nom)
        mode = "fond lavande"
    couleur, alpha, ajout = ajouter_marge(couleur, alpha)
    if CONTOUR_PX > 0:
        couleur, alpha = contour(couleur, alpha)
    out = np.concatenate([couleur, alpha[:, :, None]], axis=2).astype(np.float32)
    ecrire(os.path.join(SORTIE, nom), out)
    if DEBUG and fond is not None:
        dbg = np.concatenate([rgb.copy(), np.ones_like(alpha)[:, :, None]], axis=2)
        dbg[fond, 0] = 1.0
        dbg[fond, 1] *= 0.4
        ecrire(os.path.join(SORTIE, nom[:-4] + "_debug.png"), dbg)
    marge = f", marge +{ajout} px" if ajout else ""
    print(f"OK {nom} ({mode}{marge}) : {float((alpha < 0.5).mean()) * 100:.0f} % transparent")


def principal():
    os.makedirs(SORTIE, exist_ok=True)
    seules = set((n + ".png") for n in os.environ.get("ICONES", "").split())
    for nom in sorted(os.listdir(SOURCE)):
        if nom.endswith(".png") and (not seules or nom in seules):
            detourer(nom)


# Lancé par Blender (--python) : on détoure. Importé par un autre script : on ne fait rien,
# on prête seulement ses fonctions (contour, lire, ecrire…).
if __name__ == "__main__":
    principal()
