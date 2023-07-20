$SubscriptionId = az account show --query id -o tsv
$ResourceGroup = "rg-ozoblockly-editor"
$ServicePrincipal = az ad sp create-for-rbac `
    --name "ozoblockly-translations" `
    --role contributor `
    --scopes /subscriptions/$SubscriptionId/resourceGroups/$ResourceGroup `
    --sdk-auth
    | ConvertFrom-Json
$RepositoryName = gh repo view --json nameWithOwner --jq .nameWithOwner
gh secret set AZURE_CREDENTIALS -a actions -b $ServicePrincipal.clientSecret --repo $RepositoryName
