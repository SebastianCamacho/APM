$ErrorActionPreference = "Stop"

Write-Host "Iniciando publicación del Worker Service..." -ForegroundColor Cyan

# Definir la ruta de salida
$outputDir = "./publish/windows/worker"

# Limpiar publicación anterior si existe
if (Test-Path $outputDir) {
    Write-Host "Limpiando directorio de salida anterior..." -ForegroundColor Yellow
    Remove-Item $outputDir -Recurse -Force
}

# Ejecutar el comando de publicación con la propiedad IsWorkerBuild definida
# Esto asegura que los targets de Android se ignoren en los proyectos compartidos
Write-Host "Ejecutando dotnet publish..." -ForegroundColor Cyan
dotnet publish WorkerService\WorkerService.csproj -c Release -f net10.0 -r win-x64 -o $outputDir /p:IsWorkerBuild=true

if ($LASTEXITCODE -eq 0) {
    Write-Host "Publicación completada exitosamente en: $outputDir" -ForegroundColor Green
} else {
    Write-Host "Error durante la publicación." -ForegroundColor Red
    exit 1
}
