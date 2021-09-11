$awsProfile = "movement-pass"
$app = "movement-pass"
$version = "v1"
$name = "public-api-background-job"
$location = "dist"

New-Item -Name $location -ItemType directory -Force

Set-Location -Path "../MovementPass.Public.Api.BackgroundJob"
Remove-Item "obj" -Recurse
Remove-Item "bin" -Recurse
dotnet lambda package -o "$($app)_$($name)_$($version).zip"
Set-Location -Path "../MovementPass.Public.Api.BackgroundJob.Stack"
Move-Item "../MovementPass.Public.Api.BackgroundJob/$($app)_$($name)_$($version).zip" $location -Force

cdk deploy --require-approval never --profile ${awsProfile}
