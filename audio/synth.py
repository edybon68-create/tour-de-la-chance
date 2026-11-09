# -*- coding: utf-8 -*-
"""
synth.py — la « boîte à outils » commune du sound designer de Tour de la Chance.

Chaque son du jeu est FABRIQUÉ par calcul (synthèse) : aucun enregistrement, aucun son
téléchargé. Ce module regroupe les briques de base utilisées par tous les scripts :

- oscillateurs : sinus, carré, triangle, dents de scie, bruit ;
- enveloppes : ADSR et « percussive » (attaque puis chute) ;
- glissements de fréquence (glissando) et vibrato ;
- filtres : passe-bas, passe-haut, passe-bande (fixes) et passe-bas « qui s'ouvre » ;
- timbres prêts à l'emploi : boîte à musique, harpe, glockenspiel, clochette, pièce, orgue...
- réverbération légère, fondus anti-clic, normalisation, écriture WAV 16 bits.

Il a besoin de numpy. Le Python système de macOS n'a pas numpy, mais celui de Blender l'a :
    /Applications/Blender.app/Contents/Resources/4.5/python/bin/python3.11 audio/generer_tout.py

Petit rappel de vocabulaire :
- un SIGNAL est un tableau de nombres entre -1 et +1, un nombre par « échantillon » ;
- on joue 44 100 échantillons par seconde (SR = « sample rate ») ;
- 0 dBFS = le niveau maximal possible ; -1 dBFS = un peu en dessous (marge de sécurité).
"""

import os
import wave

import numpy as np

SR = 44100  # échantillons par seconde (qualité CD)
NYQUIST = SR / 2  # au-delà de cette fréquence, un son numérique se replie (« aliasing »)


# ---------------------------------------------------------------------------
# Outils de base : temps, notes
# ---------------------------------------------------------------------------

def n_ech(duree):
    """Nombre d'échantillons pour une durée en secondes."""
    return int(round(duree * SR))


def temps(duree):
    """Axe du temps (en secondes) : 0, 1/SR, 2/SR, ..."""
    return np.arange(n_ech(duree)) / SR


NOMS_NOTES = {"C": 0, "D": 2, "E": 4, "F": 5, "G": 7, "A": 9, "B": 11}


def midi(nom):
    """'C5' -> 72, 'Bb4' -> 70, 'F#3' -> 54. (Notation anglaise : C = do, D = ré, ...)."""
    lettre = nom[0].upper()
    reste = nom[1:]
    alteration = 0
    while reste and reste[0] in "#b":
        alteration += 1 if reste[0] == "#" else -1
        reste = reste[1:]
    octave = int(reste)
    return 12 * (octave + 1) + NOMS_NOTES[lettre] + alteration


def hz(note):
    """Fréquence d'une note : accepte un nom ('A4') ou un numéro MIDI (69). La4 = 440 Hz."""
    m = midi(note) if isinstance(note, str) else note
    return 440.0 * 2.0 ** ((m - 69) / 12.0)


def _comme_tableau(freq, n):
    """Transforme une fréquence fixe en tableau, pour traiter fixes et glissandos pareil."""
    if np.isscalar(freq):
        return np.full(n, float(freq))
    f = np.asarray(freq, dtype=float)
    if len(f) != n:
        raise ValueError("le tableau de fréquences n'a pas la bonne longueur")
    return f


def phase(freq, duree, phase0=0.0):
    """
    Phase (en radians) d'un oscillateur. Pour une fréquence qui change dans le temps, on
    additionne les petits pas 2*pi*f/SR : c'est ce qui permet des glissandos sans à-coup.
    """
    n = n_ech(duree)
    f = _comme_tableau(freq, n)
    return phase0 + 2 * np.pi * np.concatenate(([0.0], np.cumsum(f[:-1]))) / SR


# ---------------------------------------------------------------------------
# Oscillateurs
# ---------------------------------------------------------------------------

def sinus(freq, duree, phase0=0.0):
    """Son pur et doux (une seule fréquence)."""
    return np.sin(phase(freq, duree, phase0))


def _additif(freq, duree, poids, pas=1, attenuation_aigus=None):
    """
    Construit une onde riche en additionnant des sinus multiples de la fréquence.
    On s'arrête avant la fréquence de Nyquist : ainsi aucun repliement (son « sale »).
    `poids(k)` donne le volume de l'harmonique k.
    """
    n = n_ech(duree)
    f = _comme_tableau(freq, n)
    ph = phase(f, duree)
    fmax = max(f.max(), 1.0)
    sortie = np.zeros(n)
    k = 1
    while k * fmax < NYQUIST * 0.95:
        g = poids(k)
        if attenuation_aigus:
            g *= 1.0 / (1.0 + (k * fmax / attenuation_aigus) ** 2)
        sortie += g * np.sin(k * ph)
        k += pas
    return sortie


def carre(freq, duree, douceur=None):
    """Onde carrée (son « 8 bits », nasillard). `douceur` (Hz) adoucit les aigus."""
    return _additif(freq, duree, lambda k: 1.0 / k, pas=2, attenuation_aigus=douceur) * 4 / np.pi


def triangle(freq, duree):
    """Triangle : entre le sinus et le carré, rond mais un peu boisé."""
    return _additif(freq, duree, lambda k: ((-1) ** ((k - 1) // 2)) / (k * k), pas=2) * 8 / np.pi ** 2


def scie(freq, duree, douceur=None):
    """Dents de scie : très riche, base des cordes et cuivres synthétiques."""
    return _additif(freq, duree, lambda k: 1.0 / k, pas=1, attenuation_aigus=douceur) * 2 / np.pi


def bruit(duree, rng):
    """Bruit blanc (« chhh »). `rng` = générateur aléatoire à graine fixe (reproductible)."""
    return rng.uniform(-1.0, 1.0, n_ech(duree))


# ---------------------------------------------------------------------------
# Enveloppes (comment le volume naît, vit et meurt)
# ---------------------------------------------------------------------------

def adsr(duree, a=0.01, d=0.05, s=0.7, r=0.05):
    """
    Enveloppe ADSR classique : Attaque (montée), Décroissance (vers le palier),
    Soutien (niveau du palier), Relâchement (chute finale). Durées en secondes.
    """
    n = n_ech(duree)
    na, nd, nr = n_ech(a), n_ech(d), n_ech(r)
    ns = max(n - na - nd - nr, 0)
    env = np.concatenate([
        np.linspace(0, 1, na, endpoint=False),
        np.linspace(1, s, nd, endpoint=False),
        np.full(ns, s),
        np.linspace(s, 0, nr),
    ])
    return _ajuste(env, n)


def perc(duree, attaque=0.002, chute=0.3):
    """
    Enveloppe percussive : montée très rapide puis chute exponentielle.
    `chute` = temps pour descendre à -60 dB (quasi silence).
    """
    t = temps(duree)
    env = np.exp(-t * 6.9 / max(chute, 1e-4))
    na = max(n_ech(attaque), 1)
    env[:na] *= np.linspace(0, 1, na)
    return env


def _ajuste(x, n):
    """Coupe ou complète avec des zéros pour obtenir exactement n échantillons."""
    if len(x) >= n:
        return x[:n]
    return np.concatenate([x, np.zeros(n - len(x))])


# ---------------------------------------------------------------------------
# Hauteur qui bouge : glissando, vibrato
# ---------------------------------------------------------------------------

def glisse(f0, f1, duree, courbe="exp"):
    """Fréquence qui va de f0 à f1. 'exp' sonne naturel à l'oreille (même vitesse par octave)."""
    x = np.linspace(0.0, 1.0, n_ech(duree))
    if courbe == "exp":
        return f0 * (f1 / f0) ** x
    return f0 + (f1 - f0) * x


def vibrato(freq, duree, vitesse=5.0, demi_tons=0.3):
    """Ajoute une ondulation de hauteur (vitesse en Hz, profondeur en demi-tons)."""
    n = n_ech(duree)
    f = _comme_tableau(freq, n)
    return f * 2.0 ** (demi_tons * np.sin(2 * np.pi * vitesse * temps(duree)) / 12.0)


# ---------------------------------------------------------------------------
# Filtres
# ---------------------------------------------------------------------------

def _biquad(type_, fc, q):
    """Coefficients d'un filtre « biquad » (formules classiques de R. Bristow-Johnson)."""
    w0 = 2 * np.pi * fc / SR
    alpha = np.sin(w0) / (2 * q)
    c = np.cos(w0)
    if type_ == "passe_bas":
        b = [(1 - c) / 2, 1 - c, (1 - c) / 2]
    elif type_ == "passe_haut":
        b = [(1 + c) / 2, -(1 + c), (1 + c) / 2]
    elif type_ == "passe_bande":  # gain de 1 au centre
        b = [alpha, 0.0, -alpha]
    else:
        raise ValueError(type_)
    a = [1 + alpha, -2 * c, 1 - alpha]
    return np.array(b) / a[0], np.array(a) / a[0]


def filtre(x, type_, fc, q=0.707, ordre=1):
    """
    Filtre fixe. Pour aller vite sur de longs signaux, on l'applique « en fréquence »
    (transformée de Fourier) : même résultat qu'un filtre classique, mais calculé d'un bloc.
    `ordre` = nombre de passages (2 = pente deux fois plus raide).
    On ajoute une marge de silence pour que la « traîne » du filtre ne revienne pas au début.
    """
    x = np.asarray(x, dtype=float)
    stereo = x.ndim == 2
    marge = n_ech(0.25)
    n = len(x) + marge
    taille = 1 << int(np.ceil(np.log2(n)))
    b, a = _biquad(type_, fc, q)
    w = np.linspace(0, np.pi, taille // 2 + 1)
    z = np.exp(-1j * w)
    h = (b[0] + b[1] * z + b[2] * z * z) / (a[0] + a[1] * z + a[2] * z * z)
    h = h ** ordre
    if stereo:
        return np.stack([np.fft.irfft(np.fft.rfft(x[:, c], taille) * h, taille)[:len(x)]
                         for c in range(2)], axis=1)
    return np.fft.irfft(np.fft.rfft(x, taille) * h, taille)[:len(x)]


def passe_bas_variable(x, fc, q=0.9):
    """
    Passe-bas dont la fréquence de coupure change dans le temps (tableau `fc`) :
    c'est l'effet « le son s'ouvre » (souffle qui s'éclaircit, « wah » de trombone).
    Filtre à variables d'état (forme « TPT »), calculé échantillon par échantillon :
    plus lent, donc réservé aux sons courts.
    """
    n = len(x)
    fc = _comme_tableau(fc, n)
    g = np.tan(np.pi * np.clip(fc, 20, NYQUIST * 0.9) / SR)
    k = 1.0 / q
    a1 = 1.0 / (1.0 + g * (g + k))
    ic1 = ic2 = 0.0
    y = np.empty(n)
    xs = x.tolist()
    gs = g.tolist()
    a1s = a1.tolist()
    for i in range(n):
        gi, ai = gs[i], a1s[i]
        v3 = xs[i] - ic2
        v1 = ai * ic1 + gi * ai * v3
        v2 = ic2 + gi * v1
        ic1 = 2 * v1 - ic1
        ic2 = 2 * v2 - ic2
        y[i] = v2
    return y


# ---------------------------------------------------------------------------
# Timbres prêts à l'emploi (chaque fonction rend une note complète)
# ---------------------------------------------------------------------------

def boite_a_musique(f, duree=0.9, chute=0.8):
    """Lame de boîte à musique : sinus + harmonique x4 qui meurt en 0,1 s."""
    return (sinus(f, duree) * perc(duree, 0.002, chute)
            + 0.35 * sinus(4 * f, duree) * perc(duree, 0.001, 0.1)
            + 0.08 * sinus(2 * f, duree) * perc(duree, 0.001, 0.3))


def harpe(f, duree=0.6, chute=0.5, eclat=1.0):
    """
    Corde pincée : plusieurs harmoniques, les aiguës s'éteignent plus vite
    (c'est ce qui donne le côté « pincé » puis rond). `eclat` < 1 = plus doux.
    """
    sortie = np.zeros(n_ech(duree))
    for k, g in [(1, 1.0), (2, 0.45), (3, 0.22), (4, 0.10), (5, 0.05)]:
        if k * f < NYQUIST * 0.9:
            sortie += g * (eclat ** (k - 1)) * sinus(k * f, duree) * perc(duree, 0.003, chute / k ** 0.7)
    return sortie


def glockenspiel(f, duree=0.8, chute=0.6):
    """Lame de métal frappée : partiels non entiers (x2,76 et x5,4), typiques du glockenspiel."""
    sortie = sinus(f, duree) * perc(duree, 0.001, chute)
    for r, g, c in [(2.76, 0.35, 0.25), (5.40, 0.15, 0.08)]:
        if r * f < NYQUIST * 0.9:
            sortie += g * sinus(r * f, duree) * perc(duree, 0.001, c)
    return sortie


def clochette(f, duree=0.3, chute=0.25, harmonique=3, poids=0.4):
    """Petite cloche : fondamentale + une harmonique (x3 ou x4) qui s'éteint plus vite."""
    return (sinus(f, duree) * perc(duree, 0.002, chute)
            + poids * sinus(harmonique * f, duree) * perc(duree, 0.001, chute * 0.4))


def ting(f, duree=0.08, chute=0.06):
    """Une pièce qui tinte : sinus aigu + partiel non entier x2,7 (son métallique)."""
    sortie = sinus(f, duree) * perc(duree, 0.0005, chute)
    if 2.7 * f < NYQUIST * 0.9:
        sortie += 0.5 * sinus(2.7 * f, duree) * perc(duree, 0.0005, chute * 0.6)
    return sortie


def orgue(f, duree, a=0.01, r=0.06, s=0.85, coupure=1500.0, tremolo=5.0, profondeur=0.18):
    """
    Orgue de barbarie léger : onde carrée douce, filtrée passe-bas, avec un trémolo
    (le volume ondule doucement, comme les tuyaux d'un orgue de manège).
    """
    t = temps(duree)
    son = carre(f, duree, douceur=coupure) + 0.3 * sinus(2 * f, duree)
    trem = 1.0 - profondeur * (0.5 + 0.5 * np.sin(2 * np.pi * tremolo * t))
    return son * adsr(duree, a, 0.05, s, r) * trem


def cuivre(f, duree, a=0.04, r=0.12, vib=0.12):
    """
    Cuivre de fanfare synthétique : dents de scie dont les aiguës « s'ouvrent » pendant
    l'attaque (le « blat » du cuivre), avec un léger vibrato.
    """
    n = n_ech(duree)
    t = temps(duree)
    freq = vibrato(f, duree, 5.5, vib) if vib else f
    env = adsr(duree, a, 0.08, 0.8, r)
    eclat = np.clip(env * 1.1, 0, 1)  # plus le son est fort, plus il est brillant
    ph = phase(freq, duree)
    sortie = np.zeros(n)
    k = 1
    while k * f < 5000:
        sortie += (1.0 / k) * (eclat ** (0.6 * (k - 1))) * np.sin(k * ph)
        k += 1
    return sortie * env


def scintillement(duree, rng, nombre, f_min=4000, f_max=6000, debut=0.0, fin=None,
                  chute=0.03, volume=1.0):
    """Une pluie de petits « tin » très aigus et très courts placés au hasard (étoiles)."""
    fin = duree if fin is None else fin
    sortie = np.zeros(n_ech(duree))
    for _ in range(nombre):
        t0 = rng.uniform(debut, fin)
        f = rng.uniform(f_min, f_max)
        d = chute * 1.5
        note = sinus(f, d) * perc(d, 0.001, chute) * rng.uniform(0.4, 1.0) * volume
        ajoute(sortie, note, t0)
    return sortie


# ---------------------------------------------------------------------------
# Assemblage
# ---------------------------------------------------------------------------

def silence(duree, stereo=False):
    return np.zeros((n_ech(duree), 2)) if stereo else np.zeros(n_ech(duree))


def ajoute(tampon, son, t0, gain=1.0):
    """Mixe `son` dans `tampon` à partir de l'instant t0 (secondes). Ce qui dépasse est coupé."""
    i = n_ech(t0)
    if i >= len(tampon):
        return tampon
    j = min(len(tampon), i + len(son))
    tampon[i:j] += gain * son[: j - i]
    return tampon


def panoramique(son, p):
    """Place un son mono dans l'espace stéréo : p = -1 (gauche) ... 0 (centre) ... +1 (droite)."""
    angle = (p + 1) * np.pi / 4  # loi « à puissance constante » : le volume perçu ne change pas
    return np.stack([son * np.cos(angle), son * np.sin(angle)], axis=1)


def convolution(a, b):
    """Convolution rapide par transformée de Fourier (utilisée par la réverbération)."""
    n = len(a) + len(b) - 1
    taille = 1 << int(np.ceil(np.log2(n)))
    return np.fft.irfft(np.fft.rfft(a, taille) * np.fft.rfft(b, taille), taille)[:n]


def reponse_reverb(duree=1.2, rng=None, predelai=0.015, brillance=6000.0):
    """
    « Empreinte » d'une salle imaginaire : un bruit qui s'éteint en `duree` secondes,
    adouci dans les aigus. Convoluer un son avec elle lui donne de l'espace.
    """
    rng = rng or np.random.default_rng(1)
    t = temps(duree)
    ir = rng.standard_normal(len(t)) * np.exp(-t * 6.9 / duree)
    ir = filtre(ir, "passe_bas", brillance)
    ir *= np.linspace(1.0, 0.0, len(ir)) ** 0.5  # la fin rejoint zéro en douceur
    ir /= np.sqrt(np.sum(ir ** 2))  # énergie unitaire : on dose ensuite avec `mix`
    return np.concatenate([np.zeros(n_ech(predelai)), ir])


def reverb(x, mix=0.18, duree=1.0, graine=7, brillance=6000.0, stereo_sortie=False):
    """
    Réverbération légère. Rend un signal plus long (la queue de réverbération dépasse).
    - mono -> mono (par défaut) ; avec stereo_sortie=True, deux réverbs différentes
      à gauche et à droite donnent une impression d'espace.
    - stéréo -> stéréo.
    """
    rng = np.random.default_rng(graine)
    ir_g = reponse_reverb(duree, rng, brillance=brillance)
    ir_d = reponse_reverb(duree, rng, brillance=brillance)
    if x.ndim == 1 and not stereo_sortie:
        humide = convolution(x, ir_g)
        sec = _ajuste(x, len(humide))
        return sec + mix * humide
    if x.ndim == 1:
        x = np.stack([x, x], axis=1)
    g = convolution(x[:, 0], ir_g)
    d = convolution(x[:, 1], ir_d)
    sec = np.stack([_ajuste(x[:, 0], len(g)), _ajuste(x[:, 1], len(d))], axis=1)
    return sec + mix * np.stack([g, d], axis=1)


def echo(x, retard=0.12, gain=0.3, repetitions=2):
    """Écho simple : le son se répète, de plus en plus faible."""
    sortie = _ajuste(np.array(x, dtype=float), len(x) + n_ech(retard * repetitions))
    for r in range(1, repetitions + 1):
        ajoute(sortie, x, retard * r, gain ** r)
    return sortie


# ---------------------------------------------------------------------------
# Finitions et export
# ---------------------------------------------------------------------------

def fondus(x, entree=0.005, sortie=0.005):
    """
    Fondu d'entrée et de sortie (5 ms par défaut). Sans eux, un son qui commence ou finit
    brutalement (pas à zéro) produit un « clic » désagréable dans le haut-parleur.
    """
    x = np.array(x, dtype=float)
    ne, ns = n_ech(entree), n_ech(sortie)
    courbe_e = np.sin(np.linspace(0, np.pi / 2, ne)) ** 2
    courbe_s = np.cos(np.linspace(0, np.pi / 2, ns)) ** 2
    if x.ndim == 2:
        courbe_e, courbe_s = courbe_e[:, None], courbe_s[:, None]
    x[:ne] *= courbe_e
    x[len(x) - ns:] *= courbe_s
    return x


def coupe(x, duree):
    """Ramène le signal exactement à la durée voulue."""
    n = n_ech(duree)
    if len(x) >= n:
        return x[:n]
    pad = np.zeros((n - len(x),) + x.shape[1:])
    return np.concatenate([x, pad])


def sans_continu(x):
    """Retire la composante continue (décalage du zéro), invisible mais qui gêne les fondus."""
    return x - np.mean(x, axis=0)


def normalise(x, pic_db=-1.0):
    """Met le point le plus fort exactement à `pic_db` dBFS (-1 dBFS = marge anti-saturation)."""
    pic = np.max(np.abs(x))
    if pic == 0:
        return x
    return x * (10 ** (pic_db / 20.0) / pic)


def volume_court_terme(x, fenetre=0.4):
    """
    Volume « ressenti » approximatif : la plus forte moyenne quadratique (RMS) sur une
    fenêtre glissante de 0,4 s, en dBFS. Le pic seul ne dit pas si un son PARAÎT fort :
    un « tic » très bref peut atteindre -1 dBFS en paraissant discret.
    """
    m = x.mean(axis=1) if x.ndim == 2 else x
    n = max(1, min(len(m), n_ech(fenetre)))
    moyenne = np.convolve(m ** 2, np.ones(n) / n, "valid")
    return 10 * np.log10(max(moyenne.max(), 1e-24))


def finalise(x, duree, entree=0.005, sortie=0.005, pic_db=-1.0, cible_db=None):
    """
    La chaîne de fin commune à tous les effets : durée exacte, fondus, normalisation du
    pic à -1 dBFS. Si `cible_db` est donné et que le son paraît plus fort que cette cible
    (volume court terme), on le baisse jusqu'à elle : son pic est alors sous -1 dBFS.
    Sert à garder des volumes cohérents (piège pas plus fort que trésor, échelle des
    révélations qui monte vraiment).
    """
    x = coupe(sans_continu(x), duree)
    x = fondus(x, entree, sortie)
    x = normalise(x, pic_db)
    if cible_db is not None:
        actuel = volume_court_terme(x)
        if actuel > cible_db:
            x = x * 10 ** ((cible_db - actuel) / 20.0)
    return x


RACINE = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DOSSIER_AUDIO = os.path.join(RACINE, "TourDeLaChance-Unity", "Assets", "Audio")


def ecris_wav(categorie, nom, x):
    """
    Écrit Assets/Audio/<categorie>/<nom>.wav en 44,1 kHz, 16 bits (mono ou stéréo).
    Un tableau à 2 colonnes est écrit en stéréo.
    """
    dossier = os.path.join(DOSSIER_AUDIO, categorie)
    os.makedirs(dossier, exist_ok=True)
    chemin = os.path.join(dossier, nom + ".wav")
    x = np.clip(np.asarray(x, dtype=float), -1.0, 1.0)
    canaux = 2 if x.ndim == 2 else 1
    donnees = np.round(x * 32767).astype("<i2")
    with wave.open(chemin, "wb") as w:
        w.setnchannels(canaux)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(donnees.tobytes())
    print("  écrit :", os.path.relpath(chemin, RACINE))
    return chemin
