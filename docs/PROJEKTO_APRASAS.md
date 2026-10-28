# Zombie Shooter: reikalavimai ir šablonai

Iš viršaus matomas išlikimo žaidimas: judėti, šaudyti, rinkti daiktus ir įveikti
zombių bangas. Palaikomas vienas žaidėjas, vietinis bendras žaidimas ir du klientai
per lokalų autoritetingą serverį. Paleidimas ir valdymas pateikti [README](../README.md).

## 1. Apimtis ir priėmimo kriterijai

| Sritis | Patikrinamas elgesys |
|---|---|
| Judėjimas | 220 vienetų/s; įstrižas judėjimas ne greitesnis; žaidėjas lieka 1280 × 720 arenoje už HUD ribų. |
| Kova | Pistoletas šauna 1 kulką, shotgun – 5; vienas šūvis sunaudoja vieną šovinį. Cooldown ir perkrovimas blokuoja šaudymą. Kulkos atkarpa tikrinama nuo praskridimo pro zombį. Draugiškos ugnies nėra. |
| Priešai | Walker, Runner ir Brute juda link artimiausio gyvo žaidėjo. Kontaktinė žala turi apsaugos intervalą; vienas nužudymas duoda vieną įvykį ir atlygį. |
| Daiktai | Gydymas +35 iki 100 HP; pilnų HP žaidėjas jo nesunaudoja. Shotgun suteikiamas 15 s; daiktai išnyksta po 18 s. |
| Bangos | Kita banga prasideda tik sunaikinus visus priešus ir pasibaigus pertraukai. Kiekvienas išgyvenęs žaidėjas gauna tris ginklų dekoratorius. |
| Bendras žaidimas | Abu žaidėjai valdomi atskirai. Žuvęs partneris grįžta su 50 HP ir 2 s apsauga po įveiktos bangos. Visų žūtis baigia sesiją. |
| Režimai | F3 cikliškai parenka Normal, Horde arba Brute ir pradeda naują sesiją. Tas pats pasirinkimas galimas serverio argumentu. |
| Komandos | Perkrovimas ir ginklo parinkimas vykdomi FIFO per kitą teigiamos trukmės žingsnį. Kiekvienas žaidėjas gali atšaukti vieną paskutinę galiojančią komandą. |
| Pauzė | P arba fokuso praradimas sustabdo vietinę simuliaciją. GameOver sustabdo simuliaciją ir atmeta komandas. |
| Tinklas | Du klientai mato serverio būseną ir valdo tik savo žaidėją. Vienas klientas laukia; atsijungus sesija sustoja; laisvą vietą gali perimti nauja jungtis. Trečia jungtis atmetama. |
| Izoliacija | Sesijos nepriklausomos; Core neturi MonoGame priklausomybės. Testai ir serveris veikia be grafinio lango. |
| Ribos | Komandų eilė ≤64; viename tinklo kadre ≤16 veiksmų; kadras ≤1 MiB. NaN, Infinity ir neteisingi veiksmų enum atmetami. |

Kliūčių žemėlapiai, kelio paieška, paskyros, išsaugojimas diske, garsas, klientų
prognozavimas ir keli vienalaikiai serverio kambariai neįtraukti. Seed naudojamas
atkuriamoms patikroms, o ne skirtingų platformų lockstep garantijai.

## 2. Žaidimo parametrai

Žaidėjas pradeda su 100 HP. Kulkos greitis 650, gyvavimo laikas 1,4 s.

| Zombis | HP | Greitis | Spindulys | Žala | Apsauga po smūgio | Taškai |
|---|---:|---:|---:|---:|---:|---:|
| Walker | 60 | 70 | 14 | 10 | 0,65 s | 10 |
| Runner | 35 | 125 | 14 | 8 | 0,5 s | 15 |
| Brute | 150 | 48 | 22 | 20 | 1,2 s | 25 |

Kiekviena banga po pirmos prideda 5 HP ir 2 greičio vienetus (greičio priedas ≤40).
`XpReward` reiškia taškų atlygį; atskiros XP sistemos nėra.

| Ginklas | Žala kulkai | Kulkos | Intervalas | Dėtuvė | Perkrovimas |
|---|---:|---:|---:|---:|---:|
| Pistol | 35 | 1 | 0,18 s | 12 | 1,2 s |
| Shotgun | 25 | 5 | 0,5 s | 6 | 1,8 s |

Atsarginių šovinių kiekis neribotas. Ginklo pakeitimas nutraukia perkrovimą,
išsaugo dėtuves; shotgun laikas eina ir pasirinkus pistoletą. Surinktas shotgun
parenka ginklą, užpildo jo dėtuvę ir nutraukia perkrovimą. Žuvusių žaidėjų laikmačiai sustoja.
Po bangos ginklai gauna DamageBonus (+5 pistoletui, +3 shotgun), FireRateBonus
(×0,95, bent 0,05 s) ir MagazineSizeBonus (+1). Dėtuvė automatiškai neužsipildo.

Tegu `w` – banga, `p` – žaidėjų skaičius:

| Pasirinkimas / strategija | Priešų kiekis | Atsiradimo intervalas | Pertrauka | Sudėtis |
|---|---|---|---|---|
| Normal / NormalMode | `(4 + 3w) × p` | `max(0,18; 0,85 − 0,035w)` | 4 s | Runner nuo 3, Brute nuo 5 |
| Horde / HordeMode | `(10 + 5w) × p` | `max(0,08; 0,35 − 0,01w)` | 2 s | Walker |
| Brute / BruteMode | `(2 + w) × p` | `max(0,3; 1,2 − 0,04w)` | 5 s | Brute |

Visuose režimuose daiktas iškrenta su 22 % tikimybe: pusė gydymas, pusė shotgun.
Režimai skiriasi tik bangų strategija; zombių parametrai ir daiktai nesikeičia.

## 3. Šablonai ir dokumentuoti minimumai

Kiekvienas šablonas atitinka paskaitų skaidrėse pateiktą GoF struktūrą. Viena klasė
gali atlikti kelių šablonų vaidmenis; dalyviai skaičiuojami kiekvienam šablonui atskirai.

| Šablonas | GoF vaidmenys → kodo dalyviai | Dabartinė būsena ir kriterijus |
|---|---|---|
| Singleton | Singleton → HighScores: privatus konstruktorius, `static Instance`, Lazy holder (SingletonHolder variantas) ir Interlocked maksimumas | Viena instancija; 10 000 lygiagrečių atnaujinimų patikra |
| Factory Method | Creator → ZombieCreator (`Spawn()` = AnOperation, kviečia `Create()`); ConcreteCreator → WalkerCreator, RunnerCreator, BruteCreator; Product → Zombie; ConcreteProduct → Walker, Runner, Brute | 3 konkretūs kūrėjai ir 3 produktų klasės |
| Strategy | Strategy → IGameMode; ConcreteStrategy → NormalMode, HordeMode, BruteMode; Context → GameSession | 3 pasirenkamos strategijos |
| Observer | Subject → IGameSubject / GameSession (`Attach`, `Detach`, `Notify`); Observer → IGameObserver (`OnGameEvent` = update); ConcreteObserver → ZombieShooterGame, GameServer, GameFacade | 3 prenumeratoriai, atjungimo patikra ir sekų diagrama |
| Builder | Director → ZombieDirector; Builder / ConcreteBuilder → ZombieBuilder (`Reset()`, grandininiai žingsniai ir `Build()` su tikrinimu); Product → ZombieArchetype | 1 builderis; direktorius saugo 3 receptus (Walker, Runner, Brute) |
| Prototype | Prototype → IPrototype; ConcretePrototype → ZombieArchetype, AttackProfile; Client → EntityFactory saugo ir klonuoja šablonus | Deep / shallow kopijos, nuorodos, adresai ir sąnaudos |
| Decorator | Component → IWeapon; ConcreteComponent → Weapon; Decorator → WeaponDecorator; ConcreteDecorator → DamageBonus, FireRateBonus, MagazineSizeBonus | 3 vienu metu taikomi sluoksniai |
| Command | Command → IGameCommand (`Execute(session)`, `Undo(session)`); ConcreteCommand → ReloadCommand, SelectWeaponCommand, UndoCommand (receiver ir būsena); Receiver → GameSession; Invoker → CommandDispatcher; Client → ZombieShooterGame, GameServer | Būseną išsaugantis ir galiojimą tikrinantis Undo |
| Adapter | Target → IPlayerInputSource; Adapter → KeyboardMouseAdapter, ControllerAdapter; Adaptee → KeyboardMouseDevice, ControllerDevice (object adapter) | 2 adapteriai; vienas Read sujungia 3 įrenginio operacijas |
| Facade | Facade → GameFacade; posistemės → InputBuffer, CommandDispatcher, GameSession | 3 posistemės; desktop ir serveris – 2 klientai |
| Bridge | Abstraction → GameNotification; RefinedAbstraction → WaveNotification, PickupNotification; Implementor → INotificationOutput; ConcreteImplementor → ZombieShooterGame, GameServer | 2 abstrakcijos ir 2 realizacijos |

`EntityFactory` sukuria `ZombieBuilder` ir perduoda jį `ZombieDirector`. Builder žino,
KAIP pagaminti dalį, o Director – KOKIA TVARKA kviesti žingsnius (`Reset()` → `OfKind`
→ … → `Build()`); taip sukuriama po vieną archetipą kiekvienai zombio rūšiai. Tie
archetipai yra Prototype šablonai: kiekvienas atsiradimas juos klonuoja ir perduoda
Factory Method kūrėjui, kurio `Spawn()` pritaiko bangos priedus ir kviečia `Create()`.

Horde ir Brute keičia tik `IGameMode` strategiją. Strategija parenkama sesijos pradžioje
(F3 arba serverio argumentu), nes režimo pakeitimas pradeda naują sesiją.

`GameSession` kaip Subject saugo `IGameObserver` sąrašą ir apie įvykius praneša per
`Notify()`. `ZombieShooterGame` ir `GameServer` yra fasado klientai, Observer
prenumeratoriai ir dvi Bridge išvesties realizacijos (`INotificationOutput`).
`GameFacade` taip pat prenumeruoja sesiją ir kaupia sunumeruotą įvykių žurnalą snapshot'ams.

Komandos konstruktoriuje gauna žaidėjo ID ir konkrečiai komandai reikalingus
parametrus. Invoker perduoda savo sesiją į `Execute(session)` bei `Undo(session)`.
`PlayerCommand` prisimena sėkmingo vykdymo sesiją ir būsenos versiją, todėl komandos
negalima atšaukti kitoje sesijoje net sutapus žaidėjų ID.

`IWeapon` realizuoja `Weapon` ir `WeaponDecorator`. Dekoratorius saugo `IWeapon`,
kad būtų galima uždėti kelis sluoksnius. `Shoot(player, factory, bullets)` ir
`Reload(player)` deleguojami vidiniam objektui. Papildomos metodų perkrovos perduoda
bendrus `WeaponStats`, kad visų dekoratorių priedai pasiektų konkretų ginklą.
`AdvanceReload` užbaigia perkrovimą. `Player` saugo šovinius ir laikmačius,
`GameSession.Fire` koordinuoja šūvį ir siunčia įvykį. `IGameSubject.Notify`
realizuojamas eksplicitiškai ir kviečia privatų sesijos `Notify`.

[Pilnos diagramos atitikimo pastabos](DIAGRAM_ALIGNMENT.md) išvardija tipų pataisas
ir veikimui paliktus papildymus.

Jeigu taikomas reikalavimas „bent 3 nesikartojantys šablonai studentui“, tikrieji
autoriai turi įrašyti savo atsakomybes. Šis dokumentas autorystės nepatvirtina.

## 4. UML ir vykdymas

Vienas [dabartinio kodo klasių modelis](uml/01-current-classes.svg)
([PlantUML šaltinis](uml/01-current-classes.puml)) ir atskira
[Use Case diagrama](uml/00-use-cases.puml). Pradinis istorinis klasių modelis pašalintas.
Klasių vaizde įtraukti visi `src/` deklaruoti tipai: klasės, sąsajos, record, enum,
DTO ir piešimo pagalbininkai. Pateikti visų matomumo lygių laukai, savybės, įvykiai,
konstruktoriai ir metodai su tipais, parametrais bei matomumu. Paveldėti nariai
rodomi baziniuose tipuose, o partial deklaracijos sujungtos. Pažymėti saugomi
pirminio konstruktoriaus parametrai. Kompiliatoriaus generuojami pagalbiniai nariai,
numatytieji nedeklaruoti konstruktoriai, top-level paleidimo kodas, lokalūs
kintamieji ir funkcijos bei testai į šį klasių modelį neįtraukti. Išoriniai baziniai
tipai rodomi be narių. SVG skirtas peržiūrėti didinant mastelį; generavimo komandos
pateiktos README. Rodyklės rodo paveldėjimą, sąsajų realizavimą, kompoziciją,
saugomų objektų ryšius ir pasirinktus šablonų ryšius. Vien parametrų, grąžinamų tipų
ir enum savybių priklausomybės rodyklėmis nekartojamos: tipai lieka narių parašuose.
Use Case `include` reiškia, kad F2/F3 visada pradeda naują sesiją.

Observer bangos pabaigos eiga (įvykis sinchroninis):

[PlantUML versija](uml/02-observer-sequence.puml) papildomai rodo prenumeratų
registravimą ir po `WaveCleared` siunčiamą `WeaponsUpgraded` įvykį.

```mermaid
sequenceDiagram
    participant Host as ZombieShooterGame
    participant Facade as GameFacade
    participant Session as GameSession
    participant Notice as WaveNotification
    Host->>Facade: Tick(dt)
    Facade->>Session: Update(dt, inputs)
    Session->>Session: Baigti bangą, patobulinti ginklus
    Session->>Session: Notify(WaveCleared)
    Session->>Facade: OnGameEvent(WaveCleared)
    Facade->>Facade: Pridėti įvykį su sekos numeriu
    Session->>Host: OnGameEvent(WaveCleared)
    Host->>Notice: Show(event)
    Notice->>Host: INotificationOutput.Write(text)
    Host->>Host: Atnaujinti HUD pranešimą
```

CommandDispatcher saugo FIFO ir paskutinę sėkmingą komandą kiekvienam žaidėjui.
Undo atkuria ginklo ir perkrovimo būseną tik toje pačioje sesijoje, kol ActionVersion
galioja. Šūvis, baigtas perkrovimas, daiktas, ginklo galiojimo pabaiga, žūtis ar bangos
patobulinimas atšaukimą panaikina. Šoviniai negrąžinami; redo nėra.

Serverio simuliacija veikia atskiru 60 Hz ciklu. Klientas siunčia PlayerInput ir
leidžiamų veiksmų enum; serveris pagal jungtį parenka žaidėjo ID ir sukuria komandas.
Užklausos atsakymas yra dabartinis snapshot, todėl dar gali nerodyti ką tik į eilę
įdėtos komandos rezultato. Prieiga prie fasado serializuojama užraktu.

TCP transportas: JSON ir 4 baitų big-endian ilgio prefiksas. Kliento ciklas laukia
16 ms, serveris taip pat riboja užklausų dažnį. Po 250 ms įvestis nunulinama; po 5 s
be užklausos jungtis nutraukiama. Kliento operacijos timeout – 3 s. Atsijungus išvaloma
įvestis, komandų eilė ir undo istorija. Fasadas laiko 128 paskutinius įvykius; snapshot
klientas naudoja sekos numerį, kad Observer įvykiai nesikartotų.

## 5. Patikra ir demonstracija

[Headless testų komanda](../README.md#run) tikrina kovą, bangas, co-op, šablonus,
undo ir tikrą TCP ryšį: du klientai, judėjimas, šaudymas, perkrovimas, atšaukimas,
atsijungimas, vietos perėmimas ir neteisingas veiksmas.
[Prototype ataskaita](PROTOTYPE_REPORT.md) aprašo deep / shallow eksperimentą.

Rankomis patikrinti: grafinį langą, pelės taikymą, P2 klaviatūrą ir valdiklį,
F2/F3, pauzę, HUD, du klientus ir serverį. Galutinei ataskaitai pridėti solo,
co-op ir tinklo demonstracijos ekrano nuotraukas. Istorinių pateikimo datų ir
studentų autorystės kodas neįrodo.
