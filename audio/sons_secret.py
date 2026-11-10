# -*- coding: utf-8 -*-
"""
Les 5 sons de la cinématique du Secret (catégorie « boite ») — spec docs/specs/v3.md §8.3
et §14. Ils s'enchaînent bout à bout, chacun calé sur la durée de sa phase :

  temps (depuis R)  0,0        0,5          1,3          2,2      2,4                 3,4
  phase             Coupure    Battement    Fissures     Éclat    (revelation_secret) Impact
  son               coupure    battement    fissure      eclat                        confettis
                    0,6 s      0,8 s        0,9 s        0,6 s                        1,5 s

Ambiance : du MYSTÈRE qui finit en FÊTE, jamais de peur. Pas de grave menaçant, pas de
dissonance : les rares notes tenues sont en ré bémol majeur, la tonalité « à part » de
revelation_secret, pour que toute la cinématique sonne comme un seul moment.

Lancer : <python de Blender> audio/sons_secret.py
"""
import numpy as np

from synth import (adsr, ajoute, bruit, ecris_wav, filtre, finalise, glisse, hz, n_ech,
                   passe_bas_variable, perc, reverb, scie, silence, sinus, temps)


def _eclat_de_verre(rng, f, duree=0.12):
    """
    Un petit morceau de verre qui tinte : sinus aigu + deux partiels non entiers qui
    s'éteignent très vite. Tiré au hasard autour de f pour que deux éclats ne se
    ressemblent jamais exactement.
    """
    sortie = sinus(f, duree) * perc(duree, 0.0005, duree * 0.8)
    for r, g in [(2.31 + rng.uniform(-0.05, 0.05), 0.4), (3.87 + rng.uniform(-0.08, 0.08), 0.25)]:
        if r * f < 20000:
            sortie += g * sinus(r * f, duree) * perc(duree, 0.0005, duree * 0.35)
    return sortie


def _craquement(rng, centre, d=0.03):
    """
    Un craquement de cristal : 3 à 6 micro-clics très rapprochés (quelques ms), chacun
    étant un éclat de bruit passé dans un filtre résonant (qui « sonne » comme du verre).
    """
    x = silence(d)
    for _ in range(rng.integers(3, 7)):
        t0 = rng.uniform(0, d * 0.6)
        dc = 0.006
        clic = bruit(dc, rng) * perc(dc, 0.0002, 0.003)
        ajoute(x, clic, t0, rng.uniform(0.5, 1.0))
    return filtre(x, "passe_bande", centre, 6.0) * 4.0


def secret_coupure(rng):
    """
    Panne de courant : « vwoum » qui descend dans le grave puis le silence.
    - un petit « clac » d'interrupteur (bruit sourd très bref) ;
    - une note électrique (dents de scie + octave) qui glisse de 220 Hz à 35 Hz en 0,42 s,
      pendant que son filtre se referme (3 000 -> 120 Hz) : le son « s'éteint » comme une
      machine qui perd sa force. Volume qui retombe à zéro vers 0,48 s ;
    - puis 0,1 s de silence complet (l'écran noir).
    Cartoon : une seule descente, courte, sans grondement qui dure.
    """
    d = 0.6
    x = silence(d)
    dc = 0.03
    clac = filtre(bruit(dc, rng), "passe_bas", 1500, ordre=2) * perc(dc, 0.0005, 0.015)
    ajoute(x, clac, 0.0, 0.9)
    dv = 0.48
    f = glisse(220, 35, dv)
    son = scie(f, dv, douceur=2500) + 0.5 * sinus(2 * f, dv)
    son = passe_bas_variable(son, glisse(3000, 120, dv), q=1.3)
    t = temps(dv)
    env = np.clip(t / 0.01, 0, 1) * (1 - t / dv) ** 1.6
    ajoute(x, son * env, 0.0)
    return finalise(x, d, 0.002, 0.005, cible_oreille=-14.0)


def secret_battement(rng):
    """
    Deux battements de cœur graves et doux, un peu mystérieux, à 0,4 s d'écart (calés sur
    le point rose qui bat 2 fois). Chaque battement est un « poum-poum » (lub-dub) : sinus
    grave qui chute (72 -> 48 Hz) + ses harmoniques 2 et 3 pour qu'on l'entende aussi sur
    un petit haut-parleur, puis un second coup plus doux 120 ms après. Dessous, un voile aigu très
    discret en ré bémol (ré♭6 + la♭6, qui ondule) : le mystère, et le lien avec la suite.
    """
    d = 0.8
    x = silence(d)

    def coup(f0, f1, dd=0.22, chute=0.16):
        f = glisse(f0, f1, dd)
        # fondamentale + harmoniques 2 et 3 : sans elles, un petit haut-parleur (portable,
        # écouteurs bon marché) ne restituerait presque rien d'un son à 60 Hz
        corps = sinus(f, dd) + 0.7 * sinus(2 * f, dd) + 0.3 * sinus(3 * f, dd)
        feutre = filtre(bruit(dd, rng), "passe_bas", 400, ordre=2) * perc(dd, 0.001, 0.03)
        return corps * perc(dd, 0.006, chute) + 0.8 * feutre

    for t0 in (0.0, 0.4):
        ajoute(x, coup(72, 48), t0, 1.0)
        ajoute(x, coup(80, 55, 0.2, 0.13), t0 + 0.12, 0.6)
    t = temps(d)
    voile = (sinus(hz("Db6"), d) + 0.7 * sinus(hz("Ab6") * 1.002, d)) * (0.75 + 0.25 * np.sin(2 * np.pi * 2.5 * t))
    ajoute(x, voile * adsr(d, 0.25, 0.1, 0.8, 0.3), 0.0, 0.025)
    x = reverb(x, 0.1, 0.5, brillance=3000)
    return finalise(x, d, 0.004, 0.005, cible_oreille=-16.0)


def secret_fissure(rng):
    """
    Le cristal se fissure : des craquements de plus en plus serrés (de 6 à 35 par seconde),
    de plus en plus aigus (1 500 -> 7 000 Hz) et de plus en plus forts, avec de petits
    éclats de verre qui tintent ; dessous, une note pure qui monte (400 -> 1 600 Hz) et un
    souffle qui s'éclaircit : la tension grimpe jusqu'à l'éclat. Le son finit « au
    sommet » : secret_eclat prend le relais à 0,9 s.
    """
    d = 0.9
    x = silence(d)
    t0 = 0.02
    while t0 < d - 0.03:
        p = t0 / d  # avancement de 0 à 1
        centre = 1500 * (7000 / 1500) ** p * rng.uniform(0.85, 1.15)
        ajoute(x, _craquement(rng, centre), t0, 0.3 + 0.7 * p ** 1.3)
        if rng.uniform() < 0.25 + 0.4 * p:
            ajoute(x, _eclat_de_verre(rng, rng.uniform(2500, 4500) * (1 + p)), t0 + 0.005,
                   0.08 + 0.15 * p)
        cadence = 6 + 29 * p ** 1.5  # craquements par seconde
        t0 += rng.uniform(0.7, 1.3) / cadence
    t = temps(d)
    tension = sinus(glisse(400, 1600, d), d) * (t / d) ** 2
    souffle = passe_bas_variable(bruit(d, rng), glisse(800, 8000, d), q=1.0) * (t / d) ** 2
    x = x + 0.06 * tension + 0.12 * souffle
    x = reverb(x, 0.1, 0.4, brillance=8000)
    # fondu de sortie de 50 ms (et pas 5) : le son s'arrête « au sommet », pour laisser la
    # place à secret_eclat ; s'il est interrompu ou décalé, il ne finit pas sec.
    return finalise(x, d, 0.003, 0.05, cible_oreille=-15.0)


def secret_eclat(rng):
    """
    La vitre magique vole en éclats + « whoosh », sans violence et très brillant :
    - un éclat d'attaque doux (bruit aigu, attaque 3 ms, aucun grave : pas de « boum ») ;
    - 40 morceaux de verre qui tintent, très denses au début puis de plus en plus rares
      (les morceaux s'envolent) ;
    - un « whoosh » : souffle dont le filtre balaie de 7 000 à 1 200 Hz (ça s'éloigne) ;
    - un accord de verre ré♭6 - fa6 - la♭6 qui fleurit : la lumière arc-en-ciel derrière,
      et le pont vers la fanfare revelation_secret (même tonalité).
    """
    d = 0.6
    x = silence(d)
    da = 0.25
    attaque = filtre(bruit(da, rng), "passe_haut", 2500, ordre=2) * perc(da, 0.003, 0.12)
    ajoute(x, attaque, 0.0, 0.6)
    for _ in range(40):
        t0 = 0.45 * rng.uniform() ** 2.2  # beaucoup au début, puis de moins en moins
        f = rng.uniform(2500, 8000)
        ajoute(x, _eclat_de_verre(rng, f, rng.uniform(0.06, 0.15)), t0, rng.uniform(0.15, 0.4))
    dw = 0.5
    tw = temps(dw)
    whoosh = passe_bas_variable(bruit(dw, rng), glisse(7000, 1200, dw), q=1.6)
    ajoute(x, whoosh * np.sin(np.pi * np.clip(tw / dw, 0, 1)) ** 1.2, 0.02, 0.35)
    for note in ["Db6", "F6", "Ab6"]:
        f = hz(note)
        dn = 0.5
        note_verre = (sinus(f, dn) + 0.3 * sinus(2 * f, dn)) * adsr(dn, 0.04, 0.1, 0.6, 0.3)
        ajoute(x, note_verre, 0.05, 0.12)
    x = reverb(x, 0.2, 0.8, brillance=9000)
    return finalise(x, d, 0.003, 0.005, cible_oreille=-12.0)


def secret_confettis(rng):
    """
    Impact de la carte : petits pétards de fête et crépitements joyeux.
    - 3 « pan ! » de cotillon (bruit filtré + petit sinus qui chute, comme un bouchon) à
      0, 0,1 et 0,28 s, chacun suivi d'un sifflet de papier qui monte ;
    - une pluie de crépitements (micro-clics aigus au hasard), dense au début puis qui se
      raréfie pendant que les confettis retombent ;
    - quelques étincelles très aiguës.
    Tout est aigu et léger : la fête, pas l'explosion.
    """
    d = 1.5
    x = silence(d)
    for t0, g, centre in [(0.0, 1.0, 1600), (0.1, 0.8, 2000), (0.28, 0.7, 1800)]:
        dp = 0.08
        pan = filtre(bruit(dp, rng), "passe_bande", centre, 1.2) * perc(dp, 0.0008, 0.04) * 2.5
        bouchon = sinus(glisse(900, 400, dp), dp) * perc(dp, 0.001, 0.05)
        ajoute(x, pan + 0.4 * bouchon, t0, g)
        ds = 0.15
        sifflet = sinus(glisse(1800, 3600, ds), ds) * np.sin(np.pi * np.linspace(0, 1, n_ech(ds))) ** 2
        ajoute(x, sifflet, t0 + 0.03, 0.06 * g)
    for _ in range(140):
        t0 = 0.05 + 1.3 * rng.uniform() ** 1.8
        dc = 0.012
        clic = filtre(bruit(dc, rng), "passe_bande", rng.uniform(2500, 7000), 3.0) * perc(dc, 0.0003, 0.006)
        ajoute(x, clic, t0, rng.uniform(0.3, 1.0) * (1.4 - t0 / d) * 1.2)
    for _ in range(14):
        t0 = rng.uniform(0.1, 1.1)
        f = rng.uniform(5000, 8000)
        dd = 0.05
        ajoute(x, sinus(f, dd) * perc(dd, 0.001, 0.035), t0, rng.uniform(0.05, 0.12))
    x = reverb(x, 0.12, 0.6, brillance=8000)
    return finalise(x, d, 0.002, 0.005, cible_oreille=-15.0)


def genere():
    # Une graine par son : retoucher l'un ne change pas les autres.
    print("Sons secret (boite) :")
    g = np.random.default_rng
    ecris_wav("boite", "secret_coupure", secret_coupure(g(601)))
    ecris_wav("boite", "secret_battement", secret_battement(g(602)))
    ecris_wav("boite", "secret_fissure", secret_fissure(g(603)))
    ecris_wav("boite", "secret_eclat", secret_eclat(g(604)))
    ecris_wav("boite", "secret_confettis", secret_confettis(g(605)))


if __name__ == "__main__":
    genere()
