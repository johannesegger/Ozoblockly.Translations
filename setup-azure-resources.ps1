$RepositoryName = gh repo view --json nameWithOwner --jq .nameWithOwner
$SubscriptionId = az account show --query id -o tsv
$ResourceLocation = "westeurope"
$ResourceGroupName = "rg-ozoblockly-translations"
$ContainerAppName = "ca-ozoblockly-translations"
$ContainerAppEnvironmentName = "cae-ozoblockly-translations"

$ResourceGroup = az group create --name $ResourceGroupName --location $ResourceLocation ` | ConvertFrom-Json
$ContainerAppEnvironment = az containerapp env create --name $ContainerAppEnvironmentName --resource-group $ResourceGroupName `
    --location $ResourceLocation `
    --logs-destination none
    | ConvertFrom-Json
$ContainerApp = az containerapp create --name $ContainerAppName --resource-group $ResourceGroupName `
    --environment $ContainerAppEnvironmentName `
    --ingress external --target-port 80 `
    --cpu 0.25 --memory 0.5Gi `
    --min-replicas 0 --max-replicas 1
    | ConvertFrom-Json

$ServicePrincipal = az ad sp create-for-rbac `
    --name "ozoblockly-translations" `
    --role contributor `
    --scopes /subscriptions/$SubscriptionId/resourceGroups/$ResourceGroupName `
    --sdk-auth
    | ConvertFrom-Json

gh secret set AZURE_CREDENTIALS -a actions -b (ConvertTo-Json $ServicePrincipal) --repo $RepositoryName
