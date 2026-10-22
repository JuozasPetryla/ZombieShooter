# Prototype: deep ir shallow kopijų palyginimas

Užfiksuota 2026-10-15 vykdant `ZombieShooter.Core.Tests` su .NET 8, Linux x64. Tai istorinio matavimo duomenys; dabartinė testų programa kiekvieną kartą spausdina naujus rezultatus.

## Eksperimentas

1. `original = new ZombieDirector(new ZombieBuilder()).Construct(ZombieKind.Walker)` sukuria archetipą ir jo AttackProfile (`Damage = 10`).
2. `shallow = original.ShallowClone()` kopijuoja archetipą, bet išsaugo AttackProfile nuorodą.
3. `deep = original.Clone()` kopijuoja ir archetipą, ir AttackProfile.
4. `shallow.Attack.Damage = 77` pakeičia shallow bei originalo žalą, tačiau nepakeičia deep kopijos.

Faktiniai testų išvesties adresai:

| Objektas | Archetipas | AttackProfile | Žala po pakeitimo |
|---|---|---|---:|
| Originalas | `0x7F7E5E02D9A0` | `0x7F7E5E02D9D0` | 77 |
| Shallow | `0x7F7E5E02D9E8` | `0x7F7E5E02D9D0` | 77 |
| Deep | `0x7F7E5E02DA18` | `0x7F7E5E02DA48` | 10 |

Visi trys archetipai yra atskiri objektai. Originalo ir shallow kopijos AttackProfile adresai sutampa; deep kopijos AttackProfile adresas skiriasi. Tą patį papildomai patikrina `ReferenceEquals` ir būsenos keitimo testai.

Adresai paimti trumpame `GC.TryStartNoGCRegion` regione ir nėra hash reikšmės. Jie galioja tik šiam diagnostiniam nuskaitymui; negalima laikyti jų pastoviais objektų identifikatoriais. Kito paleidimo adresai bus kitokie. Adresai niekada nedereferencijuojami ir nenaudojami žaidimo logikoje.

## Kopijavimo sąnaudos

100 000 kopijų vieno paleidimo rezultatas:

| Kopijavimas | Trukmė | Paskirta atmintis |
|---|---:|---:|
| Shallow | 6,362 ms | 4 800 000 baitų |
| Deep | 6,229 ms | 9 600 000 baitų |

Deep variantas papildomai sukuria AttackProfile, todėl šiame eksperimente paskiria daugiau atminties. Trukmės yra iliustracinės: vienas nekontroliuojamas matavimas su JIT ir GC poveikiu neleidžia teigti, kad deep visuomet greitesnis ar lėtesnis. Šiame paleidime trukmės beveik vienodos.

## Pakartojimas ir gynimas

```sh
dotnet run --project tests/ZombieShooter.Core.Tests
```

Kadangi ši programa vykdo ir serverio patikras, reikia leisti vietinius TCP lizdus. Programa pati spausdina naują adresų ir sąnaudų ataskaitą; šio dokumento automatiškai neperrašo.

Demonstracijai `ZombieArchetype.Clone()` metode laikinai pakeisti `Attack = Attack.Clone()` į `Attack = Attack`: gilioji kopija tampa negili, jos nuorodų ir būsenos izoliacijos testai nepraeina. Po demonstracijos atkurti pirminę eilutę. Žaidimas naudoja gilią kopiją per `EntityFactory.CreateZombie`, kad vieno zombio kūrimas nepakeistų kitų prototipų.
