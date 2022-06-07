using System.Threading.Tasks.Dataflow;
using Microsoft.VisualBasic;
using PV178.Homeworks.HW03.DataLoading.DataContext;
using PV178.Homeworks.HW03.DataLoading.Factory;
using PV178.Homeworks.HW03.Model;
using PV178.Homeworks.HW03.Model.Enums;

namespace PV178.Homeworks.HW03
{
    public class Queries
    {
        private IDataContext? _dataContext;
        public IDataContext DataContext => _dataContext ??= new DataContextFactory().CreateDataContext();

        /// <summary>
        /// SFTW si vyžiadala počet útokov, ktoré sa udiali v krajinách začinajúcich na písmeno <'A', 'G'>,
        /// a kde obete boli muži v rozmedzí <15, 40> rokov.
        /// </summary>
        /// <returns>The query result</returns>
        public int AttacksAtoGCountriesMaleBetweenFifteenAndFortyQuery()
        {

            var attacksCount = DataContext.Countries
                .Join(DataContext.SharkAttacks,
                    country => country.Id,
                    attack => attack.CountryId,
                    (country, attack) => new {attack.AttackedPersonId, country.Name})
                .Join(DataContext.AttackedPeople,
                    countyAndShark => countyAndShark.AttackedPersonId,
                    person => person.Id,
                    (countyAndShark, person) => new {countyAndShark.Name, person.Age, person.Sex})
                .Where(country =>
                    (country.Name!.FirstOrDefault().Equals('A') || country.Name.FirstOrDefault().Equals('B')
                                                                || country.Name!.FirstOrDefault().Equals('C')
                                                                || country.Name!.FirstOrDefault().Equals('D')
                                                                || country.Name!.FirstOrDefault().Equals('E')
                                                                || country.Name!.FirstOrDefault().Equals('F')
                                                                || country.Name!.FirstOrDefault().Equals('G'))
                    && (country.Age >= 15 && country.Age <= 40)
                    && (country.Sex == Sex.Male))
                .ToList()
                .Count();
            
            return attacksCount;
        }

        /// <summary>
        /// Vráti zoznam, v ktorom je textová informácia o každom človeku,
        /// ktorého meno nie je známe (začína na malé písmeno alebo číslo) a na ktorého zaútočil žralok v štáte Bahamas.
        /// Táto informácia je v tvare:
        /// {meno človeka} was attacked in Bahamas by {latinský názov žraloka}
        /// </summary>
        /// <returns>The query result</returns>
        public List<string> InfoAboutPeopleWithUnknownNamesAndWasInBahamasQuery()
        {
            var unknownNamesAndWasInBahamasQuery = DataContext.Countries
                .Join(DataContext.SharkAttacks,
                    country => country.Id,
                    attack => attack.CountryId,
                    (country, attack) => new {attack.AttackedPersonId, country.Name, attack.SharkSpeciesId})
                .Where(info => info.Name!.Equals("Bahamas"))
                .Join(DataContext.AttackedPeople,
                    countyAndShark => countyAndShark.AttackedPersonId,
                    person => person.Id,
                    (countyAndShark, person) => new {person.Name, countyAndShark.SharkSpeciesId})
                .Where(info => Char.IsLower(info.Name!.ToCharArray()[0]) || Char.IsNumber(info.Name!.ToCharArray()[0]))
                .Join(DataContext.SharkSpecies,
                    personAndAttackInfo => personAndAttackInfo.SharkSpeciesId,
                    shark => shark.Id,
                    (personAndAttackInfo, shark) => new {personAndAttackInfo.Name, shark.LatinName})
                .Select(info => info.Name + " was attacked in Bahamas by " + info.LatinName)
                .ToList();

            return unknownNamesAndWasInBahamasQuery;
        }

        /// <summary>
        /// Prišla nám ďalšia požiadavka od našej milovanej SFTW. 
        /// Chcú od nás 5 názvov krajín s najviac útokmi, kde žraloky merali viac ako 3 metre.
        /// Požadujú, aby tieto data boli zoradené abecedne.
        /// </summary>
        /// <returns>The query result</returns>
        public List<string> FiveCountriesWithTopNumberOfAttackSharksLongerThanThreeMetersQuery()
        {
            var countryListOfLongSharks = DataContext.Countries
                .Join(DataContext.SharkAttacks,
                    country => country.Id,
                    attack => attack.CountryId,
                    (country, attack) => new {country.Name, attack.SharkSpeciesId})
                .Join(DataContext.SharkSpecies,
                    attack => attack.SharkSpeciesId,
                    sharkInfo => sharkInfo.Id,
                    (attack, sharkInfo) => new {attack.Name, sharkInfo.Length})
                .Where(info => (int)info.Length > 3)
                .GroupBy(info => info.Name)
                .OrderByDescending(info => info.Count())
                .Take(5)
                .Select(country => country.Key)
                .OrderBy(country => country)
                .ToList();
            
            return countryListOfLongSharks;
        }

        /// <summary>
        /// SFTW chce zistiť, či žraloky berú ohľad na pohlavie obete. 
        /// Vráti informáciu či každý druh žraloka, ktorý je dlhší ako 2 metre
        /// útočil aj na muža aj na ženu.
        /// </summary>
        /// <returns>The query result</returns>
        public bool AreAllLongSharksGenderIgnoringQuery()
        {
            var isGenderIgnoring = DataContext.SharkSpecies
                .Where(shark => shark.Length > 2)
                .Join(DataContext.SharkAttacks,
                    shark => shark.Id,
                    attack => attack.SharkSpeciesId,
                    (shark, attack) => new {attack.AttackedPersonId, shark})
                .Join(DataContext.AttackedPeople,
                    attack => attack.AttackedPersonId,
                    person => person.Id,
                    (attack, person) => new {person, attack.shark})
                .GroupBy(attackInfo => new {attackInfo.shark, attackInfo.person.Sex})
                .Select(info => new
                {
                    femaleInfo = info.Key.Sex == Sex.Female ? true : false,
                    maleInfo = info.Key.Sex == Sex.Male ? true : false
                })
                .Select(info => info.femaleInfo && info.maleInfo)
                .Aggregate((first, second) => first && second);

            return isGenderIgnoring;
        }

        /// <summary>
        /// Každý túži po prezývke a žralok nie je výnimkou. Keď na Vás pekne volajú, hneď Vám lepšie chutí. 
        /// Potrebujeme získať všetkých žralokov, ktorí nemajú prezývku(AlsoKnownAs) a k týmto žralokom krajinu v ktorej najviac útočili.
        /// Samozrejme to SFTW chce v podobe Dictionary, kde key bude názov žraloka a value názov krajiny.
        /// Len si predstavte tie rôznorodé prezývky, napr. Devil of Kyrgyzstan.
        /// </summary>
        /// <returns>The query result</returns>
        public Dictionary<string, string> SharksWithoutNickNameAndCountryWithMostAttacksQuery()
        {
            var sharksWithoutNickNameAndCountryWithMostAttacksQuery = DataContext.SharkSpecies
                .Where(shark => shark.AlsoKnownAs.Equals(""))
                .Join(DataContext.SharkAttacks,
                    shark => shark.Id,
                    attack => attack.SharkSpeciesId,
                    (shark, attack) => new {shark.Name, attack.CountryId, attack.SharkSpeciesId})
                .Join(DataContext.Countries,
                    attack => attack.CountryId,
                    country => country.Id,
                    (attack, country) => new {attack.Name, countryName = country.Name})
                .GroupBy(shark => shark.Name)
                .Select(shark => new
                {
                    shark.Key,
                    mostPopularCountry = shark
                        .GroupBy(x => x.countryName)
                        .OrderByDescending(x => x.Count()).First().Key
                })
                .ToDictionary(shark => shark.Key, 
                    shark => shark.mostPopularCountry);
            
                return sharksWithoutNickNameAndCountryWithMostAttacksQuery;
        }

        /// <summary>
        /// Ohúrili ste SFTW natoľko, že si u Vás objednali rovno textové výpisy. Samozrejme, že sa to dá zvladnúť pomocou LINQ. 
        /// Chcú aby ste pre všetky fatálne útoky v štátoch na písmenko 'D' a 'E', urobili výpis v podobe: 
        /// "{Meno obete} (iba ak sa začína na veľké písmeno) was attacked in {názov štátu} by {latinský názov žraloka}"
        /// Získané pole zoraďte abecedne a vraťte prvých 5 viet.
        /// </summary>
        /// <returns>The query result</returns>
        public List<string> InfoAboutPeopleAndCountriesOnDorEAndFatalAttacksQuery()
        {
            var infoAboutPeopleAndCountriesOnDorEAndFatalAttacksQuery = DataContext.AttackedPeople
                .Join(DataContext.SharkAttacks, 
                    person => person.Id, 
                    attack => attack.AttackedPersonId,
                    (person, attack) => new
                    {
                        person.Name, 
                        attack.SharkSpeciesId, 
                        attack.CountryId, 
                        attack.AttackSeverenity
                    })
                .Join(DataContext.Countries,
                    attack => attack.CountryId, 
                    country => country.Id,
                    (attack, country) => new
                    {
                        attack.Name, 
                        attack.SharkSpeciesId, 
                        countryName = country.Name, 
                        attack.AttackSeverenity
                    })
                .Join(DataContext.SharkSpecies,
                    attack => attack.SharkSpeciesId,
                    shark => shark.Id,
                    (attack, shark) => new
                    {
                        attack.Name, 
                        attack.countryName, 
                        shark.LatinName, 
                        attack.AttackSeverenity
                    })
                .Where(info => info.countryName!.FirstOrDefault().Equals('D')
                               || info.countryName!.FirstOrDefault().Equals('E') 
                               && info.AttackSeverenity == AttackSeverenity.Fatal)
                .ToList()
                .Where(person => char.IsUpper(person.Name.ToCharArray()[0]))
                .Select(info => info.Name 
                                + " was attacked in " + info.countryName + " by " + info.LatinName)
                .OrderBy(info => info)
                .Take(5)
                .ToList();

            return infoAboutPeopleAndCountriesOnDorEAndFatalAttacksQuery;
        }

        /// <summary>
        /// SFTW pretlačil nový zákon. Chce pokutovať štáty v Afrike.
        /// Každý z týchto štátov dostane pokutu za každý útok na ich území a to buď 250 meny danej krajiny alebo 300 meny danej krajiny (ak bol fatálny).
        /// Ak útok nebol preukázany ako fatal alebo non-fatal, štát za takýto útok nie je pokutovaný. Vyberte prvých 5 štátov s najvyššou pokutou.
        /// Vety budú zoradené zostupne podľa výšky pokuty.
        /// Opäť od Vás požadujú neštandardné formátovanie: "{Názov krajiny}: {Pokuta} {Mena danej krajiny}"
        /// Egypt: 10150 EGP
        /// Senegal: 2950 XOF
        /// Kenya: 2800 KES
        /// </summary>
        /// <returns>The query result</returns>
        public List<string> InfoAboutFinesOfAfricanCountriesTopFiveQuery()
        {
            var infoAboutFinesOfAfricanCountriesTopFiveQuery = DataContext.Countries
                .Where(country => country.Continent!.Equals("Africa"))
                .Join(DataContext.SharkAttacks,
                    country => country.Id,
                    attack => attack.CountryId,
                    (country, attack) => new
                    {
                        country.Name,
                        country.CurrencyCode,
                        attack.AttackSeverenity,
                        fee = attack.AttackSeverenity == AttackSeverenity.Fatal ? 300 :
                            attack.AttackSeverenity == AttackSeverenity.NonFatal ? 250 : 0
                    })
                .GroupBy(
                    group => new {group.Name, group.CurrencyCode},
                    group => group)
                .Select(country => new
                {
                    country.Key.Name, 
                    country.Key.CurrencyCode, 
                    fee = country.Sum(attack => attack.fee)
                })
                .OrderByDescending(country => country.fee)
                .Take(5)
                .Select(country => country.Name + ": " + country.fee + " " + country.CurrencyCode)
                .ToList();

            return infoAboutFinesOfAfricanCountriesTopFiveQuery;
        }

        /// <summary>
        /// CEO chce kandidovať na prezidenta celej planéty. Chce zistiť ako ma štylizovať svoju rétoriku aby zaujal čo najviac krajín.
        /// Preto od Vás chce, aby ste mu pomohli zistiť aké percentuálne zastúpenie majú jednotlivé typy vlád.
        /// Požaduje to ako jeden string: "{typ vlády}: {percentuálne zastúpenie}%, ...". 
        /// Výstup je potrebné mať zoradený, od najväčších percent po najmenšie a percentá sa budú zaokrúhľovať na jedno desatinné číslo.
        /// Pre zlúčenie použite Aggregate(..).
        /// </summary>
        /// <returns>The query result</returns>
        public string GovernmentTypePercentagesQuery()
        {
            var countries = DataContext.Countries
                .GroupBy(govForm => new {govForm.GovernmentForm})
                .Select(govForm => new {govForm.Key, precent = govForm.Count()})
                .ToList();
            var sum = countries.Sum(govForm => govForm.precent);
            var result = countries
                .Select(govForm => govForm.Key.GovernmentForm + ": " +
                                   String.Format("{0:0.0}",govForm.precent / (double) sum * 100))
                .ToList()
                .OrderByDescending(govForm => Double.Parse(govForm.Split(" ")[1]));
            return String.Join("%, ", result.ToArray()) + "%";
        }

        /// <summary>
        /// Oslovili nás surfisti. Chcú vedieť, či sú ako skupina viacej ohrození žralokmi. 
        /// Súrne potrebujeme vedieť koľko bolo fatálnych útokov na surfistov("surf", "Surf", "SURF") 
        /// a aký bol ich premierný vek(zaokrúliť na 2 desatinné miesta). 
        /// Zadávateľ úlohy nám to, ale skomplikoval. Tieto údaje chce pre každý kontinent.
        /// </summary>
        /// <returns>The query result</returns>
        public Dictionary<string, Tuple<int, double>> InfoForSurfersByContinentQuery()
        {
            var infoForSurfersByContinentQuery = DataContext.SharkAttacks
                .Join(DataContext.Countries,
                    attak => attak.CountryId,
                    country => country.Id,
                    (attack, country) => new
                    {
                        country.Continent, 
                        attack.AttackedPersonId, 
                        attack.Activity, 
                        attack.AttackSeverenity
                    })
                .Where(attack => attack.Activity!.ToUpper().Contains("surf".ToUpper()) 
                                 && attack.AttackSeverenity == AttackSeverenity.Fatal)
                .Join(DataContext.AttackedPeople,
                    attackData => attackData.AttackedPersonId,
                    person => person.Id,
                    (attackData, person) => new {attackData.Continent, person.Age})
                .GroupBy(attackData => attackData.Continent)
                .Select(attackData => new
                {
                    attackData.Key, 
                    allFatalCasesWithoutNull = attackData.Count(person => person.Age > 0), 
                    allFatalCases = attackData.Count(person => person.Age > 0 || person.Age == null), 
                    avarageAge = attackData.Sum(person => person.Age)
                })
                .Select(line => new
                {
                    line.Key, 
                    line.allFatalCases, 
                    avarageAge = line.avarageAge / (double) line.allFatalCasesWithoutNull
                })
                .ToDictionary(info => info.Key.ToString(),
                    info => new Tuple<int, double>(info.allFatalCases, Math.Round((double)info.avarageAge, 2)));
            
            return infoForSurfersByContinentQuery;
        }

        /// <summary>
        /// Zaujíma nás 10 najťažších žralokov na planéte a krajiny Severnej Ameriky. 
        /// CEO požaduje zoznam dvojíc, kde pre každý štát z danej množiny bude uvedený zoznam žralokov z danej množiny, ktorí v tom štáte útočili.
        /// Pokiaľ v nejakom štáte neútočil žiaden z najťažších žralokov, zoznam žralokov bude prázdny.
        /// SFTW požaduje prvých 5 položiek zoznamu dvojíc, zoradeného abecedne podľa mien štátov.

        /// </summary>
        /// <returns>The query result</returns>
        public List<Tuple<string, List<SharkSpecies>>> HeaviestSharksInNorthAmericaQuery()
        {
            var heavySharks = DataContext.SharkSpecies
                .OrderByDescending(species => species.Weight)
                .Take(10)
                .ToList();

            var attacksOfHeavySharks = DataContext.SharkAttacks
                .Join(heavySharks,
                    attack => attack.SharkSpeciesId,
                    species => species.Id,
                    (attack, species) => new {species, attack.CountryId})
                .Where(info =>info.CountryId != null)
                .Distinct()
                .GroupBy(info => info.CountryId)
                .ToDictionary(info => info.Key, info => info
                    .Select(attack => attack.species));
            
            var northAmericaCountriesAttacks = DataContext.Countries
                .Where(country => country.Continent == "North America")
                .Select(country => new Tuple<String, List<SharkSpecies>>(
                
                    country.Name, 
                    attacksOfHeavySharks.ContainsKey(country.Id) ? attacksOfHeavySharks[country.Id].ToList() : new List<SharkSpecies>()
                ))
                .OrderBy(info => info.Item1)
                .Take(5)
                .ToList();
            
            return northAmericaCountriesAttacks;
        }

        /// <summary>
        /// Zistite nám prosím všetky útoky spôsobené pri člnkovaní (attack type "Boating"), ktoré mal na vine žralok s prezývkou "White death". 
        /// Zaujímajú nás útoky z obdobia po 3.3.1960 (vrátane) a ľudia, ktorých meno začína na písmeno z intervalu <U, Z>.
        /// Výstup požadujeme ako zoznam mien zoradených abecedne.
        /// </summary>
        /// <returns>The query result</returns>
        public List<string> NonFatalAttemptOfWhiteDeathOnPeopleBetweenUAndZQuery()
        {
            var nonFatalAttemptOfWhiteDeathOnPeopleBetweenUAndZQuery = DataContext.SharkSpecies
                .Where(species => species.AlsoKnownAs.ToUpper().Equals("White death".ToUpper()))
                .Join(DataContext.SharkAttacks,
                    species => species.Id,
                    attack => attack.SharkSpeciesId,
                    (species, attack) => new
                    {
                        attack.Type, 
                        attack.AttackedPersonId, 
                        species.Name,
                        attack.DateTime
                    })
                .Where(info => info.Type == AttackType.Boating 
                               && info.AttackedPersonId != null 
                               && info.DateTime > new DateTime(1960, 3, 3))
                .Join(DataContext.AttackedPeople,
                    attack => attack.AttackedPersonId,
                    person => person.Id,
                    (attack, person) => person.Name)
                .Where(person => person.ToCharArray()[0] >= 117 && person.ToCharArray()[0] <= 122 
                                 || person.ToCharArray()[0] >= 85 && person.ToCharArray()[0] <= 90)
                .OrderBy(person => person)
                .ToList();

            return nonFatalAttemptOfWhiteDeathOnPeopleBetweenUAndZQuery;
        }

        /// <summary>
        /// Myslíme si, že rýchlejší žralok ma plnší žalúdok. 
        /// Požadujeme údaj o tom koľko percent útokov má na svedomí najrýchlejší a najpomalší žralok.
        /// Výstup požadujeme vo formáte: "{percentuálne zastúpenie najrýchlejšieho}% vs {percentuálne zastúpenie najpomalšieho}%"
        /// Perc. zastúpenie zaokrúhlite na jedno desatinné miesto.
        /// </summary>
        /// <returns>The query result</returns>
        public string FastestVsSlowestSharkQuery()
        {
            var sharkList = DataContext.SharkSpecies
                .Join(DataContext.SharkAttacks, 
                    species => species.Id,
                    attack => attack.SharkSpeciesId,
                    (species, attack) => new {species.TopSpeed, attack, species})
                .GroupBy(info => info.species)
                .Select(shark => new
                {
                    shark.Key.TopSpeed,
                    attaksOfOneShark = String.Format("{0:0.0}", (shark.Count() / (double) DataContext.SharkAttacks.Count() * 100))

                })
                .OrderByDescending(species => species.TopSpeed)
                .Where(shark => shark.TopSpeed != null)
                .ToList();

            return sharkList[0].attaksOfOneShark + "% vs " + sharkList[sharkList.Count-1].attaksOfOneShark + "%";
        }

        /// <summary>
        /// Prišla nám požiadavka z hora, aby sme im vrátili zoznam, 
        /// v ktorom je textová informácia o KAŽDOM človeku na ktorého zaútočil žralok v štáte Bahamas.
        /// Táto informácia je taktiež v tvare:
        /// {meno človeka} was attacked by {latinský názov žraloka}
        /// 
        /// Ale pozor váš nový nadriadený ma panický strach z operácie Join alebo GroupJoin.
        /// Nariadil vám použiť metódu Zip.
        /// Zistite teda tieto informácie bez spojenia hocijakých dvoch tabuliek a s použitím metódy Zip.
        /// </summary>
        /// <returns>The query result</returns>
        public List<string> AttackedPeopleInBahamasWithoutJoinQuery()
        {
            var countries = DataContext.Countries
                .Where(country => country.Name.Equals("Bahamas"));
            
            var bahamasId = countries.Select(country => country.Id).Take(1).Max();
            
            var attackListInBahamas = DataContext.SharkAttacks
                .Where(sharkAttack => sharkAttack.CountryId == bahamasId);
            
            var resultBahamasAttacks = attackListInBahamas
                .Zip(attackListInBahamas, (person, shark) => new
                {
                    person.AttackedPersonId, shark.SharkSpeciesId
                }).ToDictionary(person => 
                    person.AttackedPersonId, 
                    shark => shark.SharkSpeciesId);
            
            var attackedPeople = DataContext.AttackedPeople
                .Select(person => resultBahamasAttacks.ContainsKey(person.Id) ? person.Name : "")
                .Where(person => !person!.Equals(""));
            
            var sharkLatinNames = DataContext.SharkSpecies
                .Select(shark =>  resultBahamasAttacks.ContainsValue(shark.Id) ? shark.LatinName : "")
                .Where(shark => !shark!.Equals(""))
                .ToArray();
            
            var listOfPersonsNameAndSharkId = attackedPeople
                .Zip(resultBahamasAttacks, (person, attack) => new {person, attack.Value})
                .ToList();

            var resultList = listOfPersonsNameAndSharkId
                .Select(info => info.person + " was attacked by " + sharkLatinNames[info.Value -1])
                .ToList();
            
            return resultList;
        }

        
        /// <summary>
        /// Vráti počet útokov podľa mien žralokov, ktoré sa stali v Austrálii, vo formáte {meno žraloka}: {počet útokov}
        /// </summary>
        /// <returns>The query result</returns>
        public List<string> MostThreateningSharksInAustralia()
        {
            var mostThreateningSharksInAustralia = DataContext.SharkSpecies
                .Join(DataContext.SharkAttacks,
                    species => species.Id,
                    attack => attack.SharkSpeciesId,
                    (species, attack) => new {species.Name, attack, attack.CountryId})
                .Join(DataContext.Countries,
                    sharkInfo => sharkInfo.CountryId,
                    country => country.Id,
                    (sharkInfo, country) => new
                    {
                        countryName =country.Name, 
                        sharkName = sharkInfo.Name,
                    })
                .Where(country => country.countryName!.Equals("Australia"))
                .GroupBy(shark => shark.sharkName)
                .Select(sharkInfo => new
                {
                    sharkInfo.Key,
                    attacksAmount = sharkInfo.Count()

                })
                .OrderByDescending(shark => shark.attacksAmount)
                .Select(shark => shark.Key + ": " + shark.attacksAmount).ToList();
            return mostThreateningSharksInAustralia;
        }
    }
}
