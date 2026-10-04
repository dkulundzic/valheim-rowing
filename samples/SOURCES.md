# Sound sources

Every file in `wav/` is listed below. **All of them are CC0 1.0 (public domain dedication)**: no attribution is legally required, and redistribution inside a mod package is allowed. No CC-BY or other-licence material is used.

Licence text: CC0 1.0 Universal, https://creativecommons.org/publicdomain/zero/1.0/

## Processing

- Every source was decoded with macOS `afconvert -f WAVE -d LEI16@44100 -c 1` (OGG, FLAC, MP3 and WAV all decoded fine; no extra tools installed).
- A Python script (stdlib `wave`/`array` only) then cut clips at silences (grunts) or at the quietest points every 1.2-2.8 s (breathing), added short fades (5 ms/30 ms for grunts, 40 ms/80 ms for breaths), and peak-normalised each clip to -3 dBFS. Output: 16-bit PCM, mono, 44100 Hz.
- `Cut` is the time range in seconds within the original file after conversion to mono 44.1 kHz.
- **Freesound sources are the public HQ MP3 previews** (~128 kbit/s), because original downloads need a logged-in account. The licence is the same; for best quality, log in and download the originals from the listed pages, then re-cut.
- Clips were selected and cut automatically. Nobody has listened to them yet, so audition before use. Some sources are "attack/fight" or "hurt" grunts rather than pure pulling effort.

## Sources (summary)

| Source page | Author | Licence | Original | Clips | Notes |
|---|---|---|---|---|---|
| https://opengameart.org/content/male-gruntyelling-sounds | HaelDB | [CC0 (dual-licensed CC0 / OGA-BY 3.0; CC0 chosen)](https://creativecommons.org/publicdomain/zero/1.0/) |  | 6 |  |
| https://opengameart.org/content/voice-clip-pack-male-adventurer-rpg | wolfwoot | [CC0](https://creativecommons.org/publicdomain/zero/1.0/) |  | 11 |  |
| https://opengameart.org/content/15-vocal-male-strainhurtpainjump-sounds | qubodup | [CC0 (page: "Available under CC0 starting 2024-08-30")](https://creativecommons.org/publicdomain/zero/1.0/) |  | 15 |  |
| https://opengameart.org/content/female-rpg-voice-starter-pack | cicifyre (Cici Fyre) | [CC0 ("Credit appreciated, but not required")](https://creativecommons.org/publicdomain/zero/1.0/) |  | 18 |  |
| https://opengameart.org/content/female-hurt-grunts-groans | Nocturnal_Vanguard (AuraVoice) | [CC0](https://creativecommons.org/publicdomain/zero/1.0/) |  | 7 | Hurt grunts/groans; check each for pain vs effort tone. |
| https://opengameart.org/content/breathing-tired | mikeask | [CC0](https://creativecommons.org/publicdomain/zero/1.0/) |  | 1 | Speaker gender not stated on page ("some person"); filed as male - verify by ear. |
| https://freesound.org/people/egomassive/sounds/536751/ | egomassive | [CC0 1.0 Universal (Creative Commons 0)](https://creativecommons.org/publicdomain/zero/1.0/) | GruntM.ogg | 1 |  |
| https://freesound.org/people/melisandepope/sounds/557134/ | melisandepope | [CC0 1.0 Universal (Creative Commons 0)](https://creativecommons.org/publicdomain/zero/1.0/) | ManlySounds01.wav | 1 |  |
| https://freesound.org/people/jlaclare/sounds/329908/ | jlaclare | [CC0 1.0 Universal (Creative Commons 0)](https://creativecommons.org/publicdomain/zero/1.0/) | Random Grunting | 5 |  |
| https://freesound.org/people/SuddenDice/sounds/558489/ | SuddenDice | [CC0 1.0 Universal (Creative Commons 0)](https://creativecommons.org/publicdomain/zero/1.0/) | AS016231_Short Exertion.flac | 4 |  |
| https://freesound.org/people/SuddenDice/sounds/558544/ | SuddenDice | [CC0 1.0 Universal (Creative Commons 0)](https://creativecommons.org/publicdomain/zero/1.0/) | AS076768_Short Exertion.flac | 2 |  |
| https://freesound.org/people/salemaudio/sounds/803168/ | salemaudio | [CC0 1.0 Universal (Creative Commons 0)](https://creativecommons.org/publicdomain/zero/1.0/) | Soft Male Vocal, Short Pained/Exhertion Groans - Vocal Efforts | 6 |  |
| https://freesound.org/people/craigsmith/sounds/481737/ | craigsmith | [CC0 1.0 Universal (Creative Commons 0)](https://creativecommons.org/publicdomain/zero/1.0/) | R15-34-Man Straining.wav | 5 |  |
| https://freesound.org/people/takareads/sounds/841313/ | takareads | [CC0 1.0 Universal (Creative Commons 0)](https://creativecommons.org/publicdomain/zero/1.0/) | strenuous_grunts | 5 |  |
| https://freesound.org/people/elynch0901/sounds/464485/ | elynch0901 | [CC0 1.0 Universal (Creative Commons 0)](https://creativecommons.org/publicdomain/zero/1.0/) | Male Attack Grunt | 1 |  |
| https://freesound.org/people/ale-batec/sounds/511023/ | ale-batec | [CC0 1.0 Universal (Creative Commons 0)](https://creativecommons.org/publicdomain/zero/1.0/) | Male Fight Grunts | 6 |  |
| https://freesound.org/people/Keskaowl/sounds/507730/ | Keskaowl | [CC0 1.0 Universal (Creative Commons 0)](https://creativecommons.org/publicdomain/zero/1.0/) | EffortNoise.wav | 1 |  |
| https://freesound.org/people/egomassive/sounds/536750/ | egomassive | [CC0 1.0 Universal (Creative Commons 0)](https://creativecommons.org/publicdomain/zero/1.0/) | GruntF.ogg | 1 |  |
| https://freesound.org/people/Reitanna/sounds/242623/ | Reitanna | [CC0 1.0 Universal (Creative Commons 0)](https://creativecommons.org/publicdomain/zero/1.0/) | grunt.wav | 1 |  |
| https://freesound.org/people/Reitanna/sounds/242622/ | Reitanna | [CC0 1.0 Universal (Creative Commons 0)](https://creativecommons.org/publicdomain/zero/1.0/) | grunt2.wav | 1 |  |
| https://freesound.org/people/BranndyBottle/sounds/464673/ | BranndyBottle | [CC0 1.0 Universal (Creative Commons 0)](https://creativecommons.org/publicdomain/zero/1.0/) | FemaleGrunt3.wav | 1 |  |
| https://freesound.org/people/martian/sounds/218908/ | martian | [CC0 1.0 Universal (Creative Commons 0)](https://creativecommons.org/publicdomain/zero/1.0/) | female grunts breaths | 6 |  |
| https://freesound.org/people/petebuchwald/sounds/724211/ | petebuchwald | [CC0 1.0 Universal (Creative Commons 0)](https://creativecommons.org/publicdomain/zero/1.0/) | Female Grunts Breaths Struggle | 6 |  |
| https://freesound.org/people/craigsmith/sounds/482814/ | craigsmith | [CC0 1.0 Universal (Creative Commons 0)](https://creativecommons.org/publicdomain/zero/1.0/) | R21-15-Female Fighting Grunts.wav | 6 |  |
| https://freesound.org/people/Artistic_Dragonborn/sounds/838819/ | Artistic_Dragonborn | [CC0 1.0 Universal (Creative Commons 0)](https://creativecommons.org/publicdomain/zero/1.0/) | Tiny Effort of a Person | 1 |  |
| https://freesound.org/people/hisoul/sounds/520270/ | hisoul | [CC0 1.0 Universal (Creative Commons 0)](https://creativecommons.org/publicdomain/zero/1.0/) | Kali, Attack, Pant Short, Move, Decent_2.wav | 1 |  |
| https://freesound.org/people/myfreesoundaccount1998/sounds/543243/ | myfreesoundaccount1998 | [CC0 1.0 Universal (Creative Commons 0)](https://creativecommons.org/publicdomain/zero/1.0/) | vocals panting.WAV | 2 |  |
| https://freesound.org/people/Artmasterrich/sounds/345464/ | Artmasterrich | [CC0 1.0 Universal (Creative Commons 0)](https://creativecommons.org/publicdomain/zero/1.0/) | Male_Medium_Breathing_01 | 3 |  |
| https://freesound.org/people/njjjjjjjjjjjjjjjjjjjjjjjj/sounds/566400/ | njjjjjjjjjjjjjjjjjjjjjjjj | [CC0 1.0 Universal (Creative Commons 0)](https://creativecommons.org/publicdomain/zero/1.0/) | Panting Male | 4 |  |
| https://freesound.org/people/simone_ds/sounds/366077/ | simone_ds | [CC0 1.0 Universal (Creative Commons 0)](https://creativecommons.org/publicdomain/zero/1.0/) | heavy breathing.wav | 4 |  |
| https://freesound.org/people/vadersfear/sounds/716947/ | vadersfear | [CC0 1.0 Universal (Creative Commons 0)](https://creativecommons.org/publicdomain/zero/1.0/) | Guy_RunAndBreath1 | 2 |  |
| https://freesound.org/people/vadersfear/sounds/716948/ | vadersfear | [CC0 1.0 Universal (Creative Commons 0)](https://creativecommons.org/publicdomain/zero/1.0/) | Guy_RunAndBreath2 | 2 |  |
| https://freesound.org/people/SPAudiobooks/sounds/776564/ | SPAudiobooks | [CC0 1.0 Universal (Creative Commons 0)](https://creativecommons.org/publicdomain/zero/1.0/) | Breathing man with tired puff | 4 |  |
| https://freesound.org/people/darranambler/sounds/321669/ | darranambler | [CC0 1.0 Universal (Creative Commons 0)](https://creativecommons.org/publicdomain/zero/1.0/) | Heavy Breathing Man/Boy.WAV | 4 |  |
| https://freesound.org/people/sherby168/sounds/542078/ | sherby168 | [CC0 1.0 Universal (Creative Commons 0)](https://creativecommons.org/publicdomain/zero/1.0/) | huff and puff | 4 |  |
| https://freesound.org/people/14GPanskaMuzatko_Matej/sounds/419779/ | 14GPanskaMuzatko_Matej | [CC0 1.0 Universal (Creative Commons 0)](https://creativecommons.org/publicdomain/zero/1.0/) | 01 - Exhale male - effort.wav | 1 |  |
| https://freesound.org/people/Kuroseishin/sounds/95567/ | Kuroseishin | [CC0 1.0 Universal (Creative Commons 0)](https://creativecommons.org/publicdomain/zero/1.0/) | breathing_female.wav | 2 |  |
| https://freesound.org/people/MelvinJaepel/sounds/435468/ | MelvinJaepel | [CC0 1.0 Universal (Creative Commons 0)](https://creativecommons.org/publicdomain/zero/1.0/) | woman run and breath.wav | 4 |  |
| https://freesound.org/people/Valerie-Vivegnis/sounds/865356/ | Valerie-Vivegnis | [CC0 1.0 Universal (Creative Commons 0)](https://creativecommons.org/publicdomain/zero/1.0/) | 26.07.26-  Going Up Stairs - Out-of-Breath Female Breathing | 4 |  |
| https://freesound.org/people/OwlStorm/sounds/151215/ | OwlStorm | [CC0 1.0 Universal (Creative Commons 0)](https://creativecommons.org/publicdomain/zero/1.0/) | Female Breathing Heavily (2) | 2 |  |
| https://freesound.org/people/tcrocker68/sounds/235593/ | tcrocker68 | [CC0 1.0 Universal (Creative Commons 0)](https://creativecommons.org/publicdomain/zero/1.0/) | Girl_Heavy_Breathing.wav | 4 |  |
| https://freesound.org/people/tcrocker68/sounds/235586/ | tcrocker68 | [CC0 1.0 Universal (Creative Commons 0)](https://creativecommons.org/publicdomain/zero/1.0/) | Girl_Gasp_Heavy_Breathing.wav | 3 |  |
| https://freesound.org/people/ShangusBurger/sounds/764157/ | ShangusBurger | [CC0 1.0 Universal (Creative Commons 0)](https://creativecommons.org/publicdomain/zero/1.0/) | CRWDBatl_Crowd Grunting, Exerting, Metered, Rowing_ShaneVincent_GSC24_MSDEC-MKH435-Spirit.wav | 1 | Group rowing chant/grunts, 12 s, kept whole (exceeds 3 s on purpose). |

Attribution text: none required for any file (all CC0). Optional courtesy credit line: "Sounds by HaelDB, wolfwoot, qubodup, cicifyre, Nocturnal_Vanguard (AuraVoice), mikeask (OpenGameArt.org) and egomassive, melisandepope, jlaclare, SuddenDice, salemaudio, craigsmith, takareads, elynch0901, ale-batec, Keskaowl, Reitanna, BranndyBottle, martian, petebuchwald, Artistic_Dragonborn, hisoul, myfreesoundaccount1998, Artmasterrich, njjjjjjjjjjjjjjjjjjjjjjjj, simone_ds, vadersfear, SPAudiobooks, darranambler, sherby168, 14GPanskaMuzatko_Matej, Kuroseishin, MelvinJaepel, Valerie-Vivegnis, OwlStorm, tcrocker68, ShangusBurger (Freesound.org), all CC0."

## Male effort grunts (68 files)

| File | Dur (s) | Source page | Author | Licence | Original file | Cut (s) |
|---|---|---|---|---|---|---|
| `male_grunt_oga-haeldb_1.wav` | 0.47 | https://opengameart.org/content/male-gruntyelling-sounds | HaelDB | CC0 1.0 | oga-haeldb-male-grunt-yelling/extracted/yelling sounds/3grunt1.wav | 0.49-0.96 |
| `male_grunt_oga-haeldb_2.wav` | 0.75 | https://opengameart.org/content/male-gruntyelling-sounds | HaelDB | CC0 1.0 | oga-haeldb-male-grunt-yelling/extracted/yelling sounds/3grunt2.wav | 0.25-1.0 |
| `male_grunt_oga-haeldb_3.wav` | 0.5 | https://opengameart.org/content/male-gruntyelling-sounds | HaelDB | CC0 1.0 | oga-haeldb-male-grunt-yelling/extracted/yelling sounds/3grunt3.wav | 0.0-0.5 |
| `male_grunt_oga-haeldb_4.wav` | 0.28 | https://opengameart.org/content/male-gruntyelling-sounds | HaelDB | CC0 1.0 | oga-haeldb-male-grunt-yelling/extracted/yelling sounds/3grunt4.wav | 0.22-0.5 |
| `male_grunt_oga-haeldb_5.wav` | 0.39 | https://opengameart.org/content/male-gruntyelling-sounds | HaelDB | CC0 1.0 | oga-haeldb-male-grunt-yelling/extracted/yelling sounds/3grunt5.wav | 0.11-0.5 |
| `male_grunt_oga-haeldb_6.wav` | 1.13 | https://opengameart.org/content/male-gruntyelling-sounds | HaelDB | CC0 1.0 | oga-haeldb-male-grunt-yelling/extracted/yelling sounds/3grunt6.wav | 0.37-1.5 |
| `male_grunt_oga-wolfwoot_1.wav` | 0.25 | https://opengameart.org/content/voice-clip-pack-male-adventurer-rpg | wolfwoot | CC0 1.0 | oga-wolfwoot-male-adventurer/extracted/RPG Male Adventurer/attack0.wav | 0.0-0.25 |
| `male_grunt_oga-wolfwoot_2.wav` | 0.31 | https://opengameart.org/content/voice-clip-pack-male-adventurer-rpg | wolfwoot | CC0 1.0 | oga-wolfwoot-male-adventurer/extracted/RPG Male Adventurer/attack1.wav | 0.0-0.31 |
| `male_grunt_oga-wolfwoot_3.wav` | 0.27 | https://opengameart.org/content/voice-clip-pack-male-adventurer-rpg | wolfwoot | CC0 1.0 | oga-wolfwoot-male-adventurer/extracted/RPG Male Adventurer/attack2.wav | 0.0-0.27 |
| `male_grunt_oga-wolfwoot_4.wav` | 0.46 | https://opengameart.org/content/voice-clip-pack-male-adventurer-rpg | wolfwoot | CC0 1.0 | oga-wolfwoot-male-adventurer/extracted/RPG Male Adventurer/attack3.wav | 0.0-0.46 |
| `male_grunt_oga-wolfwoot_5.wav` | 0.24 | https://opengameart.org/content/voice-clip-pack-male-adventurer-rpg | wolfwoot | CC0 1.0 | oga-wolfwoot-male-adventurer/extracted/RPG Male Adventurer/attack4.wav | 0.0-0.24 |
| `male_grunt_oga-wolfwoot_6.wav` | 0.5 | https://opengameart.org/content/voice-clip-pack-male-adventurer-rpg | wolfwoot | CC0 1.0 | oga-wolfwoot-male-adventurer/extracted/RPG Male Adventurer/attack5.wav | 0.0-0.5 |
| `male_grunt_oga-wolfwoot_7.wav` | 0.67 | https://opengameart.org/content/voice-clip-pack-male-adventurer-rpg | wolfwoot | CC0 1.0 | oga-wolfwoot-male-adventurer/extracted/RPG Male Adventurer/attack6.wav | 0.0-0.67 |
| `male_grunt_oga-wolfwoot_8.wav` | 0.4 | https://opengameart.org/content/voice-clip-pack-male-adventurer-rpg | wolfwoot | CC0 1.0 | oga-wolfwoot-male-adventurer/extracted/RPG Male Adventurer/attack7.wav | 0.04-0.44 |
| `male_grunt_oga-wolfwoot_9.wav` | 0.35 | https://opengameart.org/content/voice-clip-pack-male-adventurer-rpg | wolfwoot | CC0 1.0 | oga-wolfwoot-male-adventurer/extracted/RPG Male Adventurer/attack8.wav | 0.0-0.35 |
| `male_grunt_oga-wolfwoot_10.wav` | 0.28 | https://opengameart.org/content/voice-clip-pack-male-adventurer-rpg | wolfwoot | CC0 1.0 | oga-wolfwoot-male-adventurer/extracted/RPG Male Adventurer/jump0.wav | 0.0-0.28 |
| `male_grunt_oga-wolfwoot_11.wav` | 0.26 | https://opengameart.org/content/voice-clip-pack-male-adventurer-rpg | wolfwoot | CC0 1.0 | oga-wolfwoot-male-adventurer/extracted/RPG Male Adventurer/jump1.wav | 0.0-0.26 |
| `male_grunt_oga-qubodup_1.wav` | 0.41 | https://opengameart.org/content/15-vocal-male-strainhurtpainjump-sounds | qubodup | CC0 1.0 | oga-qubodup-male-strain/extracted/slightscream-01.flac | 0.03-0.44 |
| `male_grunt_oga-qubodup_2.wav` | 0.34 | https://opengameart.org/content/15-vocal-male-strainhurtpainjump-sounds | qubodup | CC0 1.0 | oga-qubodup-male-strain/extracted/slightscream-02.flac | 0.02-0.36 |
| `male_grunt_oga-qubodup_3.wav` | 0.49 | https://opengameart.org/content/15-vocal-male-strainhurtpainjump-sounds | qubodup | CC0 1.0 | oga-qubodup-male-strain/extracted/slightscream-03.flac | 0.04-0.53 |
| `male_grunt_oga-qubodup_4.wav` | 0.52 | https://opengameart.org/content/15-vocal-male-strainhurtpainjump-sounds | qubodup | CC0 1.0 | oga-qubodup-male-strain/extracted/slightscream-04.flac | 0.01-0.53 |
| `male_grunt_oga-qubodup_5.wav` | 0.62 | https://opengameart.org/content/15-vocal-male-strainhurtpainjump-sounds | qubodup | CC0 1.0 | oga-qubodup-male-strain/extracted/slightscream-05.flac | 0.01-0.63 |
| `male_grunt_oga-qubodup_6.wav` | 0.48 | https://opengameart.org/content/15-vocal-male-strainhurtpainjump-sounds | qubodup | CC0 1.0 | oga-qubodup-male-strain/extracted/slightscream-06.flac | 0.03-0.51 |
| `male_grunt_oga-qubodup_7.wav` | 0.47 | https://opengameart.org/content/15-vocal-male-strainhurtpainjump-sounds | qubodup | CC0 1.0 | oga-qubodup-male-strain/extracted/slightscream-07.flac | 0.01-0.48 |
| `male_grunt_oga-qubodup_8.wav` | 0.4 | https://opengameart.org/content/15-vocal-male-strainhurtpainjump-sounds | qubodup | CC0 1.0 | oga-qubodup-male-strain/extracted/slightscream-08.flac | 0.01-0.41 |
| `male_grunt_oga-qubodup_9.wav` | 0.53 | https://opengameart.org/content/15-vocal-male-strainhurtpainjump-sounds | qubodup | CC0 1.0 | oga-qubodup-male-strain/extracted/slightscream-09.flac | 0.0-0.53 |
| `male_grunt_oga-qubodup_10.wav` | 0.68 | https://opengameart.org/content/15-vocal-male-strainhurtpainjump-sounds | qubodup | CC0 1.0 | oga-qubodup-male-strain/extracted/slightscream-10.flac | 0.03-0.71 |
| `male_grunt_oga-qubodup_11.wav` | 0.33 | https://opengameart.org/content/15-vocal-male-strainhurtpainjump-sounds | qubodup | CC0 1.0 | oga-qubodup-male-strain/extracted/slightscream-11.flac | 0.0-0.33 |
| `male_grunt_oga-qubodup_12.wav` | 0.3 | https://opengameart.org/content/15-vocal-male-strainhurtpainjump-sounds | qubodup | CC0 1.0 | oga-qubodup-male-strain/extracted/slightscream-12.flac | 0.08-0.38 |
| `male_grunt_oga-qubodup_13.wav` | 0.51 | https://opengameart.org/content/15-vocal-male-strainhurtpainjump-sounds | qubodup | CC0 1.0 | oga-qubodup-male-strain/extracted/slightscream-13.flac | 0.03-0.54 |
| `male_grunt_oga-qubodup_14.wav` | 0.45 | https://opengameart.org/content/15-vocal-male-strainhurtpainjump-sounds | qubodup | CC0 1.0 | oga-qubodup-male-strain/extracted/slightscream-14.flac | 0.1-0.55 |
| `male_grunt_oga-qubodup_15.wav` | 0.71 | https://opengameart.org/content/15-vocal-male-strainhurtpainjump-sounds | qubodup | CC0 1.0 | oga-qubodup-male-strain/extracted/slightscream-15.flac | 0.02-0.73 |
| `male_grunt_fs-egomassive-536751_1.wav` | 1.1 | https://freesound.org/people/egomassive/sounds/536751/ | egomassive | CC0 1.0 | freesound-previews/536751_egomassive.mp3 (Freesound title: "GruntM.ogg") | 0.02-1.12 |
| `male_grunt_fs-melisandepope-557134_1.wav` | 0.56 | https://freesound.org/people/melisandepope/sounds/557134/ | melisandepope | CC0 1.0 | freesound-previews/557134_melisandepope.mp3 (Freesound title: "ManlySounds01.wav") | 0.02-0.58 |
| `male_grunt_fs-jlaclare-329908_1.wav` | 0.94 | https://freesound.org/people/jlaclare/sounds/329908/ | jlaclare | CC0 1.0 | freesound-previews/329908_jlaclare.mp3 (Freesound title: "Random Grunting") | 0.55-1.49 |
| `male_grunt_fs-jlaclare-329908_2.wav` | 0.57 | https://freesound.org/people/jlaclare/sounds/329908/ | jlaclare | CC0 1.0 | freesound-previews/329908_jlaclare.mp3 (Freesound title: "Random Grunting") | 1.5-2.07 |
| `male_grunt_fs-jlaclare-329908_3.wav` | 0.54 | https://freesound.org/people/jlaclare/sounds/329908/ | jlaclare | CC0 1.0 | freesound-previews/329908_jlaclare.mp3 (Freesound title: "Random Grunting") | 2.09-2.63 |
| `male_grunt_fs-jlaclare-329908_4.wav` | 1.33 | https://freesound.org/people/jlaclare/sounds/329908/ | jlaclare | CC0 1.0 | freesound-previews/329908_jlaclare.mp3 (Freesound title: "Random Grunting") | 2.68-4.01 |
| `male_grunt_fs-jlaclare-329908_5.wav` | 1.45 | https://freesound.org/people/jlaclare/sounds/329908/ | jlaclare | CC0 1.0 | freesound-previews/329908_jlaclare.mp3 (Freesound title: "Random Grunting") | 4.11-5.56 |
| `male_grunt_fs-suddendice-558489_1.wav` | 0.4 | https://freesound.org/people/SuddenDice/sounds/558489/ | SuddenDice | CC0 1.0 | freesound-previews/558489_SuddenDice.mp3 (Freesound title: "AS016231_Short Exertion.flac") | 0.13-0.53 |
| `male_grunt_fs-suddendice-558489_2.wav` | 0.28 | https://freesound.org/people/SuddenDice/sounds/558489/ | SuddenDice | CC0 1.0 | freesound-previews/558489_SuddenDice.mp3 (Freesound title: "AS016231_Short Exertion.flac") | 1.72-2.0 |
| `male_grunt_fs-suddendice-558489_3.wav` | 0.4 | https://freesound.org/people/SuddenDice/sounds/558489/ | SuddenDice | CC0 1.0 | freesound-previews/558489_SuddenDice.mp3 (Freesound title: "AS016231_Short Exertion.flac") | 2.2-2.6 |
| `male_grunt_fs-suddendice-558489_4.wav` | 0.8 | https://freesound.org/people/SuddenDice/sounds/558489/ | SuddenDice | CC0 1.0 | freesound-previews/558489_SuddenDice.mp3 (Freesound title: "AS016231_Short Exertion.flac") | 3.77-4.57 |
| `male_grunt_fs-suddendice-558544_1.wav` | 1.1 | https://freesound.org/people/SuddenDice/sounds/558544/ | SuddenDice | CC0 1.0 | freesound-previews/558544_SuddenDice.mp3 (Freesound title: "AS076768_Short Exertion.flac") | 0.24-1.34 |
| `male_grunt_fs-suddendice-558544_2.wav` | 0.73 | https://freesound.org/people/SuddenDice/sounds/558544/ | SuddenDice | CC0 1.0 | freesound-previews/558544_SuddenDice.mp3 (Freesound title: "AS076768_Short Exertion.flac") | 1.95-2.68 |
| `male_grunt_fs-salemaudio-803168_1.wav` | 0.43 | https://freesound.org/people/salemaudio/sounds/803168/ | salemaudio | CC0 1.0 | freesound-previews/803168_salemaudio.mp3 (Freesound title: "Soft Male Vocal, Short Pained/Exhertion Groans - Vocal Efforts") | 0.11-0.54 |
| `male_grunt_fs-salemaudio-803168_2.wav` | 0.39 | https://freesound.org/people/salemaudio/sounds/803168/ | salemaudio | CC0 1.0 | freesound-previews/803168_salemaudio.mp3 (Freesound title: "Soft Male Vocal, Short Pained/Exhertion Groans - Vocal Efforts") | 1.61-2.0 |
| `male_grunt_fs-salemaudio-803168_3.wav` | 0.42 | https://freesound.org/people/salemaudio/sounds/803168/ | salemaudio | CC0 1.0 | freesound-previews/803168_salemaudio.mp3 (Freesound title: "Soft Male Vocal, Short Pained/Exhertion Groans - Vocal Efforts") | 4.21-4.63 |
| `male_grunt_fs-salemaudio-803168_4.wav` | 0.41 | https://freesound.org/people/salemaudio/sounds/803168/ | salemaudio | CC0 1.0 | freesound-previews/803168_salemaudio.mp3 (Freesound title: "Soft Male Vocal, Short Pained/Exhertion Groans - Vocal Efforts") | 7.3-7.71 |
| `male_grunt_fs-salemaudio-803168_5.wav` | 0.38 | https://freesound.org/people/salemaudio/sounds/803168/ | salemaudio | CC0 1.0 | freesound-previews/803168_salemaudio.mp3 (Freesound title: "Soft Male Vocal, Short Pained/Exhertion Groans - Vocal Efforts") | 8.68-9.06 |
| `male_grunt_fs-salemaudio-803168_6.wav` | 0.57 | https://freesound.org/people/salemaudio/sounds/803168/ | salemaudio | CC0 1.0 | freesound-previews/803168_salemaudio.mp3 (Freesound title: "Soft Male Vocal, Short Pained/Exhertion Groans - Vocal Efforts") | 11.59-12.16 |
| `male_grunt_fs-craigsmith-481737_1.wav` | 0.84 | https://freesound.org/people/craigsmith/sounds/481737/ | craigsmith | CC0 1.0 | freesound-previews/481737_craigsmith.mp3 (Freesound title: "R15-34-Man Straining.wav") | 0.06-0.9 |
| `male_grunt_fs-craigsmith-481737_2.wav` | 0.27 | https://freesound.org/people/craigsmith/sounds/481737/ | craigsmith | CC0 1.0 | freesound-previews/481737_craigsmith.mp3 (Freesound title: "R15-34-Man Straining.wav") | 1.32-1.59 |
| `male_grunt_fs-craigsmith-481737_3.wav` | 0.8 | https://freesound.org/people/craigsmith/sounds/481737/ | craigsmith | CC0 1.0 | freesound-previews/481737_craigsmith.mp3 (Freesound title: "R15-34-Man Straining.wav") | 5.68-6.48 |
| `male_grunt_fs-craigsmith-481737_4.wav` | 0.86 | https://freesound.org/people/craigsmith/sounds/481737/ | craigsmith | CC0 1.0 | freesound-previews/481737_craigsmith.mp3 (Freesound title: "R15-34-Man Straining.wav") | 8.27-9.13 |
| `male_grunt_fs-craigsmith-481737_5.wav` | 0.93 | https://freesound.org/people/craigsmith/sounds/481737/ | craigsmith | CC0 1.0 | freesound-previews/481737_craigsmith.mp3 (Freesound title: "R15-34-Man Straining.wav") | 11.36-12.29 |
| `male_grunt_fs-takareads-841313_1.wav` | 0.33 | https://freesound.org/people/takareads/sounds/841313/ | takareads | CC0 1.0 | freesound-previews/841313_takareads.mp3 (Freesound title: "strenuous_grunts") | 2.82-3.15 |
| `male_grunt_fs-takareads-841313_2.wav` | 0.33 | https://freesound.org/people/takareads/sounds/841313/ | takareads | CC0 1.0 | freesound-previews/841313_takareads.mp3 (Freesound title: "strenuous_grunts") | 4.47-4.8 |
| `male_grunt_fs-takareads-841313_3.wav` | 0.36 | https://freesound.org/people/takareads/sounds/841313/ | takareads | CC0 1.0 | freesound-previews/841313_takareads.mp3 (Freesound title: "strenuous_grunts") | 6.24-6.6 |
| `male_grunt_fs-takareads-841313_4.wav` | 0.41 | https://freesound.org/people/takareads/sounds/841313/ | takareads | CC0 1.0 | freesound-previews/841313_takareads.mp3 (Freesound title: "strenuous_grunts") | 7.93-8.34 |
| `male_grunt_fs-takareads-841313_5.wav` | 0.38 | https://freesound.org/people/takareads/sounds/841313/ | takareads | CC0 1.0 | freesound-previews/841313_takareads.mp3 (Freesound title: "strenuous_grunts") | 9.67-10.05 |
| `male_grunt_fs-elynch0901-464485_1.wav` | 0.55 | https://freesound.org/people/elynch0901/sounds/464485/ | elynch0901 | CC0 1.0 | freesound-previews/464485_elynch0901.mp3 (Freesound title: "Male Attack Grunt") | 0.15-0.7 |
| `male_grunt_fs-ale-batec-511023_1.wav` | 0.34 | https://freesound.org/people/ale-batec/sounds/511023/ | ale-batec | CC0 1.0 | freesound-previews/511023_ale-batec.mp3 (Freesound title: "Male Fight Grunts") | 0.5-0.84 |
| `male_grunt_fs-ale-batec-511023_2.wav` | 0.39 | https://freesound.org/people/ale-batec/sounds/511023/ | ale-batec | CC0 1.0 | freesound-previews/511023_ale-batec.mp3 (Freesound title: "Male Fight Grunts") | 5.33-5.72 |
| `male_grunt_fs-ale-batec-511023_3.wav` | 0.35 | https://freesound.org/people/ale-batec/sounds/511023/ | ale-batec | CC0 1.0 | freesound-previews/511023_ale-batec.mp3 (Freesound title: "Male Fight Grunts") | 11.46-11.81 |
| `male_grunt_fs-ale-batec-511023_4.wav` | 0.36 | https://freesound.org/people/ale-batec/sounds/511023/ | ale-batec | CC0 1.0 | freesound-previews/511023_ale-batec.mp3 (Freesound title: "Male Fight Grunts") | 18.24-18.6 |
| `male_grunt_fs-ale-batec-511023_5.wav` | 0.34 | https://freesound.org/people/ale-batec/sounds/511023/ | ale-batec | CC0 1.0 | freesound-previews/511023_ale-batec.mp3 (Freesound title: "Male Fight Grunts") | 22.25-22.59 |
| `male_grunt_fs-ale-batec-511023_6.wav` | 0.6 | https://freesound.org/people/ale-batec/sounds/511023/ | ale-batec | CC0 1.0 | freesound-previews/511023_ale-batec.mp3 (Freesound title: "Male Fight Grunts") | 26.27-26.87 |

## Female effort grunts (50 files)

| File | Dur (s) | Source page | Author | Licence | Original file | Cut (s) |
|---|---|---|---|---|---|---|
| `female_grunt_oga-cicifyre-type1_1.wav` | 0.31 | https://opengameart.org/content/female-rpg-voice-starter-pack | cicifyre (Cici Fyre) | CC0 1.0 | oga-cicifyre-female-rpg-voice-starter/extracted/RPG Voice Starter Pack/Type 1/attack1.wav | 0.0-0.31 |
| `female_grunt_oga-cicifyre-type1_2.wav` | 0.39 | https://opengameart.org/content/female-rpg-voice-starter-pack | cicifyre (Cici Fyre) | CC0 1.0 | oga-cicifyre-female-rpg-voice-starter/extracted/RPG Voice Starter Pack/Type 1/attack2.wav | 0.0-0.39 |
| `female_grunt_oga-cicifyre-type1_3.wav` | 0.35 | https://opengameart.org/content/female-rpg-voice-starter-pack | cicifyre (Cici Fyre) | CC0 1.0 | oga-cicifyre-female-rpg-voice-starter/extracted/RPG Voice Starter Pack/Type 1/attack3.wav | 0.0-0.35 |
| `female_grunt_oga-cicifyre-type1_4.wav` | 0.32 | https://opengameart.org/content/female-rpg-voice-starter-pack | cicifyre (Cici Fyre) | CC0 1.0 | oga-cicifyre-female-rpg-voice-starter/extracted/RPG Voice Starter Pack/Type 1/jump1.wav | 0.0-0.32 |
| `female_grunt_oga-cicifyre-type1_5.wav` | 0.29 | https://opengameart.org/content/female-rpg-voice-starter-pack | cicifyre (Cici Fyre) | CC0 1.0 | oga-cicifyre-female-rpg-voice-starter/extracted/RPG Voice Starter Pack/Type 1/jump2.wav | 0.0-0.29 |
| `female_grunt_oga-cicifyre-type1_6.wav` | 0.29 | https://opengameart.org/content/female-rpg-voice-starter-pack | cicifyre (Cici Fyre) | CC0 1.0 | oga-cicifyre-female-rpg-voice-starter/extracted/RPG Voice Starter Pack/Type 1/jump3.wav | 0.0-0.29 |
| `female_grunt_oga-cicifyre-type2_1.wav` | 0.37 | https://opengameart.org/content/female-rpg-voice-starter-pack | cicifyre (Cici Fyre) | CC0 1.0 | oga-cicifyre-female-rpg-voice-starter/extracted/RPG Voice Starter Pack/Type 2/attack1.wav | 0.0-0.37 |
| `female_grunt_oga-cicifyre-type2_2.wav` | 0.39 | https://opengameart.org/content/female-rpg-voice-starter-pack | cicifyre (Cici Fyre) | CC0 1.0 | oga-cicifyre-female-rpg-voice-starter/extracted/RPG Voice Starter Pack/Type 2/attack2.wav | 0.0-0.39 |
| `female_grunt_oga-cicifyre-type2_3.wav` | 0.33 | https://opengameart.org/content/female-rpg-voice-starter-pack | cicifyre (Cici Fyre) | CC0 1.0 | oga-cicifyre-female-rpg-voice-starter/extracted/RPG Voice Starter Pack/Type 2/attack3.wav | 0.0-0.33 |
| `female_grunt_oga-cicifyre-type2_4.wav` | 0.22 | https://opengameart.org/content/female-rpg-voice-starter-pack | cicifyre (Cici Fyre) | CC0 1.0 | oga-cicifyre-female-rpg-voice-starter/extracted/RPG Voice Starter Pack/Type 2/jump1.wav | 0.0-0.22 |
| `female_grunt_oga-cicifyre-type2_5.wav` | 0.22 | https://opengameart.org/content/female-rpg-voice-starter-pack | cicifyre (Cici Fyre) | CC0 1.0 | oga-cicifyre-female-rpg-voice-starter/extracted/RPG Voice Starter Pack/Type 2/jump2.wav | 0.0-0.22 |
| `female_grunt_oga-cicifyre-type2_6.wav` | 0.22 | https://opengameart.org/content/female-rpg-voice-starter-pack | cicifyre (Cici Fyre) | CC0 1.0 | oga-cicifyre-female-rpg-voice-starter/extracted/RPG Voice Starter Pack/Type 2/jump3.wav | 0.0-0.22 |
| `female_grunt_oga-cicifyre-type3_1.wav` | 0.29 | https://opengameart.org/content/female-rpg-voice-starter-pack | cicifyre (Cici Fyre) | CC0 1.0 | oga-cicifyre-female-rpg-voice-starter/extracted/RPG Voice Starter Pack/Type 3/attack1.wav | 0.0-0.29 |
| `female_grunt_oga-cicifyre-type3_2.wav` | 0.35 | https://opengameart.org/content/female-rpg-voice-starter-pack | cicifyre (Cici Fyre) | CC0 1.0 | oga-cicifyre-female-rpg-voice-starter/extracted/RPG Voice Starter Pack/Type 3/attack2.wav | 0.0-0.35 |
| `female_grunt_oga-cicifyre-type3_3.wav` | 0.39 | https://opengameart.org/content/female-rpg-voice-starter-pack | cicifyre (Cici Fyre) | CC0 1.0 | oga-cicifyre-female-rpg-voice-starter/extracted/RPG Voice Starter Pack/Type 3/attack3.wav | 0.0-0.39 |
| `female_grunt_oga-cicifyre-type3_4.wav` | 0.29 | https://opengameart.org/content/female-rpg-voice-starter-pack | cicifyre (Cici Fyre) | CC0 1.0 | oga-cicifyre-female-rpg-voice-starter/extracted/RPG Voice Starter Pack/Type 3/jump1.wav | 0.0-0.29 |
| `female_grunt_oga-cicifyre-type3_5.wav` | 0.29 | https://opengameart.org/content/female-rpg-voice-starter-pack | cicifyre (Cici Fyre) | CC0 1.0 | oga-cicifyre-female-rpg-voice-starter/extracted/RPG Voice Starter Pack/Type 3/jump2.wav | 0.0-0.29 |
| `female_grunt_oga-cicifyre-type3_6.wav` | 0.32 | https://opengameart.org/content/female-rpg-voice-starter-pack | cicifyre (Cici Fyre) | CC0 1.0 | oga-cicifyre-female-rpg-voice-starter/extracted/RPG Voice Starter Pack/Type 3/jump3.wav | 0.0-0.32 |
| `female_grunt_oga-auravoice_1.wav` | 0.89 | https://opengameart.org/content/female-hurt-grunts-groans | Nocturnal_Vanguard (AuraVoice) | CC0 1.0 | oga-auravoice-female-hurt-grunts/female_hurt_grunts_groans_1.ogg | 0.32-1.21 |
| `female_grunt_oga-auravoice_2.wav` | 0.72 | https://opengameart.org/content/female-hurt-grunts-groans | Nocturnal_Vanguard (AuraVoice) | CC0 1.0 | oga-auravoice-female-hurt-grunts/female_hurt_grunts_groans_1.ogg | 2.06-2.78 |
| `female_grunt_oga-auravoice_3.wav` | 0.52 | https://opengameart.org/content/female-hurt-grunts-groans | Nocturnal_Vanguard (AuraVoice) | CC0 1.0 | oga-auravoice-female-hurt-grunts/female_hurt_grunts_groans_1.ogg | 3.68-4.2 |
| `female_grunt_oga-auravoice_4.wav` | 1.3 | https://opengameart.org/content/female-hurt-grunts-groans | Nocturnal_Vanguard (AuraVoice) | CC0 1.0 | oga-auravoice-female-hurt-grunts/female_hurt_grunts_groans_1.ogg | 4.88-6.18 |
| `female_grunt_oga-auravoice_5.wav` | 0.88 | https://opengameart.org/content/female-hurt-grunts-groans | Nocturnal_Vanguard (AuraVoice) | CC0 1.0 | oga-auravoice-female-hurt-grunts/female_hurt_grunts_groans_1.ogg | 7.3-8.18 |
| `female_grunt_oga-auravoice_6.wav` | 1.01 | https://opengameart.org/content/female-hurt-grunts-groans | Nocturnal_Vanguard (AuraVoice) | CC0 1.0 | oga-auravoice-female-hurt-grunts/female_hurt_grunts_groans_1.ogg | 9.35-10.36 |
| `female_grunt_oga-auravoice_7.wav` | 0.98 | https://opengameart.org/content/female-hurt-grunts-groans | Nocturnal_Vanguard (AuraVoice) | CC0 1.0 | oga-auravoice-female-hurt-grunts/female_hurt_grunts_groans_1.ogg | 11.65-12.63 |
| `female_grunt_fs-keskaowl-507730_1.wav` | 0.32 | https://freesound.org/people/Keskaowl/sounds/507730/ | Keskaowl | CC0 1.0 | freesound-previews/507730_Keskaowl.mp3 (Freesound title: "EffortNoise.wav") | 0.0-0.32 |
| `female_grunt_fs-egomassive-536750_1.wav` | 0.68 | https://freesound.org/people/egomassive/sounds/536750/ | egomassive | CC0 1.0 | freesound-previews/536750_egomassive.mp3 (Freesound title: "GruntF.ogg") | 0.33-1.01 |
| `female_grunt_fs-reitanna-242623_1.wav` | 0.4 | https://freesound.org/people/Reitanna/sounds/242623/ | Reitanna | CC0 1.0 | freesound-previews/242623_Reitanna.mp3 (Freesound title: "grunt.wav") | 0.0-0.4 |
| `female_grunt_fs-reitanna-242622_1.wav` | 0.38 | https://freesound.org/people/Reitanna/sounds/242622/ | Reitanna | CC0 1.0 | freesound-previews/242622_Reitanna.mp3 (Freesound title: "grunt2.wav") | 0.04-0.42 |
| `female_grunt_fs-branndybottle-464673_1.wav` | 0.35 | https://freesound.org/people/BranndyBottle/sounds/464673/ | BranndyBottle | CC0 1.0 | freesound-previews/464673_BranndyBottle.mp3 (Freesound title: "FemaleGrunt3.wav") | 0.0-0.35 |
| `female_grunt_fs-martian-218908_1.wav` | 0.29 | https://freesound.org/people/martian/sounds/218908/ | martian | CC0 1.0 | freesound-previews/218908_martian.mp3 (Freesound title: "female grunts breaths") | 1.25-1.54 |
| `female_grunt_fs-martian-218908_2.wav` | 0.3 | https://freesound.org/people/martian/sounds/218908/ | martian | CC0 1.0 | freesound-previews/218908_martian.mp3 (Freesound title: "female grunts breaths") | 2.07-2.37 |
| `female_grunt_fs-martian-218908_3.wav` | 0.26 | https://freesound.org/people/martian/sounds/218908/ | martian | CC0 1.0 | freesound-previews/218908_martian.mp3 (Freesound title: "female grunts breaths") | 4.2-4.46 |
| `female_grunt_fs-martian-218908_4.wav` | 0.45 | https://freesound.org/people/martian/sounds/218908/ | martian | CC0 1.0 | freesound-previews/218908_martian.mp3 (Freesound title: "female grunts breaths") | 4.84-5.29 |
| `female_grunt_fs-martian-218908_5.wav` | 0.34 | https://freesound.org/people/martian/sounds/218908/ | martian | CC0 1.0 | freesound-previews/218908_martian.mp3 (Freesound title: "female grunts breaths") | 7.86-8.2 |
| `female_grunt_fs-martian-218908_6.wav` | 0.39 | https://freesound.org/people/martian/sounds/218908/ | martian | CC0 1.0 | freesound-previews/218908_martian.mp3 (Freesound title: "female grunts breaths") | 8.52-8.91 |
| `female_grunt_fs-petebuchwald-724211_1.wav` | 1.04 | https://freesound.org/people/petebuchwald/sounds/724211/ | petebuchwald | CC0 1.0 | freesound-previews/724211_petebuchwald.mp3 (Freesound title: "Female Grunts Breaths Struggle") | 1.23-2.27 |
| `female_grunt_fs-petebuchwald-724211_2.wav` | 0.39 | https://freesound.org/people/petebuchwald/sounds/724211/ | petebuchwald | CC0 1.0 | freesound-previews/724211_petebuchwald.mp3 (Freesound title: "Female Grunts Breaths Struggle") | 9.01-9.4 |
| `female_grunt_fs-petebuchwald-724211_3.wav` | 0.99 | https://freesound.org/people/petebuchwald/sounds/724211/ | petebuchwald | CC0 1.0 | freesound-previews/724211_petebuchwald.mp3 (Freesound title: "Female Grunts Breaths Struggle") | 35.44-36.43 |
| `female_grunt_fs-petebuchwald-724211_4.wav` | 0.56 | https://freesound.org/people/petebuchwald/sounds/724211/ | petebuchwald | CC0 1.0 | freesound-previews/724211_petebuchwald.mp3 (Freesound title: "Female Grunts Breaths Struggle") | 45.54-46.1 |
| `female_grunt_fs-petebuchwald-724211_5.wav` | 0.67 | https://freesound.org/people/petebuchwald/sounds/724211/ | petebuchwald | CC0 1.0 | freesound-previews/724211_petebuchwald.mp3 (Freesound title: "Female Grunts Breaths Struggle") | 52.11-52.78 |
| `female_grunt_fs-petebuchwald-724211_6.wav` | 0.37 | https://freesound.org/people/petebuchwald/sounds/724211/ | petebuchwald | CC0 1.0 | freesound-previews/724211_petebuchwald.mp3 (Freesound title: "Female Grunts Breaths Struggle") | 60.95-61.32 |
| `female_grunt_fs-craigsmith-482814_1.wav` | 1.34 | https://freesound.org/people/craigsmith/sounds/482814/ | craigsmith | CC0 1.0 | freesound-previews/482814_craigsmith.mp3 (Freesound title: "R21-15-Female Fighting Grunts.wav") | 0.0-1.34 |
| `female_grunt_fs-craigsmith-482814_2.wav` | 0.52 | https://freesound.org/people/craigsmith/sounds/482814/ | craigsmith | CC0 1.0 | freesound-previews/482814_craigsmith.mp3 (Freesound title: "R21-15-Female Fighting Grunts.wav") | 3.52-4.04 |
| `female_grunt_fs-craigsmith-482814_3.wav` | 0.63 | https://freesound.org/people/craigsmith/sounds/482814/ | craigsmith | CC0 1.0 | freesound-previews/482814_craigsmith.mp3 (Freesound title: "R21-15-Female Fighting Grunts.wav") | 6.72-7.35 |
| `female_grunt_fs-craigsmith-482814_4.wav` | 0.69 | https://freesound.org/people/craigsmith/sounds/482814/ | craigsmith | CC0 1.0 | freesound-previews/482814_craigsmith.mp3 (Freesound title: "R21-15-Female Fighting Grunts.wav") | 15.76-16.45 |
| `female_grunt_fs-craigsmith-482814_5.wav` | 0.75 | https://freesound.org/people/craigsmith/sounds/482814/ | craigsmith | CC0 1.0 | freesound-previews/482814_craigsmith.mp3 (Freesound title: "R21-15-Female Fighting Grunts.wav") | 19.11-19.86 |
| `female_grunt_fs-craigsmith-482814_6.wav` | 0.69 | https://freesound.org/people/craigsmith/sounds/482814/ | craigsmith | CC0 1.0 | freesound-previews/482814_craigsmith.mp3 (Freesound title: "R21-15-Female Fighting Grunts.wav") | 22.71-23.4 |
| `female_grunt_fs-artistic_dragonb-838819_1.wav` | 0.34 | https://freesound.org/people/Artistic_Dragonborn/sounds/838819/ | Artistic_Dragonborn | CC0 1.0 | freesound-previews/838819_Artistic_Dragonborn.mp3 (Freesound title: "Tiny Effort of a Person") | 0.9-1.24 |
| `female_grunt_fs-hisoul-520270_1.wav` | 0.34 | https://freesound.org/people/hisoul/sounds/520270/ | hisoul | CC0 1.0 | freesound-previews/520270_hisoul.mp3 (Freesound title: "Kali, Attack, Pant Short, Move, Decent_2.wav") | 0.0-0.34 |

## Male heavy breathing (31 files)

| File | Dur (s) | Source page | Author | Licence | Original file | Cut (s) |
|---|---|---|---|---|---|---|
| `male_breath_oga-mikeask_1.wav` | 2.53 | https://opengameart.org/content/breathing-tired | mikeask | CC0 1.0 | oga-mikeask-breathing-tired/breathing tired.wav | 0.16-2.69 |
| `male_breath_fs-myfreesoundaccou-543243_1.wav` | 1.89 | https://freesound.org/people/myfreesoundaccount1998/sounds/543243/ | myfreesoundaccount1998 | CC0 1.0 | freesound-previews/543243_myfreesoundaccount1998.mp3 (Freesound title: "vocals panting.WAV") | 0.74-2.63 |
| `male_breath_fs-myfreesoundaccou-543243_2.wav` | 2.81 | https://freesound.org/people/myfreesoundaccount1998/sounds/543243/ | myfreesoundaccount1998 | CC0 1.0 | freesound-previews/543243_myfreesoundaccount1998.mp3 (Freesound title: "vocals panting.WAV") | 2.75-5.56 |
| `male_breath_fs-artmasterrich-345464_1.wav` | 1.77 | https://freesound.org/people/Artmasterrich/sounds/345464/ | Artmasterrich | CC0 1.0 | freesound-previews/345464_Artmasterrich.mp3 (Freesound title: "Male_Medium_Breathing_01") | 0.02-1.79 |
| `male_breath_fs-artmasterrich-345464_2.wav` | 2.7 | https://freesound.org/people/Artmasterrich/sounds/345464/ | Artmasterrich | CC0 1.0 | freesound-previews/345464_Artmasterrich.mp3 (Freesound title: "Male_Medium_Breathing_01") | 1.81-4.51 |
| `male_breath_fs-artmasterrich-345464_3.wav` | 2.12 | https://freesound.org/people/Artmasterrich/sounds/345464/ | Artmasterrich | CC0 1.0 | freesound-previews/345464_Artmasterrich.mp3 (Freesound title: "Male_Medium_Breathing_01") | 4.55-6.67 |
| `male_breath_fs-njjjjjjjjjjjjjjj-566400_1.wav` | 2.1 | https://freesound.org/people/njjjjjjjjjjjjjjjjjjjjjjjj/sounds/566400/ | njjjjjjjjjjjjjjjjjjjjjjjj | CC0 1.0 | freesound-previews/566400_njjjjjjjjjjjjjjjjjjjjjjjj.mp3 (Freesound title: "Panting Male") | 0.0-2.1 |
| `male_breath_fs-njjjjjjjjjjjjjjj-566400_2.wav` | 1.91 | https://freesound.org/people/njjjjjjjjjjjjjjjjjjjjjjjj/sounds/566400/ | njjjjjjjjjjjjjjjjjjjjjjjj | CC0 1.0 | freesound-previews/566400_njjjjjjjjjjjjjjjjjjjjjjjj.mp3 (Freesound title: "Panting Male") | 2.13-4.04 |
| `male_breath_fs-njjjjjjjjjjjjjjj-566400_3.wav` | 1.85 | https://freesound.org/people/njjjjjjjjjjjjjjjjjjjjjjjj/sounds/566400/ | njjjjjjjjjjjjjjjjjjjjjjjj | CC0 1.0 | freesound-previews/566400_njjjjjjjjjjjjjjjjjjjjjjjj.mp3 (Freesound title: "Panting Male") | 4.08-5.93 |
| `male_breath_fs-njjjjjjjjjjjjjjj-566400_4.wav` | 2.72 | https://freesound.org/people/njjjjjjjjjjjjjjjjjjjjjjjj/sounds/566400/ | njjjjjjjjjjjjjjjjjjjjjjjj | CC0 1.0 | freesound-previews/566400_njjjjjjjjjjjjjjjjjjjjjjjj.mp3 (Freesound title: "Panting Male") | 5.97-8.69 |
| `male_breath_fs-simone_ds-366077_1.wav` | 2.59 | https://freesound.org/people/simone_ds/sounds/366077/ | simone_ds | CC0 1.0 | freesound-previews/366077_simone_ds.mp3 (Freesound title: "heavy breathing.wav") | 0.16-2.75 |
| `male_breath_fs-simone_ds-366077_2.wav` | 2.33 | https://freesound.org/people/simone_ds/sounds/366077/ | simone_ds | CC0 1.0 | freesound-previews/366077_simone_ds.mp3 (Freesound title: "heavy breathing.wav") | 2.96-5.29 |
| `male_breath_fs-simone_ds-366077_3.wav` | 1.83 | https://freesound.org/people/simone_ds/sounds/366077/ | simone_ds | CC0 1.0 | freesound-previews/366077_simone_ds.mp3 (Freesound title: "heavy breathing.wav") | 7.42-9.25 |
| `male_breath_fs-simone_ds-366077_4.wav` | 1.36 | https://freesound.org/people/simone_ds/sounds/366077/ | simone_ds | CC0 1.0 | freesound-previews/366077_simone_ds.mp3 (Freesound title: "heavy breathing.wav") | 9.35-10.71 |
| `male_breath_fs-vadersfear-716947_1.wav` | 1.34 | https://freesound.org/people/vadersfear/sounds/716947/ | vadersfear | CC0 1.0 | freesound-previews/716947_vadersfear.mp3 (Freesound title: "Guy_RunAndBreath1") | 0.02-1.36 |
| `male_breath_fs-vadersfear-716947_2.wav` | 2.44 | https://freesound.org/people/vadersfear/sounds/716947/ | vadersfear | CC0 1.0 | freesound-previews/716947_vadersfear.mp3 (Freesound title: "Guy_RunAndBreath1") | 1.42-3.86 |
| `male_breath_fs-vadersfear-716948_1.wav` | 2.3 | https://freesound.org/people/vadersfear/sounds/716948/ | vadersfear | CC0 1.0 | freesound-previews/716948_vadersfear.mp3 (Freesound title: "Guy_RunAndBreath2") | 0.04-2.34 |
| `male_breath_fs-vadersfear-716948_2.wav` | 2.0 | https://freesound.org/people/vadersfear/sounds/716948/ | vadersfear | CC0 1.0 | freesound-previews/716948_vadersfear.mp3 (Freesound title: "Guy_RunAndBreath2") | 2.41-4.41 |
| `male_breath_fs-spaudiobooks-776564_1.wav` | 2.21 | https://freesound.org/people/SPAudiobooks/sounds/776564/ | SPAudiobooks | CC0 1.0 | freesound-previews/776564_SPAudiobooks.mp3 (Freesound title: "Breathing man with tired puff") | 0.16-2.37 |
| `male_breath_fs-spaudiobooks-776564_2.wav` | 2.53 | https://freesound.org/people/SPAudiobooks/sounds/776564/ | SPAudiobooks | CC0 1.0 | freesound-previews/776564_SPAudiobooks.mp3 (Freesound title: "Breathing man with tired puff") | 2.41-4.94 |
| `male_breath_fs-spaudiobooks-776564_3.wav` | 2.79 | https://freesound.org/people/SPAudiobooks/sounds/776564/ | SPAudiobooks | CC0 1.0 | freesound-previews/776564_SPAudiobooks.mp3 (Freesound title: "Breathing man with tired puff") | 10.91-13.7 |
| `male_breath_fs-spaudiobooks-776564_4.wav` | 2.06 | https://freesound.org/people/SPAudiobooks/sounds/776564/ | SPAudiobooks | CC0 1.0 | freesound-previews/776564_SPAudiobooks.mp3 (Freesound title: "Breathing man with tired puff") | 13.89-15.95 |
| `male_breath_fs-darranambler-321669_1.wav` | 2.32 | https://freesound.org/people/darranambler/sounds/321669/ | darranambler | CC0 1.0 | freesound-previews/321669_darranambler.mp3 (Freesound title: "Heavy Breathing Man/Boy.WAV") | 0.94-3.26 |
| `male_breath_fs-darranambler-321669_2.wav` | 1.22 | https://freesound.org/people/darranambler/sounds/321669/ | darranambler | CC0 1.0 | freesound-previews/321669_darranambler.mp3 (Freesound title: "Heavy Breathing Man/Boy.WAV") | 3.31-4.53 |
| `male_breath_fs-darranambler-321669_3.wav` | 2.66 | https://freesound.org/people/darranambler/sounds/321669/ | darranambler | CC0 1.0 | freesound-previews/321669_darranambler.mp3 (Freesound title: "Heavy Breathing Man/Boy.WAV") | 4.64-7.3 |
| `male_breath_fs-darranambler-321669_4.wav` | 2.03 | https://freesound.org/people/darranambler/sounds/321669/ | darranambler | CC0 1.0 | freesound-previews/321669_darranambler.mp3 (Freesound title: "Heavy Breathing Man/Boy.WAV") | 7.32-9.35 |
| `male_breath_fs-sherby168-542078_1.wav` | 1.34 | https://freesound.org/people/sherby168/sounds/542078/ | sherby168 | CC0 1.0 | freesound-previews/542078_sherby168.mp3 (Freesound title: "huff and puff") | 0.98-2.32 |
| `male_breath_fs-sherby168-542078_2.wav` | 1.89 | https://freesound.org/people/sherby168/sounds/542078/ | sherby168 | CC0 1.0 | freesound-previews/542078_sherby168.mp3 (Freesound title: "huff and puff") | 3.78-5.67 |
| `male_breath_fs-sherby168-542078_3.wav` | 2.35 | https://freesound.org/people/sherby168/sounds/542078/ | sherby168 | CC0 1.0 | freesound-previews/542078_sherby168.mp3 (Freesound title: "huff and puff") | 13.38-15.73 |
| `male_breath_fs-sherby168-542078_4.wav` | 1.63 | https://freesound.org/people/sherby168/sounds/542078/ | sherby168 | CC0 1.0 | freesound-previews/542078_sherby168.mp3 (Freesound title: "huff and puff") | 23.34-24.97 |
| `male_breath_fs-14gpanskamuzatko-419779_1.wav` | 1.22 | https://freesound.org/people/14GPanskaMuzatko_Matej/sounds/419779/ | 14GPanskaMuzatko_Matej | CC0 1.0 | freesound-previews/419779_14GPanskaMuzatko_Matej.mp3 (Freesound title: "01 - Exhale male - effort.wav") | 0.66-1.88 |

## Female heavy breathing (19 files)

| File | Dur (s) | Source page | Author | Licence | Original file | Cut (s) |
|---|---|---|---|---|---|---|
| `female_breath_fs-kuroseishin-95567_1.wav` | 2.31 | https://freesound.org/people/Kuroseishin/sounds/95567/ | Kuroseishin | CC0 1.0 | freesound-previews/95567_Kuroseishin.mp3 (Freesound title: "breathing_female.wav") | 0.1-2.41 |
| `female_breath_fs-kuroseishin-95567_2.wav` | 2.6 | https://freesound.org/people/Kuroseishin/sounds/95567/ | Kuroseishin | CC0 1.0 | freesound-previews/95567_Kuroseishin.mp3 (Freesound title: "breathing_female.wav") | 2.49-5.09 |
| `female_breath_fs-melvinjaepel-435468_1.wav` | 2.68 | https://freesound.org/people/MelvinJaepel/sounds/435468/ | MelvinJaepel | CC0 1.0 | freesound-previews/435468_MelvinJaepel.mp3 (Freesound title: "woman run and breath.wav") | 0.0-2.68 |
| `female_breath_fs-melvinjaepel-435468_2.wav` | 2.73 | https://freesound.org/people/MelvinJaepel/sounds/435468/ | MelvinJaepel | CC0 1.0 | freesound-previews/435468_MelvinJaepel.mp3 (Freesound title: "woman run and breath.wav") | 2.7-5.43 |
| `female_breath_fs-melvinjaepel-435468_3.wav` | 2.7 | https://freesound.org/people/MelvinJaepel/sounds/435468/ | MelvinJaepel | CC0 1.0 | freesound-previews/435468_MelvinJaepel.mp3 (Freesound title: "woman run and breath.wav") | 7.24-9.94 |
| `female_breath_fs-melvinjaepel-435468_4.wav` | 2.66 | https://freesound.org/people/MelvinJaepel/sounds/435468/ | MelvinJaepel | CC0 1.0 | freesound-previews/435468_MelvinJaepel.mp3 (Freesound title: "woman run and breath.wav") | 9.96-12.62 |
| `female_breath_fs-valerie-vivegnis-865356_1.wav` | 2.02 | https://freesound.org/people/Valerie-Vivegnis/sounds/865356/ | Valerie-Vivegnis | CC0 1.0 | freesound-previews/865356_Valerie-Vivegnis.mp3 (Freesound title: "26.07.26-  Going Up Stairs - Out-of-Breath Female Breathing") | 0.26-2.28 |
| `female_breath_fs-valerie-vivegnis-865356_2.wav` | 1.7 | https://freesound.org/people/Valerie-Vivegnis/sounds/865356/ | Valerie-Vivegnis | CC0 1.0 | freesound-previews/865356_Valerie-Vivegnis.mp3 (Freesound title: "26.07.26-  Going Up Stairs - Out-of-Breath Female Breathing") | 4.38-6.08 |
| `female_breath_fs-valerie-vivegnis-865356_3.wav` | 2.32 | https://freesound.org/people/Valerie-Vivegnis/sounds/865356/ | Valerie-Vivegnis | CC0 1.0 | freesound-previews/865356_Valerie-Vivegnis.mp3 (Freesound title: "26.07.26-  Going Up Stairs - Out-of-Breath Female Breathing") | 8.91-11.23 |
| `female_breath_fs-valerie-vivegnis-865356_4.wav` | 1.35 | https://freesound.org/people/Valerie-Vivegnis/sounds/865356/ | Valerie-Vivegnis | CC0 1.0 | freesound-previews/865356_Valerie-Vivegnis.mp3 (Freesound title: "26.07.26-  Going Up Stairs - Out-of-Breath Female Breathing") | 13.74-15.09 |
| `female_breath_fs-owlstorm-151215_1.wav` | 2.42 | https://freesound.org/people/OwlStorm/sounds/151215/ | OwlStorm | CC0 1.0 | freesound-previews/151215_OwlStorm.mp3 (Freesound title: "Female Breathing Heavily (2)") | 0.11-2.53 |
| `female_breath_fs-owlstorm-151215_2.wav` | 1.3 | https://freesound.org/people/OwlStorm/sounds/151215/ | OwlStorm | CC0 1.0 | freesound-previews/151215_OwlStorm.mp3 (Freesound title: "Female Breathing Heavily (2)") | 2.56-3.86 |
| `female_breath_fs-tcrocker68-235593_1.wav` | 1.96 | https://freesound.org/people/tcrocker68/sounds/235593/ | tcrocker68 | CC0 1.0 | freesound-previews/235593_tcrocker68.mp3 (Freesound title: "Girl_Heavy_Breathing.wav") | 0.0-1.96 |
| `female_breath_fs-tcrocker68-235593_2.wav` | 2.68 | https://freesound.org/people/tcrocker68/sounds/235593/ | tcrocker68 | CC0 1.0 | freesound-previews/235593_tcrocker68.mp3 (Freesound title: "Girl_Heavy_Breathing.wav") | 9.88-12.56 |
| `female_breath_fs-tcrocker68-235593_3.wav` | 1.23 | https://freesound.org/people/tcrocker68/sounds/235593/ | tcrocker68 | CC0 1.0 | freesound-previews/235593_tcrocker68.mp3 (Freesound title: "Girl_Heavy_Breathing.wav") | 23.45-24.68 |
| `female_breath_fs-tcrocker68-235593_4.wav` | 1.57 | https://freesound.org/people/tcrocker68/sounds/235593/ | tcrocker68 | CC0 1.0 | freesound-previews/235593_tcrocker68.mp3 (Freesound title: "Girl_Heavy_Breathing.wav") | 34.7-36.27 |
| `female_breath_fs-tcrocker68-235586_1.wav` | 2.48 | https://freesound.org/people/tcrocker68/sounds/235586/ | tcrocker68 | CC0 1.0 | freesound-previews/235586_tcrocker68.mp3 (Freesound title: "Girl_Gasp_Heavy_Breathing.wav") | 0.0-2.48 |
| `female_breath_fs-tcrocker68-235586_2.wav` | 1.25 | https://freesound.org/people/tcrocker68/sounds/235586/ | tcrocker68 | CC0 1.0 | freesound-previews/235586_tcrocker68.mp3 (Freesound title: "Girl_Gasp_Heavy_Breathing.wav") | 2.52-3.77 |
| `female_breath_fs-tcrocker68-235586_3.wav` | 2.77 | https://freesound.org/people/tcrocker68/sounds/235586/ | tcrocker68 | CC0 1.0 | freesound-previews/235586_tcrocker68.mp3 (Freesound title: "Girl_Gasp_Heavy_Breathing.wav") | 3.83-6.6 |

## Extra: crowd rowing grunts (1 files)

| File | Dur (s) | Source page | Author | Licence | Original file | Cut (s) |
|---|---|---|---|---|---|---|
| `extra_crowd_rowing_fs-shangusburger-764157_1.wav` | 12.44 | https://freesound.org/people/ShangusBurger/sounds/764157/ | ShangusBurger | CC0 1.0 | freesound-previews/764157_ShangusBurger.mp3 (Freesound title: "CRWDBatl_Crowd Grunting, Exerting, Metered, Rowing_ShaneVincent_GSC24_MSDEC-MKH435-Spirit.wav") | 0.0-12.44 |

## Drums (2026-10-04)

All by JIMMYJAMES112 on Freesound: a dunun set (replicas from oil drums, and a carved sangban, with cow hide skins), sampled on a Roland SPD-SX. Downloaded as the public HQ MP3 previews (the originals need a login) to `originals/fs-jimmyjames112-dunun/`.

| File | Page | License | Used |
|---|---|---|---|
| `170180` Dundunba.WAV | https://freesound.org/people/JIMMYJAMES112/sounds/170180/ | CC0 1.0 | **Yes:** `assets/sounds/dundun.wav` (the war drum), tail trimmed below -60 dB, normalized to -1 dBFS |
| `170177` Sangban.WAV | https://freesound.org/people/JIMMYJAMES112/sounds/170177/ | CC0 1.0 | Not yet |
| `170178` Sangban mute.WAV | https://freesound.org/people/JIMMYJAMES112/sounds/170178/ | CC0 1.0 | Not yet |
| `170179` Kenkeni.WAV | https://freesound.org/people/JIMMYJAMES112/sounds/170179/ | CC0 1.0 | Not yet |
| `506467` Kenkeni_Mute.aiff | https://freesound.org/people/JIMMYJAMES112/sounds/506467/ | **CC BY-NC 4.0** | **No** (not CC0); deleted |
