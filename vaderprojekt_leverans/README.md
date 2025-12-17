# Väderprojekt (Databashantering SQL)

Detta repo innehåller en konsolbaserad applikation som läser **temperatur- och luftfuktighetsdata** från `TempFuktData.csv`, sparar i en databas (Entity Framework **Code First**) och gör sökningar/sorteringar/slutsatser enligt projektkraven.

## Struktur (3 projekt)
- **VaderProjekt.Core** – all analyslogik (LINQ-beräkningar)
- **VaderProjekt.DataAccess** – databaskontext + inläsning av CSV
- **VaderProjekt.UI** – konsolgränssnitt (`Program.cs`)

## K/process 
- Databasen skapas automatiskt första gången med `DatabaseFacade.EnsureCreated()`.
- SQLite används (ingen SQL Server krävs).
- Om tabellen är tom läses CSV in och databasen fylls.

## Körning
1. Öppna `VaderProjekt.sln` i Visual Studio.
2. Sätt **VaderProjekt.UI** som Startup Project.
3. Kör.

`TempFuktData.csv` ligger i **VäderProjekt.UI** och är konfigurerad att kopieras till output.

## Reset av databas
Om du vill köra om från start: ta bort filen `vaderprojekt.sqlite` som skapas i samma mapp som exe-filen (bin-katalogen).

## extra-funktioner
I menyn finns två extra val:
- Dagar då inne/ute skiljer sig **mest/minst** (medel av |Inne-Ute| per minut)
- Uppskattning av hur länge **balkongdörren** står öppen per dag (heuristik baserat på snabb ändring inne/ute)

## Loggbok & reflektion
- `Loggbok` – fyll i vad du gjort dag för dag.
- `Reflektion` – personlig reflektion .
