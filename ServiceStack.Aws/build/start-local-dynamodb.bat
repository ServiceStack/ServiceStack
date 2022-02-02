REM download http://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Tools.DynamoDBLocal.html and SET DYNAMODB_HOME environment variable

java -Djava.library.path=%DYNAMODB_HOME%\DynamoDBLocal_lib -jar %DYNAMODB_HOME%\DynamoDBLocal.jar -sharedDb
