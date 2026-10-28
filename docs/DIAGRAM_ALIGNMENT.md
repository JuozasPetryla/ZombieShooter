# Atitikimas pateiktai pilnai diagramai

Lyginama su paskutiniu pilnos diagramos paveikslėliu, kuriame yra `IWeapon` ir
komandų metodai su `session` parametru. Ankstesnis ginklų fragmentas turėjo kitą
struktūrą ir šiam variantui netaikomas.

## Pritaikyta

- `Weapon` ir `WeaponDecorator` realizuoja `IWeapon`; dekoratoriai paveldi
  `WeaponDecorator`. `Player` saugo du `IWeapon` objektus.
- `Shoot(player, factory, bullets)` ir `Reload(player)` gauna kontekstą per
  parametrus; senų `Bind` ir saugomų sesijos konteksto nuorodų nebėra.
- `GameSession.Fire(player)` kviečia ginklą ir po sėkmės siunčia `ShotFired`.
- Komandų konstruktoriai gauna `playerId` (pasirinkimo komanda dar `weapon`).
  `Execute(session)`, `Undo(session)`, `Apply(session)` gauna sesiją vykdymo metu.
- `CommandDispatcher.ExecutePending`, `UndoLast`, `Clear` yra vieši;
  dispatcher perduoda savo sesiją komandai.
- `IGameSubject` turi `Notify`; sesija realizuoja jį eksplicitiškai, išlaikydama
  privatų `Notify` metodą, kaip sesijos langelyje.
- Serverio `AcceptAsync`, `Frame`, `ServeAsync`, `TickAsync`, kliento
  `ExchangeAsync` ir sesijos `DiscardPendingCommands` yra vieši, kaip diagramoje.
  `GameServer` numatytasis prievadas yra 7777. Įprastai serverį paleidžia `Start`,
  kuris koordinuoja ciklus; tiesiogiai papildomai jų paleisti nereikia.

## Būtini papildymai ir diagramos žymėjimo pataisos

- `WeaponDecorator.Inner` ir priedų konstruktoriai priima `IWeapon`, ne konkretų
  `Weapon`. Kitaip dekoratorius negalėtų apgaubti kito dekoratoriaus.
- Ginklo savybės įtrauktos į `IWeapon`: jų reikia HUD, snapshot'ams ir priedams.
  Tai viešos tik skaitomos savybės, o ne viešai keičiami laukai.
- Papildomos operacijų perkrovos priima `WeaponStats`, perduodamus per grandinę.
  Įprasti klientai kviečia diagramoje parodytus parašus. Tai leidžia galutiniam
  `Weapon` pritaikyti išorinių dekoratorių priedus be papildomų helper klasių.
- `AdvanceReload(player, dt)` išlaiko laipsnišką perkrovimą. `Player.Weapon` ir
  `MagazineSize` išlaiko pasirinkimą bei HUD; jų paveikslėlyje nėra.
- `PlayerCommand._executed` saugo nuo pakartotinio vykdymo; `_receiver` saugomas po
  sėkmės ir saugo nuo Undo kitoje sesijoje. Abu patikrinimai būtini korektiškai istorijai.
- `IPlayerInputSource.Read` paliktas sąsajoje: be jo adapterių negalima kviesti
  per bendrą kontraktą, nors paveikslėlyje sąsajos operacijų skyrius tuščias.
- `IPrototype<T>` paliktas generinis: diagramos `Clone(): T` reikalauja tipo parametro.
- Masyvai ir generinės kolekcijos išlaikyti: `TcpClient?[]`, `long[]`,
  `NetworkAction[]`, snapshot masyvai, `Queue<IGameCommand>` ir kt.
  Diagramoje dalyje atributų praleisti masyvo žymenys ar generiniai argumentai.
- Konstruktoriai neturi grąžinimo tipo. Diagramoje esantys `: bool` / `: void`
  prie konstruktorių nelaikomi vykdomo kodo reikalavimu.
- Sesijos skaitomos būsenos savybės ir serverio `Port` lieka viešos, nes jų reikia
  HUD, fasadui, paleidimo kodui ir testams. Paveikslėlio privatūs ženklai šiose
  vietose neatspindi vartotojų priklausomybių. Setterių prieiga lieka ribota.
- `WeaponStats`, enum tipai ir `PickupSnapshot` palikti, nors atskiri jų langeliai
  nepateikti: tipai naudojami pačioje diagramoje ir tinklo protokole.
- Išlaikyti `abstract`, `sealed`, `static`, `readonly`, nullable ir C# property
  accessor apribojimai, kurių diagramoje dalis nepažymėta. `FireCooldown` nėra
  `Pellets` perrašymas, nors prie jo diagramoje likusi tokia anotacija.

Bridge, Strategy, Builder, Prototype, Factory Method, Facade ir Singleton
pagrindinės struktūros paliktos. Generuojama PlantUML diagrama rodo visą faktinį
kodą, įskaitant šiuos papildymus; ji nėra rankinio paveikslėlio kopija.
