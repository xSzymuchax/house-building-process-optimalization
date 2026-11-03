# Optymalizacja procesu budowy domu

Aplikacja desktopowa WPF (.NET 8, Windows) dobiera harmonogram zadań budowlanych. Na wejściu jest graf zadań, zasoby i kanały kontroli. Algorytm ewolucyjny szuka przydziału, który ogranicza czas i koszt realizacji.

Graf jest rysowany biblioteką MSAGL. Po zakończeniu obliczeń wynik trafia do pliku w katalogu `RESULTS`.

## Dane wejściowe

Plik tekstowy wczytuje się przyciskiem **Wczytaj Plik...**. Sekcje rozdziela znak `@`. Parser składa białe znaki w jeden ciąg i czyta kolejne tokeny.

| Sekcja | Zawartość |
| --- | --- |
| `@tasks` | Liczba zadań, potem każde zadanie: nazwa, liczba następników i następniki w postaci `NAZWA(etykieta)`. Etykieta krawędzi jest wielkością przekazywaną kanałem kontroli. |
| `@proc` | Liczba zasobów, potem koszt, dostępność i typ. Dostępność jest pomijana. Typ `0` to zasób dedykowany, `1` to zasób ogólny. |
| `@times` | Czasy wykonania: dla każdego zadania po jednym czasie na każdy zasób. Wartość `-1` oznacza, że zasób nie wykonuje tego zadania. |
| `@cost` | Koszty wykonania w tym samym układzie co czasy. |
| `@comm` | Kanały kontroli: nazwa, koszt doliczany raz, przepustowość oraz uprawnienie (`0` albo `1`) do każdego zasobu. Czas kontroli wynika z etykiety krawędzi podzielonej przez przepustowość. |

Prefiks nazwy zadania określa jego typ:

| Prefiks | Typ | Kto może wykonać | Wiele zasobów naraz |
| --- | --- | --- | --- |
| `DT` | zadanie dedykowane | zasób dedykowany | nie |
| `GT` | zadanie ogólne | dedykowany albo ogólny | nie |
| `UT` | zadanie uniwersalne | zasób ogólny | nie |
| `CDT` | wspólne dedykowane | zasób dedykowany | tak |
| `CGT` | wspólne ogólne | dedykowany albo ogólny | tak |

Każdy kolejny zasób przy zadaniu wielozasobowym mnoży czas pracy przez `0,7`.

## Generator danych wejściowych

`TaskResourcesGenerator.ipynb` przygotowuje dane wejściowe dla aplikacji. Notatnik w Pythonie (NumPy) losuje graf zadań, zasoby, czasy, koszty i kanały kontroli, a potem zapisuje je w pliku `GENERATED_GRAPH_<znacznik_czasu>.txt`. Ten plik wczytuje się w oknie programu.

Rozmiar instancji i przedziały liczb ustawiają stałe na końcu notatnika: liczba zadań, zasobów i kanałów, wagi typów zadań oraz zakresy cen, czasów, kosztów, etykiet krawędzi i przepustowości. Każde zadanie poza pierwszym dostaje poprzednika wśród co najwyżej dziesięciu wcześniejszych zadań, a dodatkowe krawędzie dochodzą z prawdopodobieństwem `0,02`. Około 30% zasobów jest ogólnych, reszta dedykowana. Czas i koszt wynoszą `-1` tam, gdzie dany typ zasobu nie może wykonać zadania. Każdy zasób ma uprawnienie przynajmniej do jednego kanału.

## Algorytm

1. Z grafu powstaje drzewo rozpinające przechodzone wszerz. Mniejsza głębokość pozwala wykonywać więcej zadań równolegle.
2. Populacja ma rozmiar `liczba zadań × liczba zasobów × α`.
3. Każdy osobnik dostaje strategie przydziału zasobów i po jednej instancji każdego kanału kontroli.
4. W iteracji osobniki są oceniane, a potem wybierane do krzyżowania (`β`), mutacji (`γ`) i klonowania (`δ`). Najlepszy osobnik przechodzi dalej bez zmian. Lepszy wynik ma większą szansę wyboru.
5. Krzyżowanie zamienia między rodzicami poddrzewo wylosowanego zadania. Mutacja zmienia liczbę albo strategię zasobów oraz, z osobnym prawdopodobieństwem, liczbę kontrolerów.
6. Pętla kończy się po zadanej liczbie iteracji albo po zadanej liczbie iteracji bez poprawy.

Strategie zasobów ogólnych to między innymi nowy najszybszy, nowy najtańszy, nowy o najlepszym iloczynie czas–koszt, już użyty najszybszy lub najtańszy, najszybciej wolny, najrzadziej alokowany oraz taki sam jak u rodzica. Zasoby dedykowane korzystają z węższego zestawu: nowy najszybszy, nowy najtańszy, nowy czas–koszt i taki sam jak u rodzica.

Ocena osobnika to `Kw × koszt + Cw × czas`. Gdy włączony jest limit, koszt i czas są dzielone przez ten limit. Przekroczenie limitu dokłada karę kwadratową zależną od parametru **Kara** oraz dużą stałą karę. Przed startem musi być włączony limit czasu, limit kosztu albo oba.

## Uruchomienie

Wymagany jest .NET 8 z obsługą aplikacji Windows.

```
dotnet run --project PracaMagisterska
```

Albo otwórz `PracaMagisterska.sln` w Visual Studio i uruchom projekt. Po **Rozpocznij** program liczy harmonogram, pokazuje graf i zapisuje `RESULTS/RESULT_<znacznik_czasu>_<numer>.txt`.
