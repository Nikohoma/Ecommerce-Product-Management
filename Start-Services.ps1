# Kill existing processes
taskkill /F /IM Gateway.exe 2>$null
taskkill /F /IM Auth.exe 2>$null
taskkill /F /IM ProductService.exe 2>$null
taskkill /F /IM ReportingService.exe 2>$null
taskkill /F /IM WorkflowService.exe 2>$null

Write-Host "Starting Services..." -ForegroundColor Cyan

# Start each service in a new window
Start-Process dotnet -ArgumentList "run --project Gateway\Gateway.csproj --urls http://localhost:7000"
Start-Process dotnet -ArgumentList "run --project AuthService\Auth.csproj --urls https://localhost:7097"
Start-Process dotnet -ArgumentList "run --project ProductService\ProductService.csproj --urls https://localhost:7098"
Start-Process dotnet -ArgumentList "run --project ReportingService\ReportingService.csproj --urls https://localhost:7258"
Start-Process dotnet -ArgumentList "run --project WorkflowService\WorkflowService.csproj --urls https://localhost:7149"

Write-Host "Start Rabbit MQ. Please wait about 30 seconds before logging in." -ForegroundColor Green
