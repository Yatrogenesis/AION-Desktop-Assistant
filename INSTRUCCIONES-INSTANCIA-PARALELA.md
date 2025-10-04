# Instrucciones para Instancia Paralela Claude Code

## Ubicación del Proyecto
**Ruta principal**: `D:\AION-Desktop-Assistant`
**Acceso remoto**: `\\D3S1GN01\D\AION-Desktop-Assistant`

## Contexto de Auditoría

Este proyecto fue objeto de auditoría el 2025-10-02. Se identificaron y corrigieron gaps críticos:
- README profesionalizado (emojis eliminados, lenguaje técnico)
- Testing infrastructure creada (xUnit + Moq + FluentAssertions)
- CI/CD configurado (GitHub Actions)
- Matriz compatibilidad documentada

**Estado actual**: 70% completado. Requiere .NET SDK para finalizar.

## Tareas Pendientes (Prioridad Alta)

### 1. Completar Tests Unitarios
```bash
cd D:\AION-Desktop-Assistant
dotnet test --verbosity normal
```

**Tests faltantes** (7 Services):
- `ScreenCaptureServiceTests.cs`
- `VoiceRecognitionServiceTests.cs`
- `VoiceSynthesisServiceTests.cs`
- `MouseAutomationServiceTests.cs`
- `KeyboardAutomationServiceTests.cs`
- `WindowManagementServiceTests.cs`
- `AccessibilityServiceTests.cs`

**Plantilla test** (usar `tests/OcrServiceTests.cs` como referencia):
```csharp
using Xunit;
using Moq;
using FluentAssertions;
using AionDesktopAssistant.Services;

namespace AionDesktopAssistant.Tests
{
    public class [ServiceName]Tests
    {
        private readonly [ServiceName] _sut;

        public [ServiceName]Tests()
        {
            _sut = new [ServiceName]();
        }

        [Fact]
        public void [Method]_With[Scenario]_Returns[Expected]()
        {
            // Arrange

            // Act

            // Assert
            result.Should().NotBeNull();
        }
    }
}
```

### 2. Integrar Serilog (Logging Estructurado)

**Agregar a `AionDesktopAssistant.csproj`**:
```xml
<PackageReference Include="Serilog" Version="3.1.1" />
<PackageReference Include="Serilog.Extensions.Hosting" Version="8.0.0" />
<PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />
<PackageReference Include="Serilog.Sinks.Console" Version="5.0.1" />
<PackageReference Include="Serilog.Formatting.Compact" Version="2.0.0" />
```

**Configurar en `App.xaml.cs`**:
```csharp
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(
        "logs/aion-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();
```

**Agregar logging en Services** (ejemplo):
```csharp
using Serilog;

public class OcrService
{
    private readonly ILogger _logger = Log.ForContext<OcrService>();

    public string ExtractText(Bitmap image)
    {
        _logger.Information("Starting OCR extraction, image size: {Width}x{Height}",
            image.Width, image.Height);

        try
        {
            // ... código existente ...
            _logger.Information("OCR extraction completed, text length: {Length}", result.Length);
            return result;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "OCR extraction failed");
            throw;
        }
    }
}
```

### 3. Build y Verificación

```bash
# Restaurar dependencias
dotnet restore

# Build debug
dotnet build --configuration Debug

# Build release
dotnet build --configuration Release

# Ejecutar tests con coverage
dotnet test --collect:"XPlat Code Coverage"

# Publicar ejecutables
dotnet publish --configuration Release --runtime win-x64 --self-contained true /p:PublishSingleFile=true
dotnet publish --configuration Release --runtime win-arm64 --self-contained true /p:PublishSingleFile=true
```

### 4. Verificar CI/CD

```bash
# Verificar workflow configurado
cat .github/workflows/ci-cd.yml

# Ejecutar build local similar a CI
dotnet build --configuration Release
dotnet test --configuration Release --verbosity normal
```

## Comandos Git Útiles

```bash
cd D:\AION-Desktop-Assistant

# Ver estado actual
git status

# Ver último commit
git log -1 --stat

# Ver diferencias
git diff HEAD~1

# Agregar cambios
git add .

# Commit con mensaje profesional
git commit -m "feat(testing): Complete unit tests for all Services

- Implemented tests for 7 remaining Services
- Added Serilog structured logging
- Integrated logging in all Service classes
- Coverage increased to 85%+

Generated with Claude Code
Co-Authored-By: Claude <noreply@anthropic.com>"

# Push
git push origin master
```

## Estructura del Proyecto

```
D:\AION-Desktop-Assistant\
├── .github/
│   └── workflows/
│       └── ci-cd.yml          # CI/CD pipeline (completado)
├── docs/
│   └── COMPATIBILITY-MATRIX.md # Matriz compatibilidad (completado)
├── tests/
│   ├── AionDesktopAssistant.Tests.csproj  # Proyecto tests (completado)
│   ├── OcrServiceTests.cs                 # Test ejemplo (completado)
│   └── [7 tests pendientes]               # PENDIENTE
├── Services/
│   ├── OcrService.cs
│   ├── ScreenCaptureService.cs
│   ├── VoiceRecognitionService.cs
│   ├── VoiceSynthesisService.cs
│   ├── MouseAutomationService.cs
│   ├── KeyboardAutomationService.cs
│   ├── WindowManagementService.cs
│   └── AccessibilityService.cs
├── AionDesktopAssistant.csproj
├── README.md                  # Profesionalizado (completado)
└── INSTRUCCIONES-INSTANCIA-PARALELA.md (este archivo)
```

## Objetivos de Completitud

### Inmediatos (esta sesión)
- [ ] Tests unitarios 7 Services (coverage >80%)
- [ ] Serilog integrado en todos los Services
- [ ] Logs estructurados JSON para trazabilidad
- [ ] Build verificado localmente
- [ ] Tests pasando al 100%

### Corto plazo (siguiente sesión)
- [ ] Referencias académicas con DOI
- [ ] GitHub Pages configurado
- [ ] Documentación usuario final
- [ ] Validación WCAG externa planificada

### Métricas Éxito
- **Test Coverage**: >80% (actual ~10%)
- **Build Success**: 100% (configurado, no verificado)
- **Logging**: Completo en todos Services (0%)
- **Documentation**: Professional (90%)

## Notas Importantes

1. **No emojis**: Código, commits, documentación deben ser profesionales
2. **Referencias verificables**: URLs técnicas válidas, DOI para papers
3. **Commits estructurados**: Conventional Commits format
4. **Coverage obligatorio**: Mínimo 80% antes de merge

## Contacto Auditoría

Documentos relacionados en `D:\AUDIT-2025-10-02\`:
- `PROGRESO-AUDITORIA.md` - Estado detallado
- `RESUMEN-EJECUTIVO-AUDITORIA.md` - Resumen ejecutivo
- `CONTEXTO-INICIAL.md` - Contexto completo

## Recursos Técnicos

### .NET SDK
```bash
# Verificar versión
dotnet --version  # Requerido: 8.0.100+

# Si no está instalado
# Descargar desde: https://dotnet.microsoft.com/download/dotnet/8.0
```

### Testing
- xUnit: https://xunit.net/
- Moq: https://github.com/moq/moq4
- FluentAssertions: https://fluentassertions.com/

### Logging
- Serilog: https://serilog.net/
- Structured Logging: https://github.com/serilog/serilog/wiki/Structured-Data

### CI/CD
- GitHub Actions: https://docs.github.com/en/actions
- Codecov: https://about.codecov.io/

---

**Última actualización**: 2025-10-02
**Estado proyecto**: 70% completado
**Próximo milestone**: Testing completo + Logging estructurado
