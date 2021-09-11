#!/usr/bin/env sh

awsProfile="movement-pass"
app="movement-pass"
version="v1"
name="public-api-background-job"
location="dist"

rm -rf ${location}
mkdir ${location}

cd ../MovementPass.Public.Api.BackgroundJob || exit
rm -rf obj
rm -rf bin
dotnet lambda package -o ${app}_${name}_${version}.zip
cd ../MovementPass.Public.Api.BackgroundJob.Stack || exit
mv ../MovementPass.Public.Api.BackgroundJob/${app}_${name}_${version}.zip ${location}/

cdk deploy --require-approval never --profile ${awsProfile}
