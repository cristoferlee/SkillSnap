# SkillSnap Deployment Guide

## Purpose

This document describes the current manual Docker and Azure deployment process
for SkillSnap.

Commands that create, update, restart, or delete cloud resources must be
reviewed before execution. Secret values must never be committed, pasted into
documentation, or shared in terminal output.

## Production Overview

The production deployment contains:

| Resource | Purpose |
| --- | --- |
| Azure Container Registry | Stores the private SkillSnap container image |
| Azure Container Apps Environment | Hosts the Consumption workload |
| Azure Container App | Runs the API and published Blazor client |
| Azure Storage Account | Provides persistent Azure Files shares |
| `skillsnap-data` share | Stores `skillsnap.db` |
| `skillsnap-keys` share | Stores ASP.NET Core Data Protection keys |
| User-assigned Managed Identity | Pulls the private image through `AcrPull` |

Production URL:

[https://ca-christoper-portfolio-prod.politesea-922c005b.eastus.azurecontainerapps.io](https://ca-christoper-portfolio-prod.politesea-922c005b.eastus.azurecontainerapps.io)

## Resource Names

```text
Resource group:           rg-skillsnap-prod
Container registry:       skillsnapcrregistry
Container Apps environment:
                          cae-christoper-portfolio-prod
Container App:            ca-christoper-portfolio-prod
Storage account:          stskillsnapprod
Data file share:          skillsnap-data
Data Protection share:    skillsnap-keys
Managed Identity:         id-skillsnap-acr-pull
Region:                   East US
```

Azure subscription IDs, storage keys, administrator credentials, and other
secret values are intentionally excluded from this document.

## Local Prerequisites

- Docker Desktop with the Docker engine running.
- Azure CLI.
- An authenticated Azure CLI session.
- Access to the target Azure subscription.
- The production resource group and resources described above.
- A local `.env` file created from `.env.example`.

Verify the tools:

```powershell
docker version
az version
az account show --output table
```

## Local Configuration

Create the private environment file:

```powershell
Copy-Item .\.env.example .\.env
```

Configure development-only administrator values in `.env`:

```text
Admin__Email
Admin__Password
```

Do not reuse a personal password. The `.env` file is excluded from Git and must
not be uploaded to Azure or included in the Docker build context.

## Build and Test

Before building a production image:

```powershell
dotnet build
dotnet test --no-build
```

The current verified suite contains 46 passing tests.

Create a Release publish when validating the host outside Docker:

```powershell
dotnet publish .\SkillSnap.Api\SkillSnap.Api.csproj `
    --configuration Release `
    --output .\artifacts\publish
```

## Docker Image

Build the application from the solution root:

```powershell
docker build `
    --file .\SkillSnap.Api\Dockerfile `
    --tag skillsnap-api:local `
    .
```

The Dockerfile uses separate .NET SDK and ASP.NET Core runtime stages. The final
container listens on port `8080`.

The `.dockerignore` file excludes source control metadata, IDE state, build
output, test output, SQLite databases, local environment files, artifacts, and
backups.

## Local Container Verification

Start the application through Docker Compose:

```powershell
docker compose up --detach --build
```

Check status:

```powershell
docker compose ps
```

Check startup logs:

```powershell
docker compose logs --tail 40 api
```

Open:

```text
http://localhost:8080
```

Verify:

1. Home loads.
2. Projects are displayed.
3. Admin authentication succeeds.
4. Project and skill mutations work.
5. Contact submission succeeds.
6. The message appears in the Admin inbox.

Stop the container without deleting named volumes:

```powershell
docker compose down
```

Do not use `docker compose down --volumes` unless permanent deletion of local
container data is explicitly intended.

## Image Publication

Use an immutable version tag for each release:

```powershell
docker tag `
    skillsnap-api:local `
    skillsnapcrregistry.azurecr.io/skillsnap:0.1.0
```

Authenticate to the registry:

```powershell
az acr login --name skillsnapcrregistry
```

Push the approved image:

```powershell
docker push skillsnapcrregistry.azurecr.io/skillsnap:0.1.0
```

Verify the remote digest:

```powershell
az acr repository show `
    --name skillsnapcrregistry `
    --image skillsnap:0.1.0 `
    --query "{Tag:name, Digest:digest}" `
    --output table
```

Publishing a new image does not update the running Container App automatically.

## Persistent Storage

The Container Apps Environment exposes two Azure Files storage definitions:

```text
skillsnap-data
skillsnap-keys
```

The Container App mounts them as:

```text
skillsnap-data -> /data
skillsnap-keys -> /keys
```

Runtime configuration points to:

```text
ConnectionStrings__SkillSnap=Data Source=/data/skillsnap.db
DataProtection__KeysPath=/keys
```

The maximum replica count must remain `1` while SQLite is used.

## Initial Database Upload

Stop any local process that may be writing to the SQLite database before copying
it.

Create a local backup:

```powershell
New-Item -ItemType Directory -Path .\backups -Force

Copy-Item `
    .\SkillSnap.Api\skillsnap.db `
    .\backups\skillsnap-before-azure.db
```

Load a storage account key into the current PowerShell session without printing
it:

```powershell
$storageKey = az storage account keys list `
    --resource-group rg-skillsnap-prod `
    --account-name stskillsnapprod `
    --query "[0].value" `
    --output tsv
```

Confirm the destination is empty:

```powershell
az storage file list `
    --account-name stskillsnapprod `
    --share-name skillsnap-data `
    --account-key $storageKey `
    --query "[].name" `
    --output table
```

Upload only after confirming that no production database will be overwritten:

```powershell
az storage file upload `
    --account-name stskillsnapprod `
    --share-name skillsnap-data `
    --account-key $storageKey `
    --source .\SkillSnap.Api\skillsnap.db `
    --path skillsnap.db
```

Clear the temporary key:

```powershell
Remove-Variable storageKey -ErrorAction SilentlyContinue
```

## Container App Configuration

The post-bootstrap Container App template is stored in:

```text
containerapp.yaml
```

It defines:

- The private image.
- External HTTPS ingress.
- Target port `8080`.
- Managed Identity registry authentication.
- Production environment variables.
- Azure Files volume mounts.
- Scale range from `0` to `1`.
- References to the Admin secrets.

The file contains secret references but no secret values.

### Initial bootstrap limitation

Azure Container App secrets belong to the Container App and cannot be created
before the application exists. The first deployment therefore requires this
order:

1. Create the initial app revision without the two Admin `secretRef` environment
   entries.
2. Store `admin-email` and `admin-password` as Container Apps secrets.
3. Add `Admin__Email` and `Admin__Password` as secret-backed environment
   variables.
4. Keep the final secret references in `containerapp.yaml`.

The committed YAML represents the final post-bootstrap state.

## Administrator Secrets

Load private values from `.env` without printing them:

```powershell
$adminEmail = (Get-Content .\.env |
    Where-Object { $_ -match '^Admin__Email=' } |
    Select-Object -First 1).Substring("Admin__Email=".Length)

$adminPassword = (Get-Content .\.env |
    Where-Object { $_ -match '^Admin__Password=' } |
    Select-Object -First 1).Substring("Admin__Password=".Length)
```

Store them as platform secrets:

```powershell
az containerapp secret set `
    --name ca-christoper-portfolio-prod `
    --resource-group rg-skillsnap-prod `
    --secrets `
        "admin-email=$adminEmail" `
        "admin-password=$adminPassword"
```

Reference the secrets from .NET configuration:

```powershell
az containerapp update `
    --name ca-christoper-portfolio-prod `
    --resource-group rg-skillsnap-prod `
    --set-env-vars `
        "Admin__Email=secretref:admin-email" `
        "Admin__Password=secretref:admin-password"
```

Clear temporary values:

```powershell
Remove-Variable adminEmail, adminPassword -ErrorAction SilentlyContinue
```

## Deploy the Container App

Initial creation from YAML requires the application name and resource group for
Azure CLI validation:

```powershell
az containerapp create `
    --name ca-christoper-portfolio-prod `
    --resource-group rg-skillsnap-prod `
    --yaml .\containerapp.yaml
```

When `--yaml` is used, the YAML remains the source of the application
configuration.

A clean first deployment must follow the bootstrap order described above.

## Deploy a New Image Version

After testing and pushing a new immutable image tag, update only the image:

```powershell
az containerapp update `
    --name ca-christoper-portfolio-prod `
    --resource-group rg-skillsnap-prod `
    --image skillsnapcrregistry.azurecr.io/skillsnap:<version>
```

Do not reuse an existing release tag. Verify the new revision before directing
users to it.

## Deployment Verification

Check the active revision:

```powershell
az containerapp revision list `
    --name ca-christoper-portfolio-prod `
    --resource-group rg-skillsnap-prod `
    --query "[].{Name:name,Active:properties.active,Health:properties.healthState,Status:properties.provisioningState,Traffic:properties.trafficWeight}" `
    --output table
```

The expected state is:

```text
Active:  True
Health:  Healthy
Status:  Provisioned
Traffic: 100
```

Complete these functional checks:

1. Load Home over HTTPS.
2. Open Projects and confirm persisted data.
3. Sign in through `/admin`.
4. Confirm the Admin inbox loads.
5. Submit a public contact message.
6. Confirm the message appears in the Admin inbox.
7. Restart the active revision.
8. Confirm the Admin session and portfolio data remain available.

## Revision Restart

Obtain the active revision name before restarting:

```powershell
az containerapp revision list `
    --name ca-christoper-portfolio-prod `
    --resource-group rg-skillsnap-prod `
    --query "[?properties.active].name" `
    --output tsv
```

Restart only after explicit authorization:

```powershell
az containerapp revision restart `
    --name ca-christoper-portfolio-prod `
    --resource-group rg-skillsnap-prod `
    --revision <active-revision-name>
```

A brief interruption or cold start can occur.

## Backup Preparation

Automated production backups are not implemented yet.

Until a backup process is introduced:

- Keep the verified local pre-deployment backup.
- Do not overwrite the Azure database without first checking the destination.
- Stop or quiesce writes before taking a consistent SQLite file backup.
- Treat restore operations as destructive and require explicit authorization.

A tested automated backup and restore procedure is prepared for later work.

## Cost Controls

The current cost-conscious configuration uses:

- Azure Container Apps Consumption.
- `minReplicas: 0`.
- `maxReplicas: 1`.
- No Log Analytics workspace.
- Standard locally redundant storage.
- Small Azure Files share quotas.
- A monthly Azure budget with alerts.

Budget alerts do not automatically stop resources and cost data can be delayed.
Cost Management and the resource inventory should be reviewed regularly.

List current resources:

```powershell
az resource list `
    --resource-group rg-skillsnap-prod `
    --query "[].{Name:name,Type:type,Location:location}" `
    --output table
```

## Current Operational Constraints

- Deployment is manual.
- Backups are not automated.
- Scale-to-zero can cause cold starts.
- SQLite requires a single application replica.
- Azure Files is appropriate only for the current low-volume workload.
- Application logs are not retained in Log Analytics.

These constraints are intentional and should be reconsidered only when actual
traffic, reliability, or operational requirements justify additional cost and
complexity.