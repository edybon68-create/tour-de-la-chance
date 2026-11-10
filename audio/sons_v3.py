# -*- coding: utf-8 -*-
"""
Nouveaux sons de la v3 : interface (améliorations, record) et coffres (coffres précieux,
coffre en plus, éclats par matière, remélange). Spec : docs/specs/v3.md §14.

Comme les autres sons du jeu, tout est en fa majeur (la tonalité de la musique), sauf ce
qui doit « sortir du lot » (les sons du Secret, voir sons_secret.py).

Volumes ressentis visés (mesurés par synth.volume_court_terme, en dB) :
  l'interface reste discrète, les éclats de coffre montent avec la valeur de la matière
  (bronze -17 < argent -15 < or -13,5 < diamant -12,5), comme l'échelle des révélations.

Lancer : <python de Blender> audio/sons_v3.py
"""
import numpy as np

from synth import (adsr, ajoute, bruit, clochette, cuivre, ecris_wav, filtre, finalise,
                   glisse, glockenspiel, harpe, hz, n_ech, orgue, passe_bas_variable, perc,
                   reverb, scintillement, silence, sinus, temps, ting, vibrato)


# --------------------------- petites briques communes ---------------------------

def _montee_3_notes(x, t0=0.0, gain=1.0):
    """
    Le « ding-ding-ding » montant des améliorations : la5 - do6 - fa6, à 80 ms d'écart,
    chaque note plus forte que la précédente. Timbre : clochette (sinus + harmonique x4)
    doublée d'un glockenspiel discret pour le brillant. Mixé dans `x` à partir de t0.
    """
    for i, note in enumerate(["A5", "C6", "F6"]):
        f = hz(note)
        g = gain * (0.65 + 0.175 * i)
        ajoute(x, clochette(f, 0.4, 0.32, harmonique=4, poids=0.3), t0 + 0.08 * i, g)
        ajoute(x, glockenspiel(f, 0.3, 0.2), t0 + 0.08 * i, 0.25 * g)
    return x


def _boum_couvercle(rng, f0=140, f1=90, d=0.18, sourdine=700):
    """Un couvercle de coffre-jouet qui retombe : sinus grave qui chute + bruit étouffé."""
    boum = sinus(glisse(f0, f1, d), d) * perc(d, 0.002, 0.14)
    choc = filtre(bruit(d, rng), "passe_bas", sourdine, ordre=2) * perc(d, 0.001, 0.045)
    return boum + 1.3 * choc


def _pouf(rng, d=0.14):
    """
    Un « pouf » de magicien (nuage de fumée) : souffle dont le filtre s'ouvre puis se
    referme, avec un petit sinus qui descend dessous pour lui donner du corps.
    """
    t = temps(d)
    souffle = passe_bas_variable(bruit(d, rng), 400 + 2600 * np.sin(np.pi * t / d), q=1.2)
    env = np.sin(np.pi * np.clip(t / d, 0, 1)) ** 1.5
    corps = sinus(glisse(320, 180, d), d) * perc(d, 0.004, 0.09)
    return 0.8 * souffle * env + 0.35 * corps


def _verre(f, duree=0.6, chute=0.5):
    """
    Verre de cristal tapé : partiels presque harmoniques (x2,0 ; x4,16), plus « purs » que
    le glockenspiel. (Le Secret a ses propres cloches de verre, x3,01 et x5,43 : on ne les
    réutilise pas, pour qu'il reste unique.)
    """
    sortie = sinus(f, duree) * perc(duree, 0.001, chute)
    for r, g, c in [(2.0, 0.3, 0.5), (4.16, 0.18, 0.2)]:
        if r * f < 20000:
            sortie += g * sinus(r * f, duree) * perc(duree, 0.001, chute * c)
    return sortie


# --------------------------- interface ---------------------------

def amelioration_achat(rng):
    """
    Achat d'un niveau d'amélioration (1 à 4) : « ding-ding-ding » montant de 3 notes,
    puis une petite étincelle qui file vers l'aigu. Message : « tu deviens plus fort ».
    """
    d = 0.6
    x = silence(d)
    _montee_3_notes(x)
    # l'étincelle : un sinus très aigu qui glisse vers le haut + 5 « tin »
    dg = 0.18
    fil = sinus(glisse(2500, 5200, dg), dg) * np.sin(np.pi * np.linspace(0, 1, n_ech(dg))) ** 2
    ajoute(x, fil, 0.2, 0.07)
    x += scintillement(d, rng, 5, 5000, 7500, debut=0.22, fin=0.42, chute=0.03, volume=0.2)
    x = reverb(x, 0.12, 0.5)
    return finalise(x, d, 0.002, 0.005, cible_oreille=-14.0)


def amelioration_max(rng):
    """
    Achat du niveau 5 (le maximum) : la même montée de 3 notes, puis une petite fanfare
    d'orgue de barbarie « ta-ta-taaa » (si bémol - do - fa) qui finit sur un accord de fa
    majeur tenu, couronné d'un glockenspiel et d'étincelles. Même famille que
    resultat_encaisse (orgue de manège), mais plus courte et avec la montée en tête :
    on reconnaît « amélioration », en version triomphe.
    """
    d = 1.2
    x = silence(d)
    _montee_3_notes(x, 0.0, 0.8)
    accords = [
        (["Bb2", "D5", "F5", "Bb5"], 0.26, 0.11),
        (["C3", "E5", "G5", "C6"], 0.38, 0.11),
        (["F3", "A4", "C5", "F5", "A5"], 0.50, 0.55),
    ]
    for notes, t0, duree in accords:
        for note in notes:
            f = hz(note)
            g = 0.5 if f < 200 else 0.28
            ajoute(x, orgue(f, duree, a=0.008, r=0.05 if duree < 0.3 else 0.2,
                            coupure=1800), t0, g)
    for i, note in enumerate(["F6", "A6", "C7"]):
        ajoute(x, glockenspiel(hz(note), 0.45, 0.4), 0.52 + 0.06 * i, 0.25)
    x += scintillement(d, rng, 14, 4500, 7500, debut=0.55, fin=0.95, chute=0.035, volume=0.2)
    x = reverb(x, 0.15, 0.8)
    return finalise(x, d, 0.003, 0.005, cible_oreille=-12.0)


def record(rng):
    """
    Bannière « Nouveau record ! » : petite trompette de fête, « Ta-da ! ». Deux notes de
    cuivre : un do5 bref (« ta »), puis un fa5 tenu avec vibrato (« daaa »), harmonisé en
    accord de fa majeur par deux autres trompettes plus douces ; un coup de cymbale légère
    (bruit aigu) et un glockenspiel qui scintille sur le « da ». Différent du légendaire
    (qui fait un appel de 3 notes) : ici c'est une fierté, pas une rareté.
    """
    d = 1.0
    x = silence(d)
    ajoute(x, cuivre(hz("C5"), 0.11, a=0.012, r=0.03, vib=0), 0.0, 0.55)
    t_da = 0.15
    for note, g in [("F5", 0.55), ("C5", 0.3), ("A4", 0.3), ("F3", 0.3)]:
        ajoute(x, cuivre(hz(note), 0.68, a=0.02, r=0.3, vib=0.18), t_da, g)
    # cymbale légère : bruit très aigu, chute 0,35 s
    dc = 0.5
    cymbale = filtre(bruit(dc, rng), "passe_haut", 6000, ordre=2) * perc(dc, 0.002, 0.35)
    ajoute(x, cymbale, t_da, 0.25)
    for i, note in enumerate(["A5", "C6", "F6"]):
        ajoute(x, glockenspiel(hz(note), 0.5, 0.45), t_da + 0.03 + 0.05 * i, 0.22)
    x += scintillement(d, rng, 12, 4500, 7500, debut=0.25, fin=0.75, chute=0.035, volume=0.2)
    x = reverb(x, 0.14, 0.7)
    return finalise(x, d, 0.003, 0.005, cible_oreille=-12.5)


# --------------------------- coffres ---------------------------

def coffres_precieux(rng):
    """
    Un étage à coffres précieux arrive : scintillement de clochettes, « ooh, c'est joli ».
    Huit petites clochettes aiguës (notes de fa majeur entre fa6 et fa7) en pluie douce sur
    0,3 s, de plus en plus légères, sur un léger souffle aigu. Le code pourra monter la
    hauteur selon le rang (AudioSource.pitch) : le son ne contient pas de grave, il reste
    joli même transposé.
    """
    d = 0.5
    x = silence(d)
    notes = ["C7", "A6", "F7", "C7", "F6", "A6", "C7", "F7"]
    for i, note in enumerate(notes):
        t0 = 0.3 * (i / 7.0) ** 1.2 + rng.uniform(-0.006, 0.006)
        g = (1.0 - 0.06 * i) * rng.uniform(0.75, 1.0)
        ajoute(x, clochette(hz(note), 0.25, 0.2, harmonique=3, poids=0.25), max(t0, 0.0), g * 0.5)
    souffle = filtre(bruit(d, rng), "passe_haut", 5000, ordre=2)
    t = temps(d)
    ajoute(x, souffle * np.sin(np.pi * np.clip(t / 0.4, 0, 1)) ** 2, 0.0, 0.03)
    x = reverb(x, 0.15, 0.5)
    return finalise(x, d, 0.002, 0.005, cible_oreille=-16.0)


def coffre_en_plus(rng):
    """
    Un 4e ou 5e coffre apparaît : « pop ! » magique, une bulle qui éclate gaiement.
    Une bulle qui éclate fait un son qui MONTE très vite (sinus 350 -> 1 400 Hz en 70 ms),
    plus un petit claquement humide ; puis deux clochettes rapprochées (fa6, do7) et
    3 étincelles pour le côté magique.
    """
    d = 0.3
    x = silence(d)
    dp = 0.07
    bulle = sinus(glisse(350, 1400, dp), dp) * perc(dp, 0.001, 0.07)
    claque = filtre(bruit(0.02, rng), "passe_bande", 2500, 2.0) * perc(0.02, 0.0005, 0.008)
    ajoute(x, bulle, 0.0)
    ajoute(x, claque, 0.0, 1.0)
    # la clochette (fa6 puis do7, très rapprochés) porte le côté « magique » et la durée
    ajoute(x, clochette(hz("F6"), 0.24, 0.22, harmonique=3, poids=0.3), 0.04, 0.55)
    ajoute(x, clochette(hz("C7"), 0.2, 0.18, harmonique=3, poids=0.25), 0.075, 0.4)
    x += scintillement(d, rng, 3, 5500, 7500, debut=0.06, fin=0.16, chute=0.025, volume=0.22)
    x = reverb(x, 0.08, 0.3)
    return finalise(x, d, 0.001, 0.005, cible_oreille=-15.5)


def coffre_eclat_bronze(rng):
    """
    Ouverture d'un coffre de bronze : tintement CHAUD et court de métal. Une petite cloche
    plutôt grave (la5, 880 Hz) aux partiels de cloche (x2 ; x2,92 ; x4,1), adoucie dans
    les aigus (passe-bas 4 kHz) : c'est la douceur qui la rend « chaude ».
    """
    d = 0.4
    f = hz("A5")
    x = sinus(f, d) * perc(d, 0.001, 0.42)
    for r, g, c in [(2.0, 0.35, 0.3), (2.92, 0.25, 0.18), (4.1, 0.12, 0.1)]:
        x += g * sinus(r * f, d) * perc(d, 0.001, c)
    x += 0.4 * sinus(f / 2, d) * perc(d, 0.003, 0.3)  # un peu de corps
    x = filtre(x, "passe_bas", 4000)
    x = reverb(x, 0.08, 0.35)
    return finalise(x, d, 0.001, 0.005, cible_oreille=-17.0)


def coffre_eclat_argent(rng):
    """
    Ouverture d'un coffre d'argent : carillon clair à 2 notes (do6 puis fa6, timbre de
    verre pur) + un souffle aigu et léger. Plus brillant et plus haut que le bronze.
    """
    d = 0.6
    x = silence(d)
    ajoute(x, _verre(hz("C6"), 0.5, 0.42), 0.0, 0.8)
    ajoute(x, _verre(hz("F6"), 0.5, 0.45), 0.09, 1.0)
    ds = 0.4
    souffle = passe_bas_variable(filtre(bruit(ds, rng), "passe_haut", 2500), glisse(3000, 9000, ds), q=1.2)
    ajoute(x, souffle * np.sin(np.pi * np.linspace(0, 1, n_ech(ds))) ** 2, 0.03, 0.12)
    x = reverb(x, 0.12, 0.5)
    return finalise(x, d, 0.001, 0.005, cible_oreille=-15.0)


def coffre_eclat_or(rng):
    """
    Ouverture d'un coffre d'or : accord de harpe éclatant (fa majeur égrené de bas en haut
    en 0,1 s, fa4 -> do6) + un glockenspiel fa6 au sommet + scintillement. Riche et chaud.
    """
    d = 0.8
    x = silence(d)
    accord = ["F4", "A4", "C5", "F5", "A5", "C6"]
    for i, note in enumerate(accord):
        ajoute(x, harpe(hz(note), 0.75, 0.65), 0.02 * i, 0.55 + 0.05 * i)
    ajoute(x, glockenspiel(hz("F6"), 0.7, 0.6), 0.12, 0.4)
    x += scintillement(d, rng, 12, 4500, 7500, debut=0.12, fin=0.55, chute=0.035, volume=0.25)
    x = reverb(x, 0.16, 0.7)
    return finalise(x, d, 0.002, 0.005, cible_oreille=-13.5)


def coffre_eclat_diamant(rng):
    """
    Ouverture d'un coffre de diamant, le plus « wow » : le cristal chante.
    1) glissando de verres : 10 notes de cristal qui montent en 0,33 s (gamme pentatonique
       de fa, fa5 -> fa7) ;
    2) éclat brillant au sommet : accord fa6-la6-do7 en verre, bouffée de bruit très aigu
       et gerbe de 25 étincelles ;
    3) un verre qui « chante » (comme un doigt mouillé sur un verre à pied) : fa6 tenu,
       vibrato lent, qui s'éteint doucement.
    """
    d = 1.1
    x = silence(d)
    gamme = ["F5", "G5", "A5", "C6", "D6", "F6", "G6", "A6", "C7", "F7"]
    for i, note in enumerate(gamme):
        ajoute(x, _verre(hz(note), 0.35, 0.3), 0.033 * i, 0.35 + 0.035 * i)
    t_eclat = 0.36
    for note in ["F6", "A6", "C7"]:
        ajoute(x, _verre(hz(note), 0.7, 0.6), t_eclat, 0.45)
    db_ = 0.3
    bouffee = filtre(bruit(db_, rng), "passe_haut", 7000, ordre=2) * perc(db_, 0.002, 0.2)
    ajoute(x, bouffee, t_eclat, 0.25)
    x += scintillement(d, rng, 25, 5000, 9000, debut=t_eclat, fin=0.85, chute=0.035, volume=0.28)
    dc = 0.7
    chant = sinus(vibrato(hz("F6"), dc, 5.0, 0.12), dc) * adsr(dc, 0.08, 0.1, 0.7, 0.45)
    ajoute(x, chant, t_eclat + 0.05, 0.2)
    x = reverb(x, 0.2, 1.0, brillance=8000)
    return finalise(x, d, 0.001, 0.005, cible_oreille=-12.5)


def coffres_remelange(rng):
    """
    Remélange de l'étage 1 : les trois couvercles claquent l'un après l'autre (« clac-clac-
    clac », du plus grave au plus aigu), puis deux « pouf » de magicien rapides : on
    rebat les cartes. Bois et fumée, rien de grave ni d'inquiétant.
    """
    d = 0.5
    x = silence(d)
    for i, (f0, f1) in enumerate([(150, 100), (170, 115), (190, 130)]):
        ajoute(x, _boum_couvercle(rng, f0, f1, 0.15, 900), 0.055 * i, 0.8)
    ajoute(x, _pouf(rng, 0.14), 0.2, 0.9)
    ajoute(x, _pouf(rng, 0.12), 0.32, 0.75)
    x = reverb(x, 0.08, 0.35)
    return finalise(x, d, 0.002, 0.005, cible_oreille=-15.0)


def genere():
    # Une graine par son : ajouter ou retoucher un son ne change pas les autres.
    print("Sons v3 (ui / coffre) :")
    g = np.random.default_rng
    ecris_wav("ui", "amelioration_achat", amelioration_achat(g(501)))
    ecris_wav("ui", "amelioration_max", amelioration_max(g(502)))
    ecris_wav("ui", "record", record(g(503)))
    ecris_wav("coffre", "coffres_precieux", coffres_precieux(g(511)))
    ecris_wav("coffre", "coffre_en_plus", coffre_en_plus(g(512)))
    ecris_wav("coffre", "coffre_eclat_bronze", coffre_eclat_bronze(g(513)))
    ecris_wav("coffre", "coffre_eclat_argent", coffre_eclat_argent(g(514)))
    ecris_wav("coffre", "coffre_eclat_or", coffre_eclat_or(g(515)))
    ecris_wav("coffre", "coffre_eclat_diamant", coffre_eclat_diamant(g(516)))
    ecris_wav("coffre", "coffres_remelange", coffres_remelange(g(517)))


if __name__ == "__main__":
    genere()
