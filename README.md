# StressLab - Narzędzie do Testów Wydajnościowych

StressLab to zaawansowane narzędzie do testów wydajnościowych napisane w C# (.NET 8) z wykorzystaniem nBomber. Narzędzie umożliwia testowanie wydajności API, procedur SQL oraz kombinowanych scenariuszy testowych.

## 🚀 Funkcjonalności

- **Testy API** - Testowanie wydajności endpointów HTTP/HTTPS
- **Testy SQL** - Testowanie wydajności procedur składowanych z monitorowaniem metryk bazy danych
- **Testy kombinowane** - Jednoczesne testowanie API i SQL w celu określenia wpływu na wydajność systemu
- **Raportowanie** - Generowanie raportów HTML, JSON i CSV
- **Integracja z TeamCity** - Automatyczne przekazywanie metryk do TeamCity
- **Monitorowanie systemu** - Zbieranie metryk CPU, pamięci, sieci podczas testów
- **Analiza wydajności** - Automatyczna ocena wpływu na wydajność systemu

## 📋 Wymagania

- .NET 8.0 SDK
- Windows/Linux/macOS
- Dostęp do testowanych API/baz danych

## 🛠️ Instalacja

1. Sklonuj repozytorium:
```bash
git clone <repository-url>
cd StressLab
```

2. Przywróć pakiety NuGet:
```bash
dotnet restore
```

3. Zbuduj rozwiązanie:
```bash
dotnet build
```

## 🎯 Użycie

### Uruchomienie podstawowego testu API

```bash
dotnet run --project tests/StressLab.PerformanceTests
```

### Uruchomienie z parametrami

```bash
dotnet run --project tests/StressLab.PerformanceTests -- \
  --name "My API Test" \
  --duration 120 \
  --users 25 \
  --endpoint "https://api.example.com/test" \
  --method "GET"
```

### Test SQL

```bash
dotnet run --project tests/StressLab.PerformanceTests -- \
  --name "SQL Performance Test" \
  --test-type Sql \
  --sql-connection "Server=localhost;Database=TestDB;Integrated Security=true;" \
  --sql-procedure "sp_GetTestData" \
  --duration 180 \
  --users 15
```

### Test kombinowany

```bash
dotnet run --project tests/StressLab.PerformanceTests -- \
  --name "Combined Test" \
  --test-type Combined \
  --endpoint "https://api.example.com/test" \
  --sql-connection "Server=localhost;Database=TestDB;Integrated Security=true;" \
  --sql-procedure "sp_GetTestData" \
  --duration 300 \
  --users 30
```

## ⚙️ Konfiguracja

### Plik appsettings.json

```json
{
  "TestConfiguration": {
    "Name": "API Performance Test",
    "Description": "Standard API performance test",
    "TestType": "Api",
    "DurationSeconds": 60,
    "ConcurrentUsers": 10,
    "RampUpSeconds": 10,
    "ApiEndpoint": "https://httpbin.org/get",
    "ApiMethod": "GET",
    "ExpectedResponseTimeMs": 1000,
    "MaxErrorRatePercent": 5.0
  }
}
```

### Parametry konfiguracyjne

| Parametr | Opis | Domyślna wartość |
|----------|------|------------------|
| `Name` | Nazwa testu | "API Performance Test" |
| `TestType` | Typ testu (Api/Sql/Combined) | "Api" |
| `DurationSeconds` | Czas trwania testu w sekundach | 60 |
| `ConcurrentUsers` | Liczba równoczesnych użytkowników | 10 |
| `RampUpSeconds` | Czas rozgrzewki w sekundach | 10 |
| `ApiEndpoint` | URL endpointu API | "https://httpbin.org/get" |
| `ApiMethod` | Metoda HTTP | "GET" |
| `SqlConnectionString` | Connection string do bazy danych | null |
| `SqlProcedureName` | Nazwa procedury składowanej | null |
| `ExpectedResponseTimeMs` | Oczekiwany czas odpowiedzi w ms | 1000 |
| `MaxErrorRatePercent` | Maksymalny dopuszczalny procent błędów | 5.0 |

## 📊 Raporty

Narzędzie generuje raporty w trzech formatach:

### HTML Report
- Interaktywny raport z wykresami i metrykami
- Ocena wpływu na wydajność
- Szczegółowe metryki systemowe
- Zalecenia optymalizacyjne

### JSON Report
- Strukturalne dane w formacie JSON
- Idealne do dalszego przetwarzania
- Zawiera wszystkie metryki testu

### CSV Report
- Dane w formacie CSV
- Łatwe do importu do Excel/Google Sheets
- Idealne do analizy trendów

## 🏗️ Integracja z TeamCity

Narzędzie automatycznie przekazuje metryki do TeamCity:

```
##teamcity[buildStatisticValue key='PerformanceTest.TotalRequests' value='1000']
##teamcity[buildStatisticValue key='PerformanceTest.AverageResponseTimeMs' value='250.5']
##teamcity[buildStatisticValue key='PerformanceTest.ErrorRatePercent' value='2.1']
```

## 📈 Metryki wydajności

### Metryki API
- **Total Requests** - Całkowita liczba żądań
- **Successful Requests** - Liczba udanych żądań
- **Failed Requests** - Liczba nieudanych żądań
- **Error Rate** - Procent błędów
- **Response Time** - Czas odpowiedzi (avg, min, max, P95, P99)
- **Throughput** - Liczba żądań na sekundę

### Metryki SQL
- **Execution Count** - Liczba wykonanych procedur
- **Execution Time** - Czas wykonania procedury
- **Error Count** - Liczba błędów SQL
- **Active Connections** - Liczba aktywnych połączeń
- **Deadlock Count** - Liczba deadlocków
- **Database CPU/Memory Usage** - Wykorzystanie zasobów bazy

### Metryki systemowe
- **CPU Usage** - Wykorzystanie procesora
- **Memory Usage** - Wykorzystanie pamięci
- **Disk Usage** - Wykorzystanie dysku
- **Network Traffic** - Ruch sieciowy

## 🎯 Ocena wpływu na wydajność

Narzędzie automatycznie ocenia wpływ na wydajność:

- **None** - Brak znaczącego wpływu
- **Minor** - Drobny wpływ
- **Moderate** - Umiarkowany wpływ
- **Major** - Znaczny wpływ
- **Critical** - Krytyczny wpływ

## 🔧 Rozszerzanie funkcjonalności

### Dodawanie nowych typów testów

1. Rozszerz enum `TestType` w `StressLab.Core.Enums`
2. Dodaj implementację w `PerformanceTestService`
3. Zaktualizuj walidatory

### Dodawanie nowych metryk

1. Rozszerz `TestResult` entity
2. Zaktualizuj `SystemMetricsService`
3. Dodaj do raportów HTML/JSON/CSV

## 📝 Przykłady użycia

### Przykład 1: Test API z wysokim obciążeniem

```bash
dotnet run --project tests/StressLab.PerformanceTests -- \
  --name "High Load API Test" \
  --duration 300 \
  --users 100 \
  --endpoint "https://api.example.com/heavy-endpoint" \
  --method "POST"
```

### Przykład 2: Test SQL z długotrwałymi procedurami

```bash
dotnet run --project tests/StressLab.PerformanceTests -- \
  --name "Long Running SQL Test" \
  --test-type Sql \
  --sql-connection "Server=prod-db;Database=Analytics;Integrated Security=true;" \
  --sql-procedure "sp_GenerateReport" \
  --duration 600 \
  --users 5
```

### Przykład 3: Test kombinowany dla oceny wpływu

```bash
dotnet run --project tests/StressLab.PerformanceTests -- \
  --name "System Impact Assessment" \
  --test-type Combined \
  --endpoint "https://api.example.com/data" \
  --sql-connection "Server=localhost;Database=TestDB;Integrated Security=true;" \
  --sql-procedure "sp_ProcessData" \
  --duration 180 \
  --users 20
```

## 🐛 Rozwiązywanie problemów

### Problem: Błędy połączenia z bazą danych
**Rozwiązanie:** Sprawdź connection string i dostępność bazy danych

### Problem: Wysokie wykorzystanie CPU podczas testów
**Rozwiązanie:** Zmniejsz liczbę równoczesnych użytkowników lub czas trwania testu

### Problem: Błędy HTTP podczas testów API
**Rozwiązanie:** Sprawdź dostępność endpointu i poprawność URL

## 📞 Wsparcie

W przypadku problemów lub pytań:
1. Sprawdź logi w katalogu `logs/`
2. Sprawdź raporty w katalogu `reports/`
3. Skontaktuj się z zespołem deweloperskim

## 📄 Licencja

Ten projekt jest licencjonowany na licencji MIT - zobacz plik [LICENSE](LICENSE) dla szczegółów.

