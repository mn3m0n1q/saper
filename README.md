# Saper

Polskojęzyczna gra w ASP.NET Core / .NET 10 LTS, bez zewnętrznych pakietów NuGet. Logika i losowanie min działają na serwerze; aktywna gra nie ujawnia klientowi lokalizacji min.

## Lokalnie

Zainstaluj SDK .NET 10, a następnie w katalogu projektu:

```sh
dotnet run --urls http://localhost:5080
```

Otwórz http://localhost:5080. Gra oferuje poziomy 9×9/10 min, 16×16/40 min i 30×16/99 min. Pierwsze pole i jego sąsiedztwo są bezpieczne. Prawy przycisk lub tryb flagowania oznacza pola. Strzałki, Enter i F umożliwiają obsługę klawiaturą. Duże plansze przewijają się poziomo na telefonie.

## VPS i domena

Potrzebny Linux z Docker Engine i wtyczką Docker Compose. Skopiuj projekt na serwer.

1. Ustaw rekord DNS A wybranej domeny na publiczny IPv4 VPS. Rekord AAAA dodaj tylko przy działającym IPv6.
2. Udostępnij porty TCP 80 i 443 w firewallu VPS i panelu dostawcy; opcjonalnie UDP 443 dla HTTP/3. Zachowaj dostęp SSH.
3. Skopiuj `.env.example` do `.env` i ustaw `DOMAIN` na samą nazwę domeny (bez `https://`).
4. Uruchom `docker compose up -d --build`.
5. Wejdź na `https://twoja-domena`. Caddy automatycznie uzyska i odnowi certyfikat TLS po poprawnym ustawieniu DNS i portów.

Diagnostyka: `docker compose logs --tail=100`, `docker compose ps`. Aktualizacja: skopiuj nowe źródła i ponów `docker compose up -d --build`. Zatrzymanie: `docker compose down`. Nie usuwaj wolumenu `caddy_data`, aby zachować certyfikaty. Backend jest dostępny tylko w sieci Compose; HTTPS kończy się w Caddy.

## Weryfikacja

```sh
dotnet build -c Release
dotnet run --project tests/GameTests.csproj -c Release
python3 tests/smoke.py http://localhost:5080
```

Test HTTP wymaga uruchomionej aplikacji. Testy obejmują poziomy, walidację, bezpieczny start, flagowanie, przegraną, wygraną i brak ujawniania min przed zakończeniem. Plik package.json służy wyłącznie podglądowi w środowisku roboczym; lokalnie i na VPS nie jest potrzebny Node.js.

## Zakres pierwszej wersji

Bez kont, rankingu i bazy danych. Gry są przechowywane w pamięci, wygasają po 2 godzinach bezczynności i znikają przy restarcie procesu lub odświeżeniu strony (przeglądarka rozpoczyna nową partię). Limit pamięci cache to 5000 gier; wyparcie może zakończyć starszą grę wcześniej. Wdrożenie przewiduje jedną instancję aplikacji. Limit API wynosi 600 żądań/minutę łącznie na instancję; przy większym ruchu należy dostroić go i dodać limity per klient na reverse proxy. Endpoint `/health` służy do sprawdzania dostępności.
