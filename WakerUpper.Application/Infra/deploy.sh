PROFILE=default
REGION=us-west-2
BUCKET=mschweitzer-artifacts
BUCKET_PREFIX=WakerUpper/

echo "BUCKET $BUCKET"
echo "BUCKET_PREFIX $BUCKET_PREFIX"
echo "PROFILE $PROFILE"
echo "REGION $REGION"

cd ..
dotnet lambda deploy-serverless WakerUpper --template template.json --s3-bucket $BUCKET --s3-prefix $BUCKET_PREFIX --profile $PROFILE --region $REGION --disable-interactive true
