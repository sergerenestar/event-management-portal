``` mermaid
graph TD
    %% ==================== MAIN CONTAINER ====================
    subgraph "📦 Resource Group: rg-eventportal-dev [westus2]"
        direction TB

        %% ==================== FRONTEND ====================
        subgraph "🌐 Frontend"
            FE[Container App<br/>ca-frontend-eventportal-dev<br/>nginx:alpine · port 80]
        end

        %% ==================== BACKEND ====================
        subgraph "⚙️ Backend"
            CAE[Container App Environment<br/>cae-eventportal-dev]
            BE[Container App<br/>ca-backend-eventportal-dev<br/>.NET 8 · port 8080]
        end

        %% ==================== REGISTRY ====================
        subgraph "🐳 Container Registry"
            ACR[Azure Container Registry<br/>acreventportaldev<br/>Basic SKU]
        end

        %% ==================== DATABASE ====================
        subgraph "🗄️ Database"
            SQLSVR[SQL Server<br/>sql-eventportal-dev]
            SQLDB[SQL Database<br/>sqldb-eventportal-dev<br/>S0]
        end

        %% ==================== STORAGE & SECURITY ====================
        subgraph "🔐 Storage & Secrets"
            KV[🔑 Key Vault<br/>kv-eventportal-dev<br/>Standard]
            ST[💾 Storage Account<br/>stcmfieventportaldev<br/>Standard LRS]
        end

        %% ==================== MONITORING ====================
        subgraph "📊 Monitoring"
            LAW[Log Analytics Workspace<br/>law-eventportal-dev]
            AI[Application Insights<br/>appi-eventportal-dev]
        end
    end

    %% ==================== CI/CD ====================
    GHA[GitHub Actions<br/>dev-deploy.yml]

    %% ==================== CONNECTIONS ====================
    CAE -->|Hosts| BE
    CAE -->|Hosts| FE
    FE  -->|API calls| BE
    BE  -->|Reads/Writes| SQLDB
    BE  -->|Stores Blobs| ST
    BE  -->|Retrieves Secrets| KV
    BE  -.->|Telemetry| AI
    AI  -->|Stores Logs| LAW
    SQLDB -->|Hosted on| SQLSVR

    GHA -->|docker push| ACR
    GHA -->|az containerapp update| BE
    GHA -->|az containerapp update| FE
    ACR -->|pulls image| BE
    ACR -->|pulls image| FE

    %% ==================== STYLING ====================
    classDef frontend  fill:#e8f5e9,stroke:#388e3c,stroke-width:2px,color:#1b5e20
    classDef backend   fill:#fff3e0,stroke:#f57c00,stroke-width:2px,color:#e65100
    classDef registry  fill:#e3f2fd,stroke:#1565c0,stroke-width:2px,color:#0d47a1
    classDef database  fill:#ffebee,stroke:#c62828,stroke-width:2px,color:#b71c1c
    classDef security  fill:#f3e5f5,stroke:#7b1fa2,stroke-width:2px,color:#4a148c
    classDef monitoring fill:#e0f2f1,stroke:#00796b,stroke-width:2px,color:#004d40
    classDef cicd      fill:#fafafa,stroke:#424242,stroke-width:2px,color:#212121

    class FE frontend
    class CAE,BE backend
    class ACR registry
    class SQLSVR,SQLDB database
    class KV,ST security
    class LAW,AI monitoring
    class GHA cicd
```
