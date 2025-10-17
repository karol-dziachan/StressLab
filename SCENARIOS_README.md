# StressLab - System Konfiguracji JSON dla Scenariuszy Testowych

## 🎯 **Nowa Funkcjonalność: Konfiguracja JSON**

StressLab teraz obsługuje zaawansowaną konfigurację scenariuszy testowych przez pliki JSON, co pozwala na:

- **Definiowanie złożonych scenariuszy** z wieloma krokami
- **Różne tryby wykonania** (Parallel, Sequential, Grouped, Weighted)
- **Kombinowanie różnych typów testów** (API + SQL + Wait + Custom)
- **Elastyczne zarządzanie obciążeniem** (ConstantRate, RampUp, Spike, Stress, Soak)
- **Konfigurację parametrów** dla każdego kroku

## 📋 **Struktura Konfiguracji JSON**

### **Główna Struktura**

```json
{
  "TestScenarios": [
    {
      "Name": "Nazwa Scenariusza",
      "Description": "Opis scenariusza",
      "ExecutionMode": "Parallel|Sequential|Grouped|Weighted",
      "LoadSimulation": { ... },
      "Steps": [ ... ],
      "Settings": { ... }
    }
  ],
  "GlobalSettings": { ... }
}
```

### **Tryby Wykonania (ExecutionMode)**

1. **Parallel** - Wszystkie kroki wykonują się równolegle
2. **Sequential** - Kroki wykonują się sekwencyjnie
3. **Grouped** - Kroki grupują się według typu i wykonują równolegle w grupach
4. **Weighted** - Kroki wykonują się z wagą (proporcjonalnie do Weight)

### **Typy Kroków (StepType)**

- **HttpApi** - Wywołania API HTTP/HTTPS
- **SqlProcedure** - Wykonywanie procedur składowanych
- **SqlQuery** - Wykonywanie zapytań SQL
- **Wait** - Opóźnienia między krokami
- **DatabaseConnection** - Test połączenia z bazą danych
- **CustomScript** - Wykonywanie niestandardowych skryptów
- **FileOperation** - Operacje na plikach

### **Typy Symulacji Obciążenia (LoadSimulationType)**

- **ConstantRate** - Stała liczba żądań na sekundę
- **RampUp** - Stopniowe zwiększanie obciążenia
- **Spike** - Testy z nagłymi skokami obciążenia
- **Stress** - Testy stresowe z maksymalnym obciążeniem
- **Soak** - Testy długotrwałe (soak testing)

## 🚀 **Przykłady Użycia**

### **1. Test API z wieloma endpointami (Parallel)**

```json
{
  "Name": "API Multi-Endpoint Test",
  "ExecutionMode": "Parallel",
  "LoadSimulation": {
    "Type": "ConstantRate",
    "Rate": 20,
    "DurationSeconds": 60,
    "RampUpSeconds": 10
  },
  "Steps": [
    {
      "Name": "Get Users",
      "Type": "HttpApi",
      "Weight": 3,
      "Configuration": {
        "Method": "GET",
        "Url": "https://api.example.com/users",
        "Headers": {
          "Accept": "application/json"
        }
      }
    },
    {
      "Name": "Create User",
      "Type": "HttpApi",
      "Weight": 1,
      "Configuration": {
        "Method": "POST",
        "Url": "https://api.example.com/users",
        "Headers": {
          "Content-Type": "application/json"
        },
        "Body": {
          "name": "Test User",
          "email": "test@example.com"
        }
      }
    }
  ]
}
```

### **2. Test SQL z wieloma procedurami (Parallel)**

```json
{
  "Name": "SQL Multi-Procedure Test",
  "ExecutionMode": "Parallel",
  "LoadSimulation": {
    "Type": "RampUp",
    "Rate": 15,
    "DurationSeconds": 120,
    "RampUpSeconds": 20
  },
  "Steps": [
    {
      "Name": "Get User Data",
      "Type": "SqlProcedure",
      "Weight": 2,
      "Configuration": {
        "ConnectionString": "Server=localhost;Database=TestDB;Integrated Security=true;",
        "ProcedureName": "sp_GetUserData",
        "Parameters": {
          "@UserId": 123,
          "@IncludeDetails": true
        }
      }
    },
    {
      "Name": "Update User Status",
      "Type": "SqlProcedure",
      "Weight": 1,
      "Configuration": {
        "ConnectionString": "Server=localhost;Database=TestDB;Integrated Security=true;",
        "ProcedureName": "sp_UpdateUserStatus",
        "Parameters": {
          "@UserId": 123,
          "@Status": "Active"
        }
      }
    }
  ]
}
```

### **3. Test Kombinowany API + SQL (Parallel)**

```json
{
  "Name": "Combined API and SQL Test",
  "ExecutionMode": "Parallel",
  "LoadSimulation": {
    "Type": "Spike",
    "Rate": 25,
    "DurationSeconds": 180,
    "RampUpSeconds": 30,
    "Parameters": {
      "SpikeDuration": 60,
      "SpikeRate": 50
    }
  },
  "Steps": [
    {
      "Name": "API Get Data",
      "Type": "HttpApi",
      "Weight": 2,
      "Configuration": {
        "Method": "GET",
        "Url": "https://api.example.com/data"
      }
    },
    {
      "Name": "SQL Process Data",
      "Type": "SqlProcedure",
      "Weight": 1,
      "Configuration": {
        "ConnectionString": "Server=localhost;Database=TestDB;Integrated Security=true;",
        "ProcedureName": "sp_ProcessData",
        "Parameters": {
          "@DataId": 456,
          "@ProcessType": "Analysis"
        }
      }
    },
    {
      "Name": "Wait Between Operations",
      "Type": "Wait",
      "Weight": 1,
      "Configuration": {
        "DurationMs": 100,
        "RandomVariation": 50
      }
    }
  ]
}
```

### **4. Test Sekwencyjny (Sequential)**

```json
{
  "Name": "Sequential Workflow Test",
  "ExecutionMode": "Sequential",
  "LoadSimulation": {
    "Type": "Soak",
    "Rate": 10,
    "DurationSeconds": 300,
    "RampUpSeconds": 15
  },
  "Steps": [
    {
      "Name": "Step 1: Authenticate",
      "Type": "HttpApi",
      "Configuration": {
        "Method": "POST",
        "Url": "https://api.example.com/auth",
        "Body": {
          "username": "testuser",
          "password": "testpass"
        }
      }
    },
    {
      "Name": "Step 2: Get Profile",
      "Type": "HttpApi",
      "Configuration": {
        "Method": "GET",
        "Url": "https://api.example.com/profile",
        "Headers": {
          "Authorization": "Bearer {{token}}"
        }
      }
    },
    {
      "Name": "Step 3: Update Profile",
      "Type": "HttpApi",
      "Configuration": {
        "Method": "PUT",
        "Url": "https://api.example.com/profile",
        "Headers": {
          "Authorization": "Bearer {{token}}",
          "Content-Type": "application/json"
        },
        "Body": {
          "name": "Updated Name"
        }
      }
    }
  ]
}
```

### **5. Test z Ważeniem (Weighted)**

```json
{
  "Name": "Weighted Distribution Test",
  "ExecutionMode": "Weighted",
  "LoadSimulation": {
    "Type": "ConstantRate",
    "Rate": 30,
    "DurationSeconds": 90
  },
  "Steps": [
    {
      "Name": "Heavy Operation",
      "Type": "SqlProcedure",
      "Weight": 1,
      "Configuration": {
        "ConnectionString": "Server=localhost;Database=TestDB;Integrated Security=true;",
        "ProcedureName": "sp_HeavyOperation"
      }
    },
    {
      "Name": "Light Operation",
      "Type": "HttpApi",
      "Weight": 5,
      "Configuration": {
        "Method": "GET",
        "Url": "https://api.example.com/light"
      }
    },
    {
      "Name": "Medium Operation",
      "Type": "HttpApi",
      "Weight": 3,
      "Configuration": {
        "Method": "POST",
        "Url": "https://api.example.com/medium"
      }
    }
  ]
}
```

## 🎮 **Uruchamianie Scenariuszy**

### **Z wiersza poleceń:**

```bash
# Uruchomienie scenariusza po nazwie
dotnet run --project tests/StressLab.PerformanceTests -- --scenario "API Multi-Endpoint Test"

# Uruchomienie scenariusza z parametrami
dotnet run --project tests/StressLab.PerformanceTests -- --scenario "Combined API and SQL Test" --duration 300

# Lista dostępnych scenariuszy
dotnet run --project tests/StressLab.PerformanceTests -- --list-scenarios
```

### **Z pliku konfiguracyjnego:**

```json
{
  "TestConfiguration": {
    "ScenarioName": "API Multi-Endpoint Test"
  }
}
```

## ⚙️ **Zaawansowane Konfiguracje**

### **Parametry Kroków HTTP API:**

```json
{
  "Configuration": {
    "Method": "GET|POST|PUT|DELETE|PATCH",
    "Url": "https://api.example.com/endpoint",
    "Headers": {
      "Accept": "application/json",
      "Authorization": "Bearer token",
      "Content-Type": "application/json"
    },
    "Body": {
      "key": "value"
    },
    "Timeout": 30000,
    "ExpectedStatusCode": 200
  }
}
```

### **Parametry Kroków SQL:**

```json
{
  "Configuration": {
    "ConnectionString": "Server=localhost;Database=TestDB;Integrated Security=true;",
    "ProcedureName": "sp_ProcedureName",
    "Parameters": {
      "@Param1": "value1",
      "@Param2": 123,
      "@Param3": true
    },
    "CommandTimeout": 30,
    "ExpectedExecutionTime": 1000
  }
}
```

### **Parametry Kroków Wait:**

```json
{
  "Configuration": {
    "DurationMs": 100,
    "RandomVariation": 50
  }
}
```

### **Ustawienia Globalne:**

```json
{
  "GlobalSettings": {
    "DefaultTimeout": 30000,
    "DefaultRetryCount": 3,
    "DefaultRetryDelay": 1000,
    "MetricsCollectionInterval": 1000,
    "ReportGeneration": {
      "Html": true,
      "Json": true,
      "Csv": true
    },
    "PerformanceThresholds": {
      "MaxResponseTimeMs": 2000,
      "MaxErrorRatePercent": 5.0,
      "MinThroughputPerSecond": 10
    }
  }
}
```

## 🔧 **Dostosowywanie Scenariuszy**

### **Zmienne w Konfiguracji:**

```json
{
  "Configuration": {
    "Url": "https://api.example.com/users/{{userId}}",
    "Headers": {
      "Authorization": "Bearer {{token}}"
    }
  }
}
```

### **Warunkowe Wykonywanie:**

```json
{
  "Name": "Conditional Step",
  "Enabled": true,
  "Settings": {
    "Condition": "{{environment}} == 'production'"
  }
}
```

### **Grupowanie Kroków:**

```json
{
  "ExecutionMode": "Grouped",
  "Steps": [
    {
      "Name": "API Group 1",
      "Type": "HttpApi",
      "Settings": {
        "Group": "api"
      }
    },
    {
      "Name": "SQL Group 1", 
      "Type": "SqlProcedure",
      "Settings": {
        "Group": "sql"
      }
    }
  ]
}
```

## 📊 **Monitorowanie i Raportowanie**

### **Metryki Specjalne dla Scenariuszy:**

- **Per-Step Metrics** - metryki dla każdego kroku osobno
- **Execution Mode Impact** - wpływ trybu wykonania na wydajność
- **Step Dependencies** - analiza zależności między krokami
- **Weight Distribution** - analiza rozkładu obciążenia według wag

### **Raporty HTML z Detalami Scenariuszy:**

- Wykresy wydajności dla każdego kroku
- Analiza trybu wykonania
- Porównanie kroków równoległych vs sekwencyjnych
- Identyfikacja wąskich gardeł w scenariuszu

## 🎯 **Najlepsze Praktyki**

1. **Używaj Parallel** dla niezależnych operacji
2. **Używaj Sequential** dla operacji z zależnościami
3. **Używaj Weighted** dla różnych typów obciążenia
4. **Dodawaj kroki Wait** między operacjami SQL i API
5. **Konfiguruj odpowiednie timeouty** dla każdego kroku
6. **Używaj zmiennych** dla elastyczności konfiguracji
7. **Testuj scenariusze** w różnych środowiskach

Ten system pozwala na tworzenie bardzo zaawansowanych scenariuszy testowych, które mogą dokładnie symulować rzeczywiste obciążenie systemu i mierzyć wpływ nowych rozwiązań na wydajność.

